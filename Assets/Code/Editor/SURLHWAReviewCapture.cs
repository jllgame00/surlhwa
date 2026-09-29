#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Offscreen review only. Temporarily projects overlay canvases through the
/// real camera, then restores them. This is not an OS-input or final display proof.</summary>
internal static class SURLHWAReviewCapture
{
    internal static void Save(string name, int width, int height)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Camera camera = Camera.main;
        bool temporaryCamera = camera == null;
        if (temporaryCamera)
        {
            camera = new GameObject("Review UI Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }
        var canvases = new List<Canvas>();
        var overlayOrders = new Dictionary<Canvas, int>();
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay) canvases.Add(canvas);
            if ((canvas.isRootCanvas || canvas.overrideSorting) &&
                canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                overlayOrders.Add(canvas, canvas.sortingOrder);
        }
        var previousCameras = new List<Camera>();
        var previousDistances = new List<float>();
        var target = RenderTexture.GetTemporary(width, height, 24);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;
        Texture2D image = null;
        try
        {
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            // Nested override canvases (including the ending panel) must keep
            // their overlay relationship when temporarily projected in world space.
            foreach (var pair in overlayOrders) pair.Key.sortingOrder = pair.Value + 10000;
            foreach (var canvas in canvases)
            {
                previousCameras.Add(canvas.worldCamera);
                previousDistances.Add(canvas.planeDistance);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 1f;
            }
            Canvas.ForceUpdateCanvases();
            foreach (var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None)) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            foreach (var mask in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.RectMask2D>(FindObjectsSortMode.None)) mask.PerformClipping();
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Logs/Review");
            File.WriteAllBytes($"Logs/Review/{name}-{width}x{height}.png", image.EncodeToPNG());
            Debug.Log($"[Review Capture] {name} {width}x{height}");
        }
        finally
        {
            for (int i = 0; i < previousCameras.Count; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                canvases[i].worldCamera = previousCameras[i];
                canvases[i].planeDistance = previousDistances[i];
            }
            foreach (var pair in overlayOrders) pair.Key.sortingOrder = pair.Value;
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            Canvas.ForceUpdateCanvases();
            if (temporaryCamera) UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }
    }
}
#endif
