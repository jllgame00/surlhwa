#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Bounded review render through Unity's main audio output. Not a gameplay or listening test.
[InitializeOnLoad]
public static class SURLHWAAudioReviewCapture
{
    const string Key = "SURLHWA_O_AUDIO_CAPTURE";
    static SURLHWAAudioReviewCapture()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            { SessionState.EraseBool(Key); new GameObject("Audio mix review fixture").AddComponent<SURLHWAAudioReviewCaptureRunner>(); }
        };
    }
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Finish the current validation and preserve unsaved work first.");
        EditorSceneManager.OpenScene("Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity");
        SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
    }
}

[DefaultExecutionOrder(1200)]
internal sealed class SURLHWAAudioReviewCaptureRunner : MonoBehaviour
{
    readonly List<short> pcm = new();
    SurlhwaSoundscape sounds;
    int previousCaptureRate, frame, channels;
    bool recording;
    readonly BarSound[] cues = { BarSound.FootLeft, BarSound.FootRight, BarSound.Seat, BarSound.Stand,
        BarSound.EntranceAir, BarSound.BottleHandle, BarSound.GlassSet, BarSound.JiggerContact, BarSound.Ice,
        BarSound.Pour, BarSound.FinePour, BarSound.Transfer, BarSound.Strain, BarSound.Stir, BarSound.Shake,
        BarSound.Select, BarSound.Back, BarSound.BookOpen, BarSound.BookClose, BarSound.Discovery };
    void Start()
    {
        sounds = FindFirstObjectByType<DemoAudioController>().Sounds;
        sounds.SetMix(.7f, .35f, .6f, false); sounds.Trace = true;
        channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
        if (AudioSettings.speakerMode != AudioSpeakerMode.Stereo && channels != 1)
            throw new InvalidOperationException("Review capture requires the current mono/stereo output.");
        previousCaptureRate = Time.captureFramerate; Time.captureFramerate = 30;
        recording = AudioRenderer.Start();
        if (!recording) { Time.captureFramerate = previousCaptureRate; throw new InvalidOperationException("Audio output capture already owned."); }
    }
    void LateUpdate()
    {
        if (!recording) return;
        int block = frame / 90 - 2;
        if (frame % 90 == 0 && block >= 0 && block < cues.Length)
        {
            var cue = cues[block];
            Debug.Log($"[Phase O Capture] seconds={frame/30f:F2} {cue}");
            if (sounds.Library.Get(cue).loop) sounds.SetPour(cue);
            else if (cue == BarSound.Discovery) sounds.StartDiscovery();
            else sounds.Play(cue);
        }
        if (frame % 90 == 60) { sounds.SetPour(null); sounds.EndDiscovery(); }
        int count = AudioRenderer.GetSampleCountForCaptureFrame() * channels;
        using (var buffer = new NativeArray<float>(count, Allocator.Temp))
        {
            if (!AudioRenderer.Render(buffer)) throw new InvalidOperationException("Main output render failed.");
            for (int i = 0; i < buffer.Length; i++) pcm.Add((short)Mathf.RoundToInt(Mathf.Clamp(buffer[i], -1, 1) * 32767));
        }
        if (++frame < (cues.Length + 3) * 90) return;
        StopCapture();
        Directory.CreateDirectory("Logs/Review/PhaseOAudio");
        using (var w = new BinaryWriter(File.Create("Logs/Review/PhaseOAudio/unity-output-cue-reel.wav")))
        {
            int bytes = pcm.Count * 2, rate = AudioSettings.outputSampleRate;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1);
            w.Write((short)channels); w.Write(rate); w.Write(rate * channels * 2);
            w.Write((short)(channels * 2)); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (short sample in pcm) w.Write(sample);
        }
        Debug.Log("[Phase O Capture] PASS: actual Unity output; default gain cue fixture, not gameplay.");
        EditorApplication.ExitPlaymode();
    }
    void StopCapture() { if (!recording) return; AudioRenderer.Stop(); recording = false; Time.captureFramerate = previousCaptureRate; }
    void OnDisable() => StopCapture();
}
#endif
