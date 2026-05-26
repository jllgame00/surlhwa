using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShakeMiniGame : MonoBehaviour, IMiniGame
{
    public enum ShakeState { Preview, Shaking, Result }
    public enum PrepDirection { None, Left, Right }

    [Header("UI")]
    [SerializeField] private RectTransform shaker;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text resultLabel;

    [Header("Start UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startHintText;

    [Header("Rules")]
    [SerializeField] private int requiredSuccessCount = 6;
    [SerializeField] private float timeLimit = 6f;
    [SerializeField] private float prepareTimeout = 0.8f;
    [SerializeField] private int maxMistakes = 3;

    [Header("Motion")]
    [SerializeField] private float prepareOffsetX = 30f;
    [SerializeField] private float shakeMoveX = 80f;
    [SerializeField] private float shakeDuration = 0.08f;
    [SerializeField] private float returnDuration = 0.08f;

    private ShakeState state = ShakeState.Preview;
    private PrepDirection preparedDirection = PrepDirection.None;
    private PrepDirection lastExecutedDirection = PrepDirection.None;

    private float remainTime;
    private float prepareRemainTime;

    private int successCount = 0;
    private int mistakeCount = 0;

    private Vector2 shakerOrigin;
    private bool isAnimating = false;

    private Action<MiniGameResult> onDone;

    [Header("Recipe Guide")]
    [SerializeField] private RecipeGuidePanelController recipeGuide;

    public void StartGame(OrderData order, Action<MiniGameResult> callback)
    {
        onDone = callback;
        AutoBindIfMissing();

        if (recipeGuide == null)
            recipeGuide = GetComponentInChildren<RecipeGuidePanelController>(true);

        state = ShakeState.Preview;
        preparedDirection = PrepDirection.None;
        lastExecutedDirection = PrepDirection.None;

        remainTime = timeLimit;
        prepareRemainTime = 0f;
        successCount = 0;
        mistakeCount = 0;
        isAnimating = false;

        if (shaker != null)
            shakerOrigin = shaker.anchoredPosition;

        if (resultLabel != null)
            resultLabel.text = "";

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(true);
            startHintText.text = "레시피를 확인한 뒤 시작하세요";
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(BeginGameplay);
        }

        UpdateProgressText();
        SetHint("시작 버튼을 눌러 쉐이크를 시작하세요");

        Debug.Log(
            $"[ShakeMiniGame] Preview ready / " +
            $"startButton={(startButton != null ? startButton.name : "NULL")} / " +
            $"startHintText={(startHintText != null ? startHintText.name : "NULL")}"
        );
    }

    public void BeginGameplay()
    {
        Debug.Log("[ShakeMiniGame] BeginGameplay clicked");

        if (state != ShakeState.Preview)
        {
            Debug.LogWarning($"[ShakeMiniGame] Begin ignored. state={state}");
            return;
        }

        state = ShakeState.Shaking;

        if (startHintText != null)
            startHintText.gameObject.SetActive(false);

        if (startButton != null)
            startButton.gameObject.SetActive(false);

        if (resultLabel != null)
            resultLabel.text = "";

        SetHint("← 또는 → 로 준비");

        Debug.Log("[ShakeMiniGame] BeginGameplay -> Shaking");
    }

    private void Update()
    {
        if (state == ShakeState.Preview)
            return;

        if (state != ShakeState.Shaking)
            return;

        remainTime -= Time.deltaTime;
        if (remainTime <= 0f)
        {
            ResolveFail("FAIL");
            return;
        }

        if (preparedDirection != PrepDirection.None)
        {
            prepareRemainTime -= Time.deltaTime;
            if (prepareRemainTime <= 0f)
            {
                preparedDirection = PrepDirection.None;
                ResetShakerToOrigin();
                SetHint("준비 시간 초과!");
                RegisterMistake();
            }
        }

        HandlePrepareInput();
        HandleExecuteInput();
    }

    private void HandlePrepareInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            Prepare(PrepDirection.Left);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            Prepare(PrepDirection.Right);
    }

    private void HandleExecuteInput()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (isAnimating) return;

        if (preparedDirection == PrepDirection.None)
        {
            SetHint("방향 먼저 준비!");
            RegisterMistake();
            return;
        }

        ExecuteShake(preparedDirection);
    }

    private void Prepare(PrepDirection dir)
    {
        preparedDirection = dir;
        prepareRemainTime = prepareTimeout;

        if (shaker != null)
        {
            float offsetX = dir == PrepDirection.Left ? -prepareOffsetX : prepareOffsetX;
            shaker.anchoredPosition = shakerOrigin + new Vector2(offsetX, 0f);
        }

        SetHint(dir == PrepDirection.Left ? "왼쪽 준비" : "오른쪽 준비");
    }

    private void ExecuteShake(PrepDirection dir)
    {
        bool isFirst = lastExecutedDirection == PrepDirection.None;
        bool alternated = lastExecutedDirection != dir;

        if (!isFirst && !alternated)
        {
            SetHint("반대 방향으로 흔들어!");
            preparedDirection = PrepDirection.None;
            ResetShakerToOrigin();
            RegisterMistake();
            return;
        }

        successCount++;
        lastExecutedDirection = dir;
        preparedDirection = PrepDirection.None;

        UpdateProgressText();
        SetHint(dir == PrepDirection.Left ? "왼쪽 쉐이크!" : "오른쪽 쉐이크!");

        StartCoroutine(PlayShakeMotion(dir));

        if (successCount >= requiredSuccessCount)
            ResolveSuccess();
    }

    private IEnumerator PlayShakeMotion(PrepDirection dir)
    {
        isAnimating = true;

        if (shaker != null)
        {
            Vector2 start = shaker.anchoredPosition;
            float moveX = dir == PrepDirection.Left ? -shakeMoveX : shakeMoveX;
            Vector2 target = shakerOrigin + new Vector2(moveX, 0f);

            yield return LerpAnchoredPos(start, target, shakeDuration);
            yield return LerpAnchoredPos(target, shakerOrigin, returnDuration);
        }

        isAnimating = false;
    }

    private IEnumerator LerpAnchoredPos(Vector2 from, Vector2 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            if (shaker != null)
                shaker.anchoredPosition = Vector2.Lerp(from, to, p);
            yield return null;
        }

        if (shaker != null)
            shaker.anchoredPosition = to;
    }

    private void RegisterMistake()
    {
        mistakeCount++;
        if (mistakeCount >= maxMistakes)
            ResolveFail("FAIL");
        else
            UpdateProgressText();
    }

    private void ResolveSuccess()
    {
        state = ShakeState.Result;

        string grade = mistakeCount == 0 ? "Perfect" : "Good";
        int repDelta = mistakeCount == 0 ? 2 : 1;

        if (resultLabel != null)
            resultLabel.text = grade;

        StartCoroutine(FinishAfterDelay(new MiniGameResult
        {
            success = true,
            repDelta = repDelta,
            grade = grade,
            brewedComposition = CreateBrewComposition()
        }));
    }

    private void ResolveFail(string label)
    {
        state = ShakeState.Result;

        if (resultLabel != null)
            resultLabel.text = label;

        StartCoroutine(FinishAfterDelay(new MiniGameResult
        {
            success = false,
            repDelta = -1,
            grade = "Fail",
            brewedComposition = CreateBrewComposition()
        }));
    }

    private IEnumerator FinishAfterDelay(MiniGameResult result)
    {
        yield return new WaitForSeconds(1f);
        onDone?.Invoke(result);
    }

    private BrewComposition CreateBrewComposition()
    {
        return new BrewComposition
        {
            method = MiniGameType.Shake,
            ingredients = new[]
            {
            new BrewIngredientEntry { ingredientId = "rum", amount = 2f },
            new BrewIngredientEntry { ingredientId = "lime", amount = 1f },
            new BrewIngredientEntry { ingredientId = "syrup", amount = 1f }
        }
        };
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
            progressText.text = $"Shake {successCount}/{requiredSuccessCount}  Mistake {mistakeCount}/{maxMistakes}";
    }

    private void SetHint(string msg)
    {
        if (hintText != null)
            hintText.text = msg;
    }

    private void ResetShakerToOrigin()
    {
        if (shaker != null)
            shaker.anchoredPosition = shakerOrigin;
    }

    private void AutoBindIfMissing()
    {
        if (shaker == null)
            shaker = transform.Find("UI/Shaker")?.GetComponent<RectTransform>();

        if (progressText == null)
            progressText = transform.Find("UI/ProgressText")?.GetComponent<TMP_Text>();

        if (hintText == null)
            hintText = transform.Find("UI/HintText")?.GetComponent<TMP_Text>();

        if (resultLabel == null)
            resultLabel = transform.Find("UI/ResultLabel")?.GetComponent<TMP_Text>();

        if (startButton == null)
            startButton = transform.Find("UI/StartButton")?.GetComponent<Button>();

        if (startButton == null)
            startButton = transform.Find("UI/StartupButton")?.GetComponent<Button>();

        if (startHintText == null)
            startHintText = transform.Find("UI/StartHintText")?.GetComponent<TMP_Text>();
    }
}