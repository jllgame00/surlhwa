using System;
using UnityEngine;

/// <summary>Bounded scene-owned voices. Gains come only from DemoAudioController.
/// Completion of a sound never completes gameplay or UI presentation.</summary>
[DefaultExecutionOrder(1100)]
public sealed class SurlhwaSoundscape : MonoBehaviour
{
    public const int OneShotVoices = 6;
    public const int SourceCount = OneShotVoices + 6;
    public SurlhwaAudioLibrary Library { get; private set; }
    public AudioSource PourSource => loops[0].source;
    public AudioSource TechniqueSource => loops[1].source;
    public AudioSource DiscoverySource => discovery;
    public bool Trace;
    public event Action<BarSound, bool, string> CueChanged;
    private sealed class Loop
    {
        public AudioSource source;
        public BarSound? desired;
        public BarSound playing;
        public float envelope;
    }
    private readonly Loop[] loops = new Loop[2];
    private AudioSource room, wind, music, discovery;
    private readonly AudioSource[] voices = new AudioSource[OneShotVoices];
    private readonly float[] voiceGain = new float[OneShotVoices];
    private readonly float[] lastCue = new float[22];
    private float master, musicGain, effectsGain, roomFade, readingFade = 1, discoveryFade;
    private bool muted, discoveryEnding;
    public bool Reading { get; set; }

    private AudioSource Source(string role, AudioClip clip = null, bool loop = false)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false; source.spatialBlend = 0; source.loop = loop;
        source.clip = clip; source.volume = 0; source.priority = role == "UI" ? 100 : 128;
        return source;
    }
    private void Awake()
    {
        Library = Resources.Load<SurlhwaAudioLibrary>("SurlhwaAudioLibrary");
        room = Source("Room", Clip(BarSound.RoomTone), true);
        wind = Source("Winter", Clip(BarSound.WinterAir), true);
        music = Source("Music"); // Accepted soundtrack absent; retain its existing preference bus.
        discovery = Source("UI");
        for (int i = 0; i < 2; i++) loops[i] = new Loop { source = Source("Physical", null, true) };
        for (int i = 0; i < voices.Length; i++) voices[i] = Source("OneShot");
        for (int i = 0; i < lastCue.Length; i++) lastCue[i] = -10;
    }
    private AudioClip Clip(BarSound cue) => Library != null ? Library.Get(cue)?.clip : null;
    private float Gain(BarSound cue) => Library != null && Library.Get(cue) != null ? Library.Get(cue).gain : 0;
    private void Report(BarSound cue, bool start, string channel)
    {
        CueChanged?.Invoke(cue, start, channel);
        if (Trace) Debug.Log($"[Phase O Audio] t={Time.unscaledTime:F3} frame={Time.frameCount} {channel} {cue} {(start ? "start" : "stop")}");
    }
    public void SetMix(float masterVolume, float musicVolume, float effectsVolume, bool mute)
    {
        master = Mathf.Clamp01(masterVolume); musicGain = Mathf.Clamp01(musicVolume);
        effectsGain = Mathf.Clamp01(effectsVolume); muted = mute;
        foreach (var source in voices)
        {
            if (source == null) continue;
            source.mute = mute;
            if (mute || master == 0 || effectsGain == 0) source.Stop();
        }
        if (mute || master == 0 || effectsGain == 0)
        {
            roomFade = 0;
            foreach (var loop in loops) StopLoop(loop);
            room.Stop(); wind.Stop(); discovery.Stop(); discoveryFade = 0;
        }
        ApplyVolumes(); // Mute and zero volume take effect in the slider callback.
    }
    public void Play(BarSound cue, float pan = 0, float level = 1)
    {
        if (muted || master <= 0 || effectsGain <= 0 || Clip(cue) == null) return;
        float gap = cue == BarSound.FootLeft || cue == BarSound.FootRight ? .07f : .12f;
        if (Time.unscaledTime - lastCue[(int)cue] < gap) return;
        for (int i = 0; i < voices.Length; i++)
        {
            if (voices[i].isPlaying) continue;
            lastCue[(int)cue] = Time.unscaledTime;
            voices[i].clip = Clip(cue); voices[i].panStereo = Mathf.Clamp(pan, -.25f, .25f);
            voices[i].pitch = cue == BarSound.FootRight ? .99f : 1;
            voiceGain[i] = Gain(cue) * Mathf.Clamp01(level);
            voices[i].volume = Effective * voiceGain[i] * (Reading ? .75f : 1);
            voices[i].Play(); Report(cue, true, "one-shot"); return;
        }
        // Quietly drop a seventh simultaneous accent instead of growing voices.
    }
    public void SetPour(BarSound? cue) => loops[0].desired = cue;
    public void SetTechnique(BarSound? cue) => loops[1].desired = cue;
    public void StopPhysical()
    {
        foreach (var loop in loops) { loop.desired = null; StopLoop(loop); }
    }
    public void StartDiscovery()
    {
        if (muted || Effective <= 0 || Clip(BarSound.Discovery) == null) return;
        discovery.Stop(); discovery.clip = Clip(BarSound.Discovery);
        discoveryEnding = false; discoveryFade = 1;
        discovery.volume = Effective * Gain(BarSound.Discovery);
        discovery.Play(); Report(BarSound.Discovery, true, "discovery");
    }
    public void EndDiscovery() => discoveryEnding = true;
    private float Effective => muted ? 0 : master * effectsGain;
    private void LateUpdate()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
        readingFade = Mathf.MoveTowards(readingFade, Reading ? .55f : 1, dt * 1.5f);
        roomFade = Mathf.MoveTowards(roomFade, Effective > 0 ? 1 : 0, dt * 2);
        if (Effective > 0)
        {
            if (room.clip != null && !room.isPlaying) room.Play();
            if (wind.clip != null && !wind.isPlaying) wind.Play();
        }
        foreach (var loop in loops)
        {
            bool wants = Effective > 0 && loop.desired.HasValue && Clip(loop.desired.Value) != null;
            bool same = wants && loop.playing == loop.desired.Value;
            loop.envelope = Mathf.MoveTowards(loop.envelope, same && loop.source.isPlaying ? 1 : 0, dt / .045f);
            if (loop.source.isPlaying && loop.envelope <= 0) StopLoop(loop);
            if (wants && !loop.source.isPlaying)
            {
                loop.playing = loop.desired.Value; loop.source.clip = Clip(loop.playing);
                loop.source.volume = 0; loop.source.Play(); Report(loop.playing, true, "loop");
            }
        }
        if (discoveryEnding) discoveryFade = Mathf.MoveTowards(discoveryFade, 0, dt / .04f);
        if (discoveryEnding && discoveryFade <= 0 && discovery.isPlaying)
        { discovery.Stop(); Report(BarSound.Discovery, false, "discovery"); }
        ApplyVolumes();
    }
    private void StopLoop(Loop loop)
    {
        if (loop == null) return;
        if (loop.source.isPlaying) Report(loop.playing, false, "loop");
        loop.source.Stop(); loop.envelope = 0;
    }
    private void ApplyVolumes()
    {
        if (room == null) return;
        room.volume = Effective * Gain(BarSound.RoomTone) * roomFade * readingFade;
        wind.volume = Effective * Gain(BarSound.WinterAir) * roomFade * readingFade * readingFade;
        music.volume = muted ? 0 : master * musicGain;
        discovery.volume = Effective * Gain(BarSound.Discovery) * discoveryFade;
        foreach (var loop in loops) loop.source.volume = Effective * Gain(loop.playing) * loop.envelope;
        for (int i = 0; i < voices.Length; i++) voices[i].volume = Effective * voiceGain[i] * (Reading ? .75f : 1);
    }
    private void OnDisable()
    {
        StopPhysical();
        if (room != null) room.Stop(); if (wind != null) wind.Stop();
        if (music != null) music.Stop(); if (discovery != null) discovery.Stop();
        foreach (var voice in voices) if (voice != null) voice.Stop();
        roomFade = 0;
    }
}
