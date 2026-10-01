using UnityEngine;

// A gentle-stir spoon exists only within its method workspace. No gameplay writes.
[DefaultExecutionOrder(1000)]
public sealed class BartendingToolAudio : MonoBehaviour
{
    private Vector3 previous;
    private SurlhwaSoundscape sounds;
    private bool sampled, sounding;
    private void OnEnable() { sampled = false; sounds = GetComponentInParent<BartendingAudioPresentation>()?.Sounds; }
    private void LateUpdate()
    {
        bool moved = sampled && Vector3.Distance(previous, transform.position) > .02f;
        previous = transform.position; sampled = true;
        moved &= Time.timeScale > 0 && !RecipeGuidePanelController.IsTechniqueReadStateActive;
        if (moved || sounding) sounds?.SetTechnique(moved ? BarSound.Stir : null);
        sounding = moved;
    }
    private void OnDisable()
    {
        if (sounding) sounds?.SetTechnique(null);
        sounding = false;
    }
}
