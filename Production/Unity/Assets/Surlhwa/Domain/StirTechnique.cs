using System;
using System.Collections.Generic;
using System.Linq;

namespace Surlhwa.Domain
{
    internal sealed class StirStamp
    {
        internal readonly string Id;
        internal readonly bool HowGood,QualityGood;
        internal readonly double Degrees,StableFraction;
        internal readonly long MovingMicros,StableMicros;
        internal readonly string[] Ingredients;
        internal StirStamp(double degrees,long movingMicros,long stableMicros,BrewingTuning tuning,IEnumerable<string> ingredients)
        {
            Id=Guid.NewGuid().ToString("N"); Degrees=degrees; MovingMicros=movingMicros; StableMicros=stableMicros;
            StableFraction=movingMicros==0 ? 0 : (double)stableMicros/movingMicros;
            HowGood=degrees+1e-7>=tuning.HowDegrees; QualityGood=degrees+1e-7>=tuning.QualityDegrees && StableFraction+1e-9>=tuning.StableFraction;
            Ingredients=ingredients.ToArray();
        }
    }
    internal sealed class StirMotion
    {
        private readonly BrewingTuning tuning;
        private bool baseline;
        private double angle;
        private int direction;
        internal double Degrees { get; private set; }
        private long movingMicros,stableMicros;
        internal long MovingMicros => movingMicros;
        internal long StableMicros => stableMicros;
        internal double StableFraction => movingMicros==0 ? 0 : (double)stableMicros/movingMicros;
        internal StirMotion(BrewingTuning tuning,double x,double y,StirStamp prior)
        {
            this.tuning=tuning; Degrees=prior?.Degrees ?? 0;
            movingMicros=prior?.MovingMicros ?? 0; stableMicros=prior?.StableMicros ?? 0;
            SetBaseline(x,y);
        }
        private static bool InCircle(double x,double y)
        {
            if(double.IsNaN(x)||double.IsNaN(y)||double.IsInfinity(x)||double.IsInfinity(y)) return false;
            double r=x*x+y*y; return r>=.0625 && r<=1.5625;
        }
        private void SetBaseline(double x,double y) { baseline=InCircle(x,y); if(baseline) angle=Math.Atan2(y,x)*180/Math.PI; }
        internal void Move(double x,double y,long micros)
        {
            if(micros<=0) throw new ArgumentOutOfRangeException(nameof(micros));
            if(!InCircle(x,y)) { baseline=false; direction=0; return; }
            if(!baseline) { SetBaseline(x,y); return; }
            double next=Math.Atan2(y,x)*180/Math.PI,delta=next-angle; angle=next;
            if(delta>180) delta-=360; if(delta< -180) delta+=360;
            double travel=Math.Abs(delta);
            if(travel<1e-7) return;
            movingMicros=checked(movingMicros+micros);
            // Discontinuous pointer jumps are not a circular gesture.
            if(travel>90) { direction=0; return; }
            int sign=Math.Sign(delta); double speed=travel*1000000/micros;
            bool stable=(direction==0 || direction==sign) && speed>=tuning.MinSpeed && speed<=tuning.MaxSpeed;
            direction=sign; Degrees+=travel; if(stable) stableMicros=checked(stableMicros+micros);
        }
    }
}
