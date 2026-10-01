using UnityEngine;

public enum BarSound
{
    RoomTone, WinterAir, FootLeft, FootRight, Seat, Stand, EntranceAir,
    BottleHandle, GlassSet, JiggerContact, Ice, Pour, FinePour, Transfer,
    Strain, Stir, Shake, Select, Back, BookOpen, BookClose, Discovery
}

/// <summary>Owned sound assets and first-pass gains; never gameplay state.</summary>
public sealed class SurlhwaAudioLibrary : ScriptableObject
{
    [System.Serializable] public sealed class Cue
    {
        public BarSound id;
        public AudioClip clip;
        [Range(0, 1)] public float gain = .5f;
        public bool loop;
    }
    public Cue[] cues;
    public Cue Get(BarSound id)
    {
        if (cues != null) foreach (var cue in cues) if (cue != null && cue.id == id) return cue;
        return null;
    }
}
