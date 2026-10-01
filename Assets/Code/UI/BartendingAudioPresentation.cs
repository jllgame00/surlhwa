using UnityEngine;

/// Reads rendered stream/tool motion and accepted discrete ice actions.
[DefaultExecutionOrder(1000)]
public sealed class BartendingAudioPresentation : MonoBehaviour
{
    private SurlhwaSoundscape sounds;
    public SurlhwaSoundscape Sounds => sounds;
    private IngredientShelfController shelf;
    private StirMiniGame stir;
    private ShakeMiniGame shake;
    private Vector3 previousTool;
    private bool sampled, resting = true, transferring, techniqueSounding;
    private int actions;
    private BarSound? previousStream;
    public void Bind(SurlhwaSoundscape owner)
    {
        sounds = owner;
        shelf = GetComponentInChildren<IngredientShelfController>(true);
        stir = GetComponentInChildren<StirMiniGame>(true);
        shake = GetComponentInChildren<ShakeMiniGame>(true);
        actions = shelf?.PreparationTrace?.Actions.Count ?? 0;
        sampled = false; resting = true; transferring = false;
    }
    public void Unbind() { if (sounds != null) sounds.StopPhysical(); sounds = null; }
    private void LateUpdate()
    {
        if (sounds == null || shelf == null) return;
        var view = shelf.PhysicalPresentation;
        bool paused = Time.timeScale <= 0 || DemoAudioController.IsSettingsOpen || RecipeGuidePanelController.IsTechniqueReadStateActive;
        BarSound? stream = !paused && view != null && view.StreamVisible ? view.StreamSound : null;
        if (stream != previousStream && sounds.Trace)
            Debug.Log($"[Phase O Visual] t={Time.unscaledTime:F3} frame={Time.frameCount} stream={stream?.ToString() ?? "off"}");
        previousStream = stream;
        sounds.SetPour(stream);
        if (view != null && !paused)
        {
            if (resting && !view.IsResting) sounds.Play(BarSound.BottleHandle);
            if (!resting && view.IsResting) sounds.Play(BarSound.BottleHandle, level: .65f);
            if (transferring && !view.IsTransferringVessel) sounds.Play(BarSound.JiggerContact);
            resting = view.IsResting; transferring = view.IsTransferringVessel;
        }
        var trace = shelf.PreparationTrace?.Actions;
        if (trace != null)
        {
            if (actions > trace.Count) actions = trace.Count;
            while (actions < trace.Count)
                if (trace[actions++].Action == RecipePreparationAction.AddIce) sounds.Play(BarSound.Ice);
        }
        RectTransform tool = stir != null ? stir.PresentedSpoon : shake != null ? shake.PresentedShaker : null;
        bool eligible = tool != null && tool.gameObject.activeInHierarchy && (shake == null || shake.IsShakerMoving);
        bool moved = eligible && sampled && Vector3.Distance(previousTool, tool.position) > .02f && !paused;
        if (tool != null) { previousTool = tool.position; sampled = true; }
        // Do not cancel the separately observed Build/Float gentle-stir spoon.
        if (moved || techniqueSounding)
        {
            var cue = shake != null ? BarSound.Shake : BarSound.Stir;
            sounds.SetTechnique(moved ? cue : null);
            if (moved != techniqueSounding && sounds.Trace)
                Debug.Log($"[Phase O Visual] t={Time.unscaledTime:F3} frame={Time.frameCount} {cue} motion={moved}");
        }
        techniqueSounding = moved;
    }
    private void OnDisable() => Unbind();
}
