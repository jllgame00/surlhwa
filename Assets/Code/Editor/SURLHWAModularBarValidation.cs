#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SURLHWAModularBarValidation
{
    private const string Pending = "SURLHWA_MODULAR_BAR_REVIEW";
    static SURLHWAModularBarValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.EraseBool(Pending);
                new GameObject("Modular bar visual fixture").AddComponent<SURLHWAModularBarReview>();
            }
        };
    }
    [MenuItem("SURLHWA/Validation/Modular Bar References")]
    public static void Run()
    {
        foreach (string folder in new[] { SURLHWAModularBarImport.EnvironmentRoot, SURLHWAModularBarImport.CharacterRoot })
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Require(sprite != null && sprite.bounds.size.sqrMagnitude > 0 && sprite.pixelsPerUnit == 32,
                    "Missing/zero-sized/world-density sprite: " + path);
                Require(importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                    importer.textureCompression == TextureImporterCompression.Uncompressed && importer.alphaIsTransparency,
                    "World pixel import drift: " + path);
                Require(sprite.pivot.y == (folder == SURLHWAModularBarImport.CharacterRoot ? 0 : sprite.rect.height / 2),
                    "World pivot drift: " + path);
                count++;
            }
            Require(count == (folder == SURLHWAModularBarImport.EnvironmentRoot ? 14 : 11), "Declared world asset set changed without review.");
        }
        var bar = AssetDatabase.LoadAssetAtPath<GameObject>(SURLHWAModularBarBuilder.PrefabPath);
        Require(bar != null, "Modular bar prefab missing.");
        var renderers = bar.GetComponentsInChildren<SpriteRenderer>();
        Require(renderers.Length >= 35 && renderers.All(r => r.sprite != null && r.sharedMaterial != null),
            "Furniture must remain independently assigned and rendered.");
        var bartender = renderers.Single(r => r.sprite.name == "Bartender");
        var front = renderers.Single(r => r.sprite.name == "CounterTop");
        Require(bartender.sortingOrder < front.sortingOrder && Mathf.Abs(bartender.transform.localPosition.x) < .01f,
            "Bartender must remain centered inside and behind the front countertop.");
        Require(renderers.Count(r => r.sprite.name == "Stool") == 3, "Three production seats need three stools.");
        Require(bar.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Memory_")) == 7,
            "Current memory display anchors missing.");
        foreach (var anchor in bar.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Memory_")))
            foreach (var occluder in renderers.Where(r => r.sprite.name.StartsWith("Counter") && r.sortingOrder > 8))
            {
                var texture = new Texture2D(2, 2);
                try
                {
                    ImageConversion.LoadImage(texture, System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(occluder.sprite)));
                    foreach (float height in new[] { 0f, .4f, .75f })
                    {
                        Vector3 local = anchor.localPosition + Vector3.up * height - occluder.transform.localPosition;
                        int x = Mathf.FloorToInt(local.x * 32 + occluder.sprite.pivot.x);
                        int y = Mathf.FloorToInt(local.y * 32 + occluder.sprite.pivot.y);
                        Require(x < 0 || y < 0 || x >= texture.width || y >= texture.height || texture.GetPixel(x, y).a < .1f,
                            "Counter hides a memory display: " + anchor.name);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        SURLHWAPresentationValidation.RunCharacters();
        Debug.Log("[Modular Bar] PASS: 25 imports, furniture references, character classes, pivots, counter depth and seven display anchors.");
    }
    [MenuItem("SURLHWA/Validation/Modular Bar Play Mode Render Fixtures")]
    public static void RenderFixtures()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play Mode first.");
        Require(!EditorSceneManager.GetActiveScene().isDirty, "Save owner scene edits before changing review scenes.");
        Run();
        EditorSceneManager.OpenScene("Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

internal sealed class SURLHWAModularBarReview : MonoBehaviour
{
    private bool oldBackground;
    private IEnumerator Start()
    {
        oldBackground = Application.runInBackground;
        Application.runInBackground = true;
        var work = Capture();
        while (true)
        {
            object next;
            try { if (!work.MoveNext()) break; next = work.Current; }
            catch (Exception e) { Debug.LogException(e); break; }
            yield return next;
        }
        Application.runInBackground = oldBackground;
        EditorApplication.ExitPlaymode();
    }
    private IEnumerator Capture()
    {
        // Visual fixtures use the production scene and seating. They never write progress.
        foreach (var spawner in FindObjectsByType<CustomerSpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
        yield return null;
        GameObject.Find("DemoEntryCanvas")?.SetActive(false);
        var ui = FindFirstObjectByType<InteractionUIController>();
        var bar = GameObject.Find("ModularWinterBar");
        SURLHWAModularBarValidation.Require(bar != null, "Production scene lacks modular prefab instance.");
        var memories = FindFirstObjectByType<CampaignBarMemories>();
        var anchors = (Transform[])Field(memories, "displayAnchors");
        SURLHWAModularBarValidation.Require(anchors.Length == 7 && anchors.All(a => a != null), "Scene memory anchor binding broke.");
        SURLHWAReviewCapture.Save("restart-empty", 1280, 720);
        var seat = FindObjectsByType<SeatPoint>(FindObjectsSortMode.None).Single(s => s.name == "SeatPoint_Left");
        foreach (string kind in new[] { "ordinary", "prince", "chunhyang", "lovers", "hamlet" })
        {
            typeof(InteractionUIController).GetMethod("ClosePanels", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(ui, new object[] { false });
            string prefab = kind == "prince" ? "SpecialGuests/LittlePrinceCustomer" : "General/Customer_A";
            var guest = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_SURLHWA/Prefabs/Customers/" + prefab + ".prefab")).GetComponent<CustomerController>();
            guest.SetUsesPatience(false);
            if (kind == "chunhyang") guest.ConfigureCampaignEncounter(AssetDatabase.LoadAssetAtPath<CampaignEncounterData>("Assets/_SURLHWA/Data/Campaign/ChunhyangMeeting.asset"));
            if (kind == "lovers") guest.ConfigureCampaignEncounter(AssetDatabase.LoadAssetAtPath<CampaignEncounterData>("Assets/_SURLHWA/Data/Campaign/LoversReunion.asset"));
            if (kind == "hamlet") guest.ConfigureCampaignEncounter(AssetDatabase.LoadAssetAtPath<CampaignEncounterData>("Assets/_SURLHWA/Data/Campaign/HamletStillness.asset"));
            guest.transform.position = seat.approachMarker.position;
            seat.Reserve(guest);
            guest.AssignSeat(seat);
            float deadline = Time.realtimeSinceStartup + 5;
            while (Vector3.Distance(guest.transform.position, seat.sitMarker.position) > .01f && Time.realtimeSinceStartup < deadline) yield return null;
            for (int i = 0; i < 4; i++) yield return null;
            var body = guest.GetComponent<SpriteRenderer>();
            if (kind == "lovers")
            {
                guest.GetComponent<CampaignGuestPresentation>().ShowServedGlass();
                yield return null;
                var contour = guest.transform.Find("One shared glass").GetComponentInChildren<LineRenderer>();
                SURLHWAModularBarValidation.Require(contour.sortingOrder > body.sortingOrder,
                    "Shared-glass presentation must stay visible in front of the guests/counter.");
            }
            if (kind == "hamlet")
            {
                var light = guest.transform.Find("Still pool of light");
                SURLHWAModularBarValidation.Require(light.GetComponentInChildren<LineRenderer>().sortingOrder == body.sortingOrder - 1 &&
                    Mathf.Approximately(light.localPosition.y, .35f), "Stillness must follow guest depth at the feet.");
            }
            SURLHWAModularBarValidation.Require(Vector3.Distance(guest.transform.position, seat.sitMarker.position) < .01f &&
                body.bounds.size.y >= 2.6f && body.sortingOrder > 200, "Seated guest scale/depth/feet drift.");
            SURLHWAReviewCapture.Save("restart-" + kind, 1280, 720);
            if (kind == "ordinary")
            {
                typeof(InteractionUIController).GetMethod("ClosePanels", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(ui, new object[] { false });
                typeof(CustomerController).GetMethod("SayLine", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(guest, new object[] { "따뜻한 한 잔이네요." });
                typeof(CustomerController).GetMethod("ShowHearts", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(guest, new object[] { 2 });
                yield return null;
                var bubble = FindFirstObjectByType<DialogueBubble>();
                var hearts = FindFirstObjectByType<HeartPopup>();
                SURLHWAModularBarValidation.Require(bubble != null && hearts != null &&
                    bubble.transform.position.y > body.bounds.max.y + .5f &&
                    hearts.transform.position.y > bubble.transform.position.y + .5f,
                    "Reaction overlays must clear the current character head and each other.");
                SURLHWAReviewCapture.Save("restart-reaction", 1280, 720);
                Destroy(bubble.gameObject);
                Destroy(hearts.gameObject);
            }
            if (kind == "chunhyang")
            {
                ((GameObject)Field(ui, "brewPanel")).SetActive(true);
                yield return null;
                SURLHWAReviewCapture.Save("restart-bartending-ready", 1280, 720);
                ((GameObject)Field(ui, "brewPanel")).SetActive(false);
                ((Button)Field(ui, "ellipsisButton")).onClick.Invoke();
                yield return null;
                SURLHWAReviewCapture.Save("restart-dialogue", 1280, 720);
            }
            Destroy(guest.gameObject);
            seat.Release();
            yield return null;
        }
        // Layout-only fixture, not earned progress: show existing contour objects briefly.
        var drawings = (System.Collections.Generic.Dictionary<string, GameObject>)Field(memories, "memories");
        var priorVisibility = drawings.Values.Select(g => g.activeSelf).ToArray();
        foreach (var drawing in drawings.Values) drawing.SetActive(true);
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.enabled).ToArray();
        foreach (var canvas in canvases) canvas.enabled = false;
        SURLHWAReviewCapture.Save("restart-display-layout-fixture", 1280, 720);
        foreach (var canvas in canvases) canvas.enabled = true;
        int index = 0;
        foreach (var drawing in drawings.Values) drawing.SetActive(priorVisibility[index++]);
        Debug.Log("[Modular Bar Play Mode] PASS: production room, ordinary/Prince/Chunhyang seating, scale/depth, overhead prompt, ready panel and dialogue renders. Staged visual fixtures; no campaign completion claim.");
    }
    private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
}
#endif
