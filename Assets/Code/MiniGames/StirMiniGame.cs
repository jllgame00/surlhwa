using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class StirMiniGame : MonoBehaviour, IMiniGame
{
    enum State { Preview, Stirring, Result }
    State state = State.Preview;

    [Header("Gauge Track (UI)")]
    public RectTransform gaugeBGRect;

    [Header("Zones (UI)")]
    public RectTransform targetZone;
    public RectTransform perfectZone;

    [Header("Marker (UI)")]
    public RectTransform marker;

    [Header("Cup / Spoon (UI)")]
    public RectTransform cup;
    public RectTransform spoon;
    public RectTransform spoonInCup;

    [Header("Text (UI)")]
    public TMP_Text resultLabel;

    [Header("Start UI")]
    public Button startButton;
    public TMP_Text startHintText;

    [Header("Timing")]
    public float fillDuration = 3.0f;
    public float resultHold = 0.8f;

    [Header("Spoon Orbit (Stir Motion)")]
    public RectTransform orbitCenter;
    public float orbitRadius = 16f;
    public float orbitSpeed = 7f;
    public float orbitYScale = 0.65f;
    public float wobbleDeg = 10f;

    [Header("Spoon Pose")]
    public float spoonBaseAngleDeg = 90f;

    private OrderData currentOrder;

    float t;
    float targetMin, targetMax;
    float perfectMin, perfectMax;
    float orbitAngle;

    Action<MiniGameResult> onDone;

    [Header("Recipe Guide")]
    [SerializeField] private RecipeGuidePanelController recipeGuide;

    private BrewSessionData currentBrewSession;

    [Header("Ingredient Shelf")]
    [SerializeField] private IngredientShelfController ingredientShelf;

    [Header("Jigger Panel")]
    [SerializeField] private JiggerPanelController jiggerPanel;

    public void StartGame(OrderData order, Action<MiniGameResult> done)
    {
        onDone = done;
        AutoBindIfMissing();

        currentOrder = order;
        currentBrewSession = null;

        if (ingredientShelf == null)
            ingredientShelf = GetComponentInChildren<IngredientShelfController>(true);

        if (ingredientShelf == null)
            throw new InvalidOperationException("[StirMiniGame] IngredientShelfController is required.");

        ingredientShelf.Initialize(MiniGameType.Stir);

        if (jiggerPanel == null)
            jiggerPanel = GetComponentInChildren<JiggerPanelController>(true);

        if (jiggerPanel != null)
        {
            jiggerPanel.Initialize(ingredientShelf);
            ingredientShelf.SetJiggerPanel(jiggerPanel);
            jiggerPanel.gameObject.SetActive(true);
        }

        if (recipeGuide == null)
            recipeGuide = GetComponentInChildren<RecipeGuidePanelController>(true);


        state = State.Preview;
        t = 0f;
        orbitAngle = 0f;

        if (resultLabel != null)
        {
            resultLabel.gameObject.SetActive(true);
            resultLabel.text = "";
        }

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(true);
            startHintText.text = "Check the recipe, then start.";
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(BeginGameplay);
        }

        float center = UnityEngine.Random.Range(0.55f, 0.95f);
        targetMin = Mathf.Clamp01(center - 0.08f);
        targetMax = Mathf.Clamp01(center + 0.08f);
        perfectMin = Mathf.Clamp01(center - 0.03f);
        perfectMax = Mathf.Clamp01(center + 0.03f);

        PlaceZone(targetZone, targetMin, targetMax);
        PlaceZone(perfectZone, perfectMin, perfectMax);

        UpdateMarker();

        if (orbitCenter == null) orbitCenter = spoonInCup != null ? spoonInCup : cup;

        if (spoon != null && spoonInCup != null)
        {
            spoon.anchoredPosition = spoonInCup.anchoredPosition;
            spoon.localRotation = Quaternion.Euler(0f, 0f, spoonBaseAngleDeg);
        }

        Debug.Log(
            $"[StirMiniGame] Preview ready / " +
            $"BrewSession={(currentBrewSession != null ? currentBrewSession.method.ToString() : "NULL")} / " +
            $"startButton={(startButton != null ? startButton.name : "NULL")} / " +
            $"startHintText={(startHintText != null ? startHintText.name : "NULL")}"
        );
    }

    public void BeginGameplay()
    {
        Debug.Log("[StirMiniGame] BeginGameplay clicked");

        if (state != State.Preview)
        {
            Debug.LogWarning($"[StirMiniGame] Begin ignored. state={state}");
            return;
        }

        if (!ingredientShelf.HasAnySelectedIngredient())
        {
            SetHint("Pour at least one ingredient first.");
            Debug.LogWarning("[StirMiniGame] Cannot start. No ingredients poured.");
            return;
        }

        currentBrewSession = ingredientShelf.BuildSession();

        Debug.Log(
            $"[{nameof(StirMiniGame)}] BrewSession built from ingredient shelf / " +
            $"Method={currentBrewSession.method} / " +
            $"Count={(currentBrewSession.ingredients != null ? currentBrewSession.ingredients.Length : 0)}"
        );

        ingredientShelf.SetVisible(false);

        if (jiggerPanel != null)
            jiggerPanel.gameObject.SetActive(false);

        state = State.Stirring;

        if (resultLabel != null)
            resultLabel.text = "";

        if (startHintText != null)
            startHintText.gameObject.SetActive(false);

        if (startButton != null)
            startButton.gameObject.SetActive(false);

        Debug.Log("[StirMiniGame] BeginGameplay -> Stirring");
    }

    void Update()
    {
        if (state == State.Preview)
            return;

        if (state == State.Stirring)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, fillDuration);

            if (t >= 1f)
            {
                t = 1f;
                UpdateMarker();
                ResolveTimeoutFail();
                return;
            }

            UpdateMarker();
            OrbitSpoon();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ResolveByStop();
            }
        }
    }

    void UpdateMarker()
    {
        if (marker == null || gaugeBGRect == null) return;

        float trackHeight = gaugeBGRect.rect.height;
        float markerHalf = marker.rect.height * 0.5f;

        float yMin = markerHalf;
        float yMax = trackHeight - markerHalf;
        float y = Mathf.Lerp(yMin, yMax, t);

        Vector2 pos = marker.anchoredPosition;
        pos.y = y;
        marker.anchoredPosition = pos;
    }

    void OrbitSpoon()
    {
        if (spoon == null || orbitCenter == null) return;

        orbitAngle += orbitSpeed * Time.deltaTime;

        float x = Mathf.Cos(orbitAngle) * orbitRadius;
        float y = Mathf.Sin(orbitAngle) * (orbitRadius * orbitYScale);

        spoon.anchoredPosition = orbitCenter.anchoredPosition + new Vector2(x, y);

        float wobble = Mathf.Sin(orbitAngle * 2f) * wobbleDeg;
        spoon.localRotation = Quaternion.Euler(0f, 0f, spoonBaseAngleDeg + wobble);
    }

    void ResolveByStop()
    {
        state = State.Result;

        bool perfect = t >= perfectMin && t <= perfectMax;
        bool good = t >= targetMin && t <= targetMax;

        MiniGameResult result = new MiniGameResult
        {
            success = good || perfect,
            repDelta = perfect ? 2 : (good ? 1 : 0),
            grade = perfect ? "Perfect" : (good ? "Good" : "Fail"),
            brewedComposition = CreateBrewComposition()
        };

        ShowResult(result.grade);
        StartCoroutine(FinishAfterDelay(result, resultHold));
    }

    void ResolveTimeoutFail()
    {
        state = State.Result;

        MiniGameResult result = new MiniGameResult
        {
            success = false,
            repDelta = 0,
            grade = "Fail",
            brewedComposition = CreateBrewComposition()
        };

        ShowResult("Fail");
        StartCoroutine(FinishAfterDelay(result, resultHold));
    }

    IEnumerator FinishAfterDelay(MiniGameResult result, float delay)
    {
        yield return new WaitForSeconds(delay);
        onDone?.Invoke(result);
    }

    private BrewComposition CreateBrewComposition()
    {
        return BrewCompositionFactory.FromSession(
            currentBrewSession,
            MiniGameType.Stir
        );
    }

    void ShowResult(string text)
    {
        if (resultLabel == null) return;
        resultLabel.text = text;
        resultLabel.gameObject.SetActive(true);
    }

    private void SetHint(string msg)
    {
        if (startHintText != null)
            startHintText.text = msg;
    }

    void PlaceZone(RectTransform zone, float min, float max)
    {
        if (zone == null) return;

        zone.anchorMin = new Vector2(0f, min);
        zone.anchorMax = new Vector2(1f, max);
        zone.offsetMin = Vector2.zero;
        zone.offsetMax = Vector2.zero;
    }

    void AutoBindIfMissing()
    {
        if (gaugeBGRect == null)
            gaugeBGRect = transform.Find("UI/GaugeRoot/GaugeBG")?.GetComponent<RectTransform>();

        if (marker == null)
            marker = transform.Find("UI/GaugeRoot/GaugeBG/GaugeFill")?.GetComponent<RectTransform>();

        if (targetZone == null)
            targetZone = transform.Find("UI/GaugeRoot/GaugeBG/TargetZone")?.GetComponent<RectTransform>();

        if (perfectZone == null)
            perfectZone = transform.Find("UI/GaugeRoot/GaugeBG/PerfectZone")?.GetComponent<RectTransform>();

        if (resultLabel == null)
            resultLabel = transform.Find("UI/ResultLabel")?.GetComponent<TMP_Text>();

        if (startButton == null)
            startButton = transform.Find("UI/StartButton")?.GetComponent<Button>();

        if (startButton == null)
            startButton = transform.Find("UI/StartupButton")?.GetComponent<Button>();

        if (startHintText == null)
            startHintText = transform.Find("UI/StartHintText")?.GetComponent<TMP_Text>();

        if (cup == null)
            cup = transform.Find("UI/Cup")?.GetComponent<RectTransform>();

        if (spoon == null)
            spoon = transform.Find("UI/Spoon")?.GetComponent<RectTransform>();

        if (spoonInCup == null)
            spoonInCup = transform.Find("UI/SpoonInCup")?.GetComponent<RectTransform>();
    }
}
