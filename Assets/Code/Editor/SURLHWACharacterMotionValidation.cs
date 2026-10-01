#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Bounded production-scene motion fixtures. Campaign proof remains the existing full-route runner.</summary>
[InitializeOnLoad]
public static class SURLHWACharacterMotionValidation
{
    private const string Pending = "SURLHWA_PHASE_N_MOTION";
    static SURLHWACharacterMotionValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.EraseBool(Pending);
                new GameObject("Phase N motion fixture").AddComponent<SURLHWACharacterMotionRunner>();
            }
        };
    }

    [MenuItem("SURLHWA/Validation/Character Motion References")]
    public static void References()
    {
        var library = Resources.Load<CustomerMotionLibrary>("CustomerMotionLibrary");
        Require(library != null && library.characters.Length > 0, "Motion library missing.");
        foreach (var poses in library.characters)
        {
            Require(poses.walk.Length == 4 && poses.walk.Distinct().Count() == 4, "Four distinct walk frames required.");
            foreach (var sprite in poses.walk.Concat(new[] { poses.turn, poses.seated, poses.listening, poses.reaction }))
            {
                Require(sprite != null && sprite.pixelsPerUnit == 32 && sprite.pivot == new Vector2(48, 0), "Missing pose or unstable pixel anchor.");
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                Require(importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                    importer.textureCompression == TextureImporterCompression.Uncompressed, "Motion import drift.");
            }
        }
        SURLHWAPresentationValidation.RunCharacters();
        Debug.Log("[Phase N References] PASS: " + library.characters.Length + " motion sets; accepted original references retained.");
    }

    [MenuItem("SURLHWA/Validation/Character Motion Fixtures")]
    public static void Run() => Start(false);
    public static void RunSafety() => Start(true);
    internal static bool Safety => SessionState.GetBool(Pending + "Safety", false);
    private static void Start(bool safety)
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Finish the current validation first.");
        Require(!EditorSceneManager.GetActiveScene().isDirty, "Preserve unsaved scene work.");
        References();
        EditorSceneManager.OpenScene("Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity");
        SessionState.SetBool(Pending, true);
        SessionState.SetBool(Pending + "Safety", safety);
        EditorApplication.EnterPlaymode();
    }
    internal static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

internal sealed class SURLHWACharacterMotionRunner : MonoBehaviour
{
    private readonly Stack<IEnumerator> work = new();
    private readonly List<string> samples = new();
    private bool previousBackground;
    private bool runtimeError;
    private void OnEnable() => Application.logMessageReceived += ObserveLog;
    private void OnDisable() => Application.logMessageReceived -= ObserveLog;
    private void ObserveLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeError = true;
    }
    private IEnumerator Start()
    {
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        work.Push(Run());
        bool passed = true;
        while (work.Count > 0)
        {
            object next;
            try
            {
                if (!work.Peek().MoveNext()) { work.Pop(); continue; }
                next = work.Peek().Current;
                if (next is IEnumerator nested) { work.Push(nested); continue; }
            }
            catch (Exception ex) { Debug.LogException(ex); passed = false; break; }
            yield return next;
        }
        Directory.CreateDirectory("Logs/Review/PhaseN");
        passed &= !runtimeError;
        File.WriteAllLines("Logs/Review/PhaseN/" + (SURLHWACharacterMotionValidation.Safety ? "safety" : "motion") + "-samples.tsv", samples);
        Debug.Log("[Phase N Motion] " + (passed ? "PASS" : "FAIL") + ": production-scene fixtures; setup/service/departure callbacks, not pointer or human play evidence.");
        Application.runInBackground = previousBackground;
        EditorApplication.ExitPlaymode();
    }

    private IEnumerator Run()
    {
        Directory.CreateDirectory("Logs/Review/PhaseN");
        var audio = FindFirstObjectByType<DemoAudioController>();
        audio.Sounds.SetMix(.7f, .35f, .6f, false); // Presentation fixture, no preference writes.
        audio.Sounds.Trace = true;
        foreach (var spawner in FindObjectsByType<CustomerSpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
        yield return null;
        GameObject.Find("DemoEntryCanvas")?.SetActive(false);
        if (!SURLHWACharacterMotionValidation.Safety) yield return Bartender();
        var library = Resources.Load<CustomerMotionLibrary>("CustomerMotionLibrary");
        var seat = FindObjectsByType<SeatPoint>(FindObjectsSortMode.None).Single(s => s.name == "SeatPoint_Left");
        foreach (var art in library.characters.Where(p => !SURLHWACharacterMotionValidation.Safety || p.character == "Hamlet"))
            yield return Guest(seat, art.character, false);
        if (library.Find("Mongryong") != null) yield return Guest(seat, "Lovers", false);
        foreach (var other in FindObjectsByType<SeatPoint>(FindObjectsSortMode.None).Where(s => s != seat))
            yield return Guest(other, "Lovers", false);
        yield return Guest(seat, "OrdinaryCoat", true);
        yield return Guest(seat, "OrdinaryCoat", false, true);
        SURLHWACharacterMotionValidation.Require(FindObjectsByType<CustomerMotionPresentation>(FindObjectsSortMode.None).Length == 0,
            "Orphan presentation survived customer removal.");
    }

    private IEnumerator Bartender()
    {
        var bartender = FindFirstObjectByType<BartenderMotionPresentation>();
        var body = bartender.GetComponent<SpriteRenderer>();
        var art = Resources.Load<CustomerMotionLibrary>("CustomerMotionLibrary");
        Vector3 anchor = bartender.transform.position;
        float start = Time.time, deadline = start + bartender.idleCadence + 2;
        while (body.sprite != art.bartenderRest && Time.time < deadline) yield return null;
        SURLHWACharacterMotionValidation.Require(body.sprite == art.bartenderRest, "Bartender hand-rest accent did not occur.");
        float capturedAt = Time.time;
        SURLHWAReviewCapture.Save("PhaseN/Bartender-000", 1280, 720);
        samples.Add("Bartender-000\t0\tIdle\tHandRest\t0");
        while (body.sprite == art.bartenderRest && Time.time < deadline) yield return null;
        SURLHWACharacterMotionValidation.Require(body.sprite != art.bartenderRest && bartender.transform.position == anchor,
            "Bartender idle failed to settle at its original anchor.");
        SURLHWAReviewCapture.Save("PhaseN/Bartender-001", 1280, 720);
        SURLHWAReviewCapture.Save("PhaseN/Bartender-idle", 1920, 1080);
        samples.Add("Bartender-001\t" + (Time.time - capturedAt).ToString("F3") + "\tIdle\tRest\t0");
        deadline = Time.time + bartender.idleCadence + 2;
        while (body.sprite != art.bartenderRest && Time.time < deadline) yield return null;
        SURLHWACharacterMotionValidation.Require(body.sprite == art.bartenderRest, "Bartender idle cadence did not repeat.");
        SURLHWAReviewCapture.Save("PhaseN/Bartender-002", 1280, 720);
        samples.Add("Bartender-002\t" + (Time.time - capturedAt).ToString("F3") + "\tIdle\tHandRest\t0");
    }

    private IEnumerator Guest(SeatPoint seat, string kind, bool fallback, bool missingFrames = false)
    {
        var ui = FindFirstObjectByType<InteractionUIController>();
        typeof(InteractionUIController).GetMethod("ClosePanels", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, new object[] { false });
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_SURLHWA/Prefabs/Customers/General/Customer_A.prefab");
        var spawner = FindFirstObjectByType<CustomerSpawner>();
        var entry = (Transform)typeof(CustomerSpawner).GetField("spawnPoint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spawner);
        var guest = Instantiate(prefab, entry.position, Quaternion.identity).GetComponent<CustomerController>();
        guest.SetUsesPatience(false);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SURLHWAModularBarImport.CharacterRoot + (kind == "Lovers" ? "Chunhyang" : kind) + ".png");
        guest.standSprite = guest.sitSprite = sprite;
        guest.GetComponent<SpriteRenderer>().sprite = sprite; // Fixture identity before its first rendered Start frame.
        if (kind == "Lovers") guest.ConfigureCampaignEncounter(AssetDatabase.LoadAssetAtPath<CampaignEncounterData>("Assets/_SURLHWA/Data/Campaign/LoversReunion.asset"));
        var view = guest.GetComponent<CustomerMotionPresentation>();
        if (fallback) view.enabled = false;
        SURLHWACharacterMotionValidation.Require(seat.Reserve(guest), "Previous guest failed to release seat.");
        guest.AssignSeat(seat);
        if (missingFrames)
        {
            yield return null;
            typeof(CustomerMotionPresentation).GetField("poses", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, null);
        }
        var bartender = FindFirstObjectByType<BartenderMotionPresentation>();
        Vector3 bartenderRoot = bartender.transform.position;
        var companion = guest.transform.Find("Mongryong companion");
        var stool = guest.transform.Find("Companion stool");
        string label = kind + (seat.name == "SeatPoint_Left" ? "" : "-" + seat.name) +
            (fallback ? "-no-presenter" : missingFrames ? "-missing-frames" : "");
        var audio = FindFirstObjectByType<DemoAudioController>();
        int footsteps = 0, seatCues = 0, standCues = 0, serviceCues = 0;
        bool idleStep = false;
        Action<BarSound, bool, string> cueObserver = (cue, begins, channel) => {
            if (!begins) return;
            if (cue == BarSound.FootLeft || cue == BarSound.FootRight)
            {
                footsteps++;
                idleStep |= guest != null && guest.CurrentState != CustomerController.State.MovingToSeat && guest.CurrentState != CustomerController.State.Leaving;
            }
            if (cue == BarSound.Seat) seatCues++;
            if (cue == BarSound.Stand) standCues++;
            if (cue == BarSound.GlassSet) serviceCues++;
        };
        audio.Sounds.CueChanged += cueObserver;
        var seen = new HashSet<CustomerController.State>();
        var frames = new HashSet<int>();
        float start = Time.time, captureAt = start, readyAt = -1, serviceAt = -1;
        int index = 0;
        Vector3 seatedRoot = Vector3.zero;
        bool sawReaction = false, sawBartender = false, returnedIdle = false;
        float lastDepartureX = 0;
        while (guest != null)
        {
            SURLHWACharacterMotionValidation.Require(Time.time - start < 22, "Motion fixture timed out: " + label);
            seen.Add(guest.CurrentState);
            if (guest.CurrentState == CustomerController.State.Leaving) lastDepartureX = guest.transform.position.x;
            SURLHWACharacterMotionValidation.Require(bartender.transform.position == bartenderRoot, "Bartender presentation moved its root.");
            if (serviceAt > 0 && !fallback)
            {
                sawReaction |= view.PresentedPose == "ServiceReaction";
                sawBartender |= bartender.GetComponent<SpriteRenderer>().sprite == Resources.Load<CustomerMotionLibrary>("CustomerMotionLibrary").bartenderAcknowledge;
                returnedIdle |= sawReaction && view.PresentedPose == "SeatedIdle";
                if (kind == "Lovers" && view.PresentedPose == "ServiceReaction")
                    SURLHWACharacterMotionValidation.Require(companion.GetComponent<CustomerMotionPresentation>().PresentedPose == "ServiceReaction" &&
                        guest.transform.Find("One shared glass").gameObject.activeSelf, "Shared service lost companion reaction/glass.");
            }
            if (kind == "Lovers" && guest.CurrentState == CustomerController.State.Waiting)
            {
                SURLHWACharacterMotionValidation.Require(companion != null && stool != null &&
                    companion.GetComponent<CustomerMotionPresentation>().Owner == guest &&
                    Mathf.Abs(companion.position.x - guest.transform.position.x - 1.5f) < .001f,
                    "Companion ownership/spacing changed.");
                SURLHWACharacterMotionValidation.Require(companion.GetComponent<SpriteRenderer>().sortingOrder == guest.GetComponent<SpriteRenderer>().sortingOrder + 1,
                    "Companion depth drift.");
            }
            if (guest.CurrentState == CustomerController.State.MovingToSeat && !fallback) frames.Add(view.WalkFrame);
            if (guest.CurrentState == CustomerController.State.Waiting)
            {
                if (readyAt < 0)
                {
                    readyAt = Time.time; seatedRoot = guest.transform.position;
                    yield return null; // Allow the existing UI follower its first LateUpdate.
                    SURLHWAReviewCapture.Save("PhaseN/" + label + "-seated", 1920, 1080);
                }
                SURLHWACharacterMotionValidation.Require(guest.transform.position == seatedRoot, "Presentation moved the seated root.");
                if (Time.time - readyAt > .65f && serviceAt < 0)
                {
                    // Resolved unknown/weak drink fixture. No order, knowledge, or campaign completion is fabricated.
                    guest.OnServedWithReaction(default);
                    serviceAt = Time.time;
                }
                if (serviceAt > 0 && Time.time - serviceAt > 1.2f)
                {
                    // Shorten narrative waiting only in this visual fixture. Full campaign tests the real reading/departure path.
                    guest.StopAllCoroutines();
                    typeof(CustomerController).GetMethod("BeginDeparture", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(guest, null);
                }
            }
            if (!fallback && !missingFrames && !SURLHWACharacterMotionValidation.Safety && Time.time >= captureAt)
            {
                string file = label + "-" + index.ToString("D3");
                SURLHWAReviewCapture.Save("PhaseN/" + file, 1280, 720);
                samples.Add(file + "\t" + (Time.time - start).ToString("F3") + "\t" + guest.CurrentState + "\t" + view.PresentedPose + "\t" + view.WalkFrame);
                captureAt = Time.time + .2f; index++;
            }
            yield return null;
        }
        foreach (var state in new[] { CustomerController.State.Turning, CustomerController.State.Sitting,
            CustomerController.State.Waiting, CustomerController.State.Standing, CustomerController.State.Leaving })
            SURLHWACharacterMotionValidation.Require(seen.Contains(state), "Missing lifecycle state: " + label + "/" + state);
        SURLHWACharacterMotionValidation.Require(fallback || frames.Count == 4, "Walking stayed on a static frame: " + label);
        SURLHWACharacterMotionValidation.Require(fallback || (sawReaction && sawBartender && returnedIdle), "Service acknowledgment/return missing: " + label);
        SURLHWACharacterMotionValidation.Require(!seat.IsOccupied && seat.Occupant == null, "Seat not released: " + label);
        SURLHWACharacterMotionValidation.Require(Mathf.Abs(lastDepartureX) > 12,
            "Guest/companion must clear the 16:9 camera before destruction: " + label);
        yield return null;
        SURLHWACharacterMotionValidation.Require(companion == null && stool == null, "Companion visual leaked after departure.");
        audio.Sounds.CueChanged -= cueObserver;
        if (!fallback)
            SURLHWACharacterMotionValidation.Require(footsteps > 0 && !idleStep && seatCues == 1 && standCues == 1 && serviceCues == 1,
                "Audio lifecycle/paired shared service mismatch: " + label + $" steps={footsteps} idleStep={idleStep} seat={seatCues} stand={standCues} service={serviceCues}");
        SURLHWACharacterMotionValidation.Require(FindObjectsByType<CustomerFoleyPresentation>(FindObjectsSortMode.None).Length == 0,
            "Foley observer leaked after departure.");
        Debug.Log($"[Phase O Character] PASS: {label} steps={footsteps} seatedSteps={idleStep} seat={seatCues} stand={standCues} service={serviceCues}; sources={audio.GetComponents<AudioSource>().Length}");
    }
}
#endif
