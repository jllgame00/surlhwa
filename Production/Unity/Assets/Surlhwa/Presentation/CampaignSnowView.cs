using System.Linq;
using UnityEngine;
namespace Surlhwa.Presentation
{
    public sealed class CampaignSnowView : MonoBehaviour
    {
        private Transform[] flakes; private Vector3[] origins; private float clock;
        private void Start()
        { flakes=FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(x=>x.name=="C_SnowfallMark").OrderBy(x=>x.position.x).ToArray(); origins=flakes.Select(x=>x.position).ToArray(); }
        private void Update()
        {
            clock+=Mathf.Min(Time.unscaledDeltaTime,.1f);
            for(int i=0;i<flakes.Length;i++) if(flakes[i]!=null) {
                // World-space source pixels; quantized drift preserves the authored pixel language.
                float fall=Mathf.Repeat(clock*(.08f+i%3*.015f)+i*.07f,1.6f);
                flakes[i].position=origins[i]+new Vector3(Mathf.Round(Mathf.Sin(clock*.2f+i)*3)/70f,-Mathf.Round(fall*70)/70f,0);
            }
        }
    }
}
