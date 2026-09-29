using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation assets only. No recipe, amount, score or persistence state.</summary>
public sealed class PixelBartendingArt : ScriptableObject
{
    public Sprite[] sprites;
    private static PixelBartendingArt loaded;

    public static Sprite Get(string name)
    {
        if (loaded == null) loaded = Resources.Load<PixelBartendingArt>("PixelBartendingArt");
        if (loaded != null && loaded.sprites != null)
            foreach (var sprite in loaded.sprites)
                if (sprite != null && sprite.name == name) return sprite;
        return null;
    }

    public static void Apply(RectTransform target, string name)
    {
        if (target == null) return;
        var image = target.GetComponent<Image>();
        if (image == null) image = target.gameObject.AddComponent<Image>();
        image.sprite = Get(name);
        image.color = Color.white;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
    }

    public static RectTransform Grip(Transform owner, Vector2 position, float scale = 1f)
    {
        var existing = owner.Find("PixelGrip") as RectTransform;
        if (existing != null) return existing;
        var grip = BartendingPhysicalPresentation.Shape("PixelGrip", owner,
            new Vector2(40f, 32f) * scale, Color.white);
        Apply(grip, "Grip");
        grip.anchoredPosition = position;
        return grip;
    }
}
