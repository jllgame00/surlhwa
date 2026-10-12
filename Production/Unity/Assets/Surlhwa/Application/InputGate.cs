namespace Surlhwa.Application
{
    public sealed class InputGate
    {
        private long lastEvent; public bool HeldBlocked { get; private set; }
        public bool Consume(long eventId) { if(eventId<=lastEvent) return false; lastEvent=eventId; return true; }
        public void CancelHeld() { HeldBlocked=true; }
        public void Release() { HeldBlocked=false; }
    }
}
