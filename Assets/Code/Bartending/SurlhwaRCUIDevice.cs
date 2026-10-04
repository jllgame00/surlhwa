#if UNITY_EDITOR || SURLHWA_RC_QA
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Opt-in standalone UI device fallback. No gameplay commands or state.</summary>
public sealed class SurlhwaRCUIDevice : MonoBehaviour
{
    const string Folder = "Logs/Review/RCAutomatedQA/standalone-native";
    [Serializable] sealed class Packet { public string id; public float x, y; public bool pressed; }
    Mouse device;
    void Awake()
    {
        device = InputSystem.AddDevice<Mouse>("RC simulated UI pointer");
        Debug.Log("[RC UI device] SIMULATED_POINTER_DEVICE; legacy keyboard/mouse polling stays native. device=" + device.deviceId);
    }
    void Update()
    {
        string path = Folder + "/ui-device-input.json";
        if (!File.Exists(path)) return;
        var packet = JsonUtility.FromJson<Packet>(File.ReadAllText(path));
        File.Delete(path);
        if (packet == null || string.IsNullOrEmpty(packet.id) || !float.IsFinite(packet.x) || !float.IsFinite(packet.y))
            throw new InvalidOperationException("Invalid RC UI device packet.");
        // Same released-move / press / release device boundary used by the
        // validated Editor pointer harness. The external driver paces packets.
        var state = new MouseState { position = new Vector2(packet.x, packet.y) };
        if (packet.pressed) state = state.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(device, state);
        SurlhwaRCNativeSmoke.Record("SIMULATED_POINTER_DEVICE id=" + packet.id + " position=" + state.position + " pressed=" + packet.pressed);
        File.WriteAllText(Folder + "/ui-device-ack.txt", packet.id);
    }
    void OnDestroy() { if (device != null && device.added) InputSystem.RemoveDevice(device); }
}
#endif
