using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CustomerController : MonoBehaviour
{
    private const string FirstRouteHintLine =
        "밖은 생각보다 조용하네요.\n" +
        "차가운 밤을 조금 데워줄 수 있는 한 잔이 있을까요?\n\n" +
        "혼자 남겨진 듯한 맛.\n" +
        "그리고, 아주 조금은 따뜻한 끝맛이면 좋겠어요.\n\n" +
        "레시피북을 펼치면, 첫 장에 적힌 조주법을 확인할 수 있습니다.";

    private const string FirstRouteSuccessImmediateLine = "따뜻하네요.";
    private const string FirstRouteSuccessExitLine = "눈이 조금은 늦게 녹는 것 같아요.";
    private const string FirstRouteUnknownSuccessLine = "낯선 맛이지만, 마음은 전해졌어요.";
    private const string FirstRouteWeakLine = "아직은 조금 흔들리는 맛이네요.";

    private const string SecondRouteHintLine =
        "잠에서 덜 깬 것처럼, 머릿속이 흐릿해요.\n" +
        "조금은 선명하게 깨워줄 한 잔이 있을까요?\n\n" +
        "잠에서 깨어나는 듯한 선명함.\n" +
        "그리고, 흐리지 않고 맑게 정리되는 끝맛이면 좋겠어요.";

    private const string SecondRouteSuccessImmediateLine = "조금 맑아졌어요.";
    private const string SecondRouteSuccessExitLine = "눈송이 사이로 길이 보이는 것 같네요.";
    private const string SecondRouteUnknownSuccessLine = "낯선 맛이지만, 어딘가 정신이 맑아지는 느낌이에요.";
    private const string SecondRouteWeakLine = "아직은 흐릿한 밤이 남아 있는 것 같아요.";

    private const string ThirdRouteHintLine =
        "조금 늦게까지 남아 있는 마음이 있어요.\n" +
        "가볍게 사라지기보다는, 잔 안에 천천히 쌓이는 맛이면 좋겠어요.\n" +
        "씁쓸하지만 너무 차갑지는 않은 끝맛을 원해요.";

    private const string ThirdRouteSuccessImmediateLine = "조용히 가라앉네요.";
    private const string ThirdRouteSuccessExitLine = "남아 있던 마음이 조금은 정리된 것 같아요.";
    private const string ThirdRouteUnknownSuccessLine = "낯선 맛이지만, 오래 남는 느낌은 나쁘지 않네요.";
    private const string ThirdRouteWeakLine = "아직은 마음이 조금 흩어지는 것 같아요.";

    private const string FourthRouteHintLine =
        "멀리 있는 무언가가 자꾸 떠올라요.\n" +
        "가라앉히기보다는, 조용히 위에 띄워두고 싶어요.\n" +
        "맑지만 조금은 그리운 끝맛이면 좋겠어요.";

    private const string FourthRouteSuccessImmediateLine = "떠오르네요.";
    private const string FourthRouteSuccessExitLine = "가라앉지 않고, 조용히 남아 있어요.";
    private const string FourthRouteUnknownSuccessLine = "낯선 맛이지만, 어딘가 오래 바라보게 되네요.";
    private const string FourthRouteWeakLine = "아직은 마음이 잔 아래로 가라앉는 것 같아요.";

    public float moveSpeed = 2.0f;
    public Vector2 arriveThreshold = new Vector2(0.05f, 0.05f);

    private enum State { Spawning, MovingToSeat, Sitting, Ordering, Waiting, Leaving }
    private State state = State.Spawning;

    private SeatPoint targetSeat;
    private SpriteRenderer sr;

    public Sprite standSprite;
    public Sprite sitSprite;

    // External references
    private SeatManager seatManager;

    // Exit destination
    public Transform defaultExitPoint;

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private GameObject dialogueBubblePrefab;

    [Header("Order")]
    [SerializeField] private OrderData orderData;

    private GameObject currentDialogueBubble;

    [Header("Emotion Reaction")]
    [SerializeField] private GuestReactionDatabase reactionDatabase;
    [SerializeField] private GuestEmotionRequest emotionRequest = new GuestEmotionRequest();
    [SerializeField] private Transform reactionAnchor;
    [SerializeField] private GameObject heartPopupPrefab;

    private static EmotionTag lastGeneralGuestPrimaryTag = EmotionTag.Loneliness;

    [Header("Interaction")]
    [SerializeField] private Transform interactionAnchor;
    private InteractionUIController interactionUI;
    private CustomerSpawner loopManager;

    [Header("Patience")]
    [SerializeField] private float patienceMin = 10f;
    [SerializeField] private float patienceMax = 15f;
    [SerializeField] private int repPenaltyOnTimeout = 1;
    [SerializeField] private string timeoutLine = "...";
    [SerializeField]
    private string[] timeoutLines =
    {
        "기다림이 조금 길었네요.",
        "마음이 식기 전에 마시고 싶었는데요.",
        "오늘은 이만 돌아가볼게요.",
        "잔이 비어 있는 시간이 너무 길었어요.",
        "다음에는 조금 더 빠르게 만날 수 있으면 좋겠어요."
    };
    [SerializeField] private string timeoutToastTitle = "평판 하락";
    [SerializeField] private string timeoutToastMessageFormat = "손님이 오래 기다리다 자리를 떠났습니다. Rep -{0}";

    private GameManager gm;
    private BartendingFlowCoordinator bartendingFlowCoordinator;
    private ResultToastController resultToast;
    private bool usesPatience;
    private bool patienceTimerActive;
    private float patienceDuration;
    private float patienceRemaining;
    
    [Header("Unknown Cocktail Reactions")]
    [SerializeField]
    private string[] unknownSuccessLines =
    {
        "이름은 모르겠지만, 묘하게 남는 맛이네요.",
        "낯선 맛이지만 이상하게 기억에 남네요.",
        "아직 완성되진 않았지만 무언가 닿는 느낌이 있어요.",
        "첫맛보다 뒤에 남는 여운이 더 좋네요."
    };

    [SerializeField]
    private string[] unknownFailLines =
    {
        "아직은 조금 흔들리는 맛이네요.",
        "오늘 원하는 느낌과는 조금 거리가 있는 것 같아요.",
        "무언가 닿긴 했지만 마음이 편해지진 않네요.",
        "입안에 남는 맛이 아직 어색하네요."
    };

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (standSprite != null)
            sr.sprite = standSprite;

        seatManager = FindFirstObjectByType<SeatManager>();
        interactionUI = FindFirstObjectByType<InteractionUIController>();
        gm = FindFirstObjectByType<GameManager>();
        bartendingFlowCoordinator = FindFirstObjectByType<BartendingFlowCoordinator>();
        resultToast = FindFirstObjectByType<ResultToastController>();
    }

    private void OnDisable()
    {
        StopPatienceTimer();

        if (interactionUI != null)
            interactionUI.ClearCustomer(this);
    }

    public void SetLoopManager(CustomerSpawner manager)
    {
        loopManager = manager;
    }

    public void SetUsesPatience(bool value)
    {
        usesPatience = value;
    }

    public void AssignSeat(SeatPoint seat)
    {
        targetSeat = seat;

        if (targetSeat == null)
        {
            state = State.Leaving;
            return;
        }

        state = State.MovingToSeat;
    }

    public void AssignRandomEmotionForGeneralGuest()
    {
        GeneralGuestEmotionGenerator.FillRandom(emotionRequest, lastGeneralGuestPrimaryTag);
        lastGeneralGuestPrimaryTag = emotionRequest.primaryTag;

        Debug.Log($"[Customer] Emotion Assigned -> {emotionRequest.primaryTag} + {emotionRequest.secondaryTag}");
    }

    public void ForceEmotionRequestForDebug(EmotionTag primaryTag, EmotionTag secondaryTag)
    {
        if (emotionRequest == null)
            emotionRequest = new GuestEmotionRequest();

        emotionRequest.Set(primaryTag, secondaryTag);
        lastGeneralGuestPrimaryTag = primaryTag;

        Debug.Log($"[Customer] Debug Emotion Forced -> {primaryTag} + {secondaryTag}");
    }

    public bool IsInForcedFirstCustomerRoute()
    {
        if (interactionUI == null)
            interactionUI = FindFirstObjectByType<InteractionUIController>();

        return interactionUI != null && interactionUI.IsForcedFirstCustomerRouteActive;
    }

    public bool IsInForcedSecondCustomerRoute()
    {
        if (interactionUI == null)
            interactionUI = FindFirstObjectByType<InteractionUIController>();

        return interactionUI != null && interactionUI.IsForcedSecondCustomerRouteActive;
    }

    public bool IsInForcedThirdCustomerRoute()
    {
        if (interactionUI == null)
            interactionUI = FindFirstObjectByType<InteractionUIController>();

        return interactionUI != null && interactionUI.IsForcedThirdCustomerRouteActive;
    }

    public bool IsInForcedFourthCustomerRoute()
    {
        if (interactionUI == null)
            interactionUI = FindFirstObjectByType<InteractionUIController>();

        return interactionUI != null && interactionUI.IsForcedFourthCustomerRouteActive;
    }

    void Update()
    {
        switch (state)
        {
            case State.MovingToSeat:
                {
                    Vector3 dest = (targetSeat != null && targetSeat.sitMarker != null)
                        ? targetSeat.sitMarker.position
                        : (targetSeat != null ? targetSeat.transform.position : transform.position);

                    MoveToward(dest, onArrive: SitDown);
                    break;
                }

            case State.Sitting:
                state = State.Ordering;
                break;

            case State.Ordering:
                Debug.Log($"[Customer] Ordering reached: {name}");

                if (interactionUI == null)
                {
                    interactionUI = FindFirstObjectByType<InteractionUIController>();
                    Debug.Log($"[Customer] Retry find InteractionUI = {(interactionUI != null ? interactionUI.name : "NULL")}");
                }

                if (interactionUI != null)
                {
                    Debug.Log("[Customer] RegisterCustomer called");
                    interactionUI.RegisterCustomer(this);
                }
                else
                {
                    Debug.LogWarning("[Customer] InteractionUIController not found");
                }

                EnterWaitingState();
                break;

            case State.Waiting:
                // InteractionUIController handles DialoguePanel -> BrewPanel -> Confirm.
                // Confirm starts OrderManager.PlaySpecificOrder(); completion calls OnServedWithReaction().
                UpdatePatienceTimer();
                break;

            case State.Leaving:
                {
                    Vector3 dest = GetExitDestination();
                    MoveToward(dest, onArrive: LeaveAndDestroy);
                    break;
                }
        }

        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
    }

    void MoveToward(Vector3 dest, System.Action onArrive)
    {
        Vector3 dir = dest - transform.position;
        float step = moveSpeed * Time.deltaTime;

        if (dir.magnitude <= step ||
            (Mathf.Abs(dir.x) < arriveThreshold.x && Mathf.Abs(dir.y) < arriveThreshold.y))
        {
            transform.position = dest;
            onArrive?.Invoke();
            return;
        }

        transform.position += dir.normalized * step;

        if (dir.x != 0)
            transform.localScale = new Vector3(dir.x > 0 ? 1 : -1, 1, 1);
    }

    void SitDown()
    {
        state = State.Sitting;

        if (targetSeat != null && targetSeat.sitMarker != null)
            transform.position = targetSeat.sitMarker.position;

        if (sitSprite != null)
            sr.sprite = sitSprite;

        AssignRandomEmotionForGeneralGuest();

        // Auto entry bubbles stay disabled to avoid overlapping the current UI flow.
        // SayRandom(dialogueData != null ? dialogueData.onEnterLines : null);
    }

    Vector3 GetExitDestination()
    {
        if (targetSeat != null && targetSeat.exitMarker != null)
            return targetSeat.exitMarker.position;

        if (defaultExitPoint != null)
            return defaultExitPoint.position;

        return transform.position + Vector3.left * 3f;
    }

    private void EnterWaitingState()
    {
        state = State.Waiting;

        if (usesPatience)
            StartPatienceTimer();
        else if (interactionUI != null)
            interactionUI.SetCustomerWaitUrgency(this, 1f);
    }

    private void StartPatienceTimer()
    {
        float min = Mathf.Max(0.1f, Mathf.Min(patienceMin, patienceMax));
        float max = Mathf.Max(min, Mathf.Max(patienceMin, patienceMax));

        patienceDuration = Random.Range(min, max);
        patienceRemaining = patienceDuration;
        patienceTimerActive = true;

        if (interactionUI != null)
        {
            interactionUI.SetCustomerWaitIndicatorVisible(this, true);
            interactionUI.SetCustomerWaitUrgency(this, 1f);
        }
    }

    private void StopPatienceTimer()
    {
        patienceTimerActive = false;

        if (interactionUI != null)
            interactionUI.SetCustomerWaitIndicatorVisible(this, false);
    }

    private void UpdatePatienceTimer()
    {
        if (!patienceTimerActive)
            return;

        if (bartendingFlowCoordinator != null && bartendingFlowCoordinator.IsBartendingActive)
            return;

        patienceRemaining -= Time.deltaTime;

        if (interactionUI != null)
            interactionUI.SetCustomerWaitUrgency(this, GetPatienceRemainingNormalized());

        if (patienceRemaining <= 0f)
            TimeoutAndLeave();
    }

    private float GetPatienceRemainingNormalized()
    {
        if (patienceDuration <= 0f)
            return 0f;

        return Mathf.Clamp01(patienceRemaining / patienceDuration);
    }

    private void TimeoutAndLeave()
    {
        patienceTimerActive = false;

        if (interactionUI != null)
        {
            interactionUI.SetCustomerWaitIndicatorVisible(this, false);
            interactionUI.ClearCustomer(this);
        }

        if (gm == null)
            gm = FindFirstObjectByType<GameManager>();

        int penalty = Mathf.Abs(repPenaltyOnTimeout);
        if (gm != null && penalty > 0)
            gm.AddReputation(-penalty);

        ShowTimeoutReputationFeedback(penalty);

        StopAllCoroutines();
        StartCoroutine(TimeoutReactionThenLeave());
    }

    private IEnumerator TimeoutReactionThenLeave()
    {
        ClearDialogueBubble();
        ShowReactionBubble(GetRandomTimeoutLine());

        yield return new WaitForSeconds(1.2f);

        ClearDialogueBubble();
        state = State.Leaving;
    }

    private string GetRandomTimeoutLine()
    {
        if (timeoutLines == null || timeoutLines.Length == 0)
            return timeoutLine;

        string line = timeoutLines[Random.Range(0, timeoutLines.Length)];
        return string.IsNullOrEmpty(line) ? timeoutLine : line;
    }

    private void ShowTimeoutReputationFeedback(int penalty)
    {
        if (penalty <= 0)
            return;

        if (resultToast == null)
            resultToast = FindFirstObjectByType<ResultToastController>();

        if (resultToast == null)
            return;

        string message = string.IsNullOrEmpty(timeoutToastMessageFormat)
            ? $"Rep -{penalty}"
            : timeoutToastMessageFormat.Replace("{0}", penalty.ToString());

        resultToast.Show(timeoutToastTitle, message);
    }

    // Legacy serve path.
    public void OnServed(bool success)
    {
        StopPatienceTimer();

        if (interactionUI != null)
            interactionUI.ClearCustomer(this);

        if (dialogueData != null)
        {
            string[] lines = success ? dialogueData.onSuccessLines : dialogueData.onFailLines;
            SayRandom(lines);
        }

        state = State.Leaving;
    }

    // Kept for older OrderData-based callers.
    public void OnServedWithReaction(OrderData servedOrder, MiniGameResult result)
    {
        CocktailData fallbackCocktail = servedOrder != null
            ? servedOrder.targetCocktail
            : null;

        OnServedWithReaction(fallbackCocktail, result);
    }

    // New path: calculate reaction from the actually served CocktailData.
    public void OnServedWithReaction(CocktailData servedCocktail, MiniGameResult result)
    {
        StopPatienceTimer();

        bool firstCustomerRoute = IsInForcedFirstCustomerRoute();
        bool secondCustomerRoute = IsInForcedSecondCustomerRoute();
        bool thirdCustomerRoute = IsInForcedThirdCustomerRoute();
        bool fourthCustomerRoute = IsInForcedFourthCustomerRoute();

        if (interactionUI != null)
            interactionUI.ClearCustomer(this);

        EmotionTag[] desiredTags = emotionRequest != null
            ? emotionRequest.ToArray()
            : System.Array.Empty<EmotionTag>();

        if (servedCocktail == null)
        {
            int unknownHearts = result.success ? 1 : 0;
            string unknownLine = firstCustomerRoute
                ? GetFirstRouteUnknownReactionLine(result.success)
                : secondCustomerRoute
                    ? GetSecondRouteUnknownReactionLine(result.success)
                    : thirdCustomerRoute
                        ? GetThirdRouteUnknownReactionLine(result.success)
                        : fourthCustomerRoute
                            ? GetFourthRouteUnknownReactionLine(result.success)
                            : GetRandomUnknownReactionLine(result.success);

            Debug.Log(
                $"[Customer] ServedCocktail=NULL / " +
                $"Hearts={unknownHearts}"
            );

            StopAllCoroutines();
            StartCoroutine(ReactionSequenceThenLeave(unknownLine, unknownHearts));
            return;
        }

        EmotionTag[] servedTags = servedCocktail.emotionTags != null
            ? servedCocktail.emotionTags
            : System.Array.Empty<EmotionTag>();

        bool recipeInTolerance = result.success;

        int hearts = GuestReactionResolver.CalculateHearts(
            desiredTags,
            servedTags,
            recipeInTolerance
        );

        Debug.Log(
            $"[Customer] ServedCocktail={(servedCocktail != null ? servedCocktail.name : "NULL")} / " +
            $"Hearts={hearts}"
        );

        StopAllCoroutines();

        if (firstCustomerRoute)
        {
            string immediateLine = result.success ? FirstRouteSuccessImmediateLine : FirstRouteWeakLine;
            string exitLine = result.success ? FirstRouteSuccessExitLine : null;
            StartCoroutine(ReactionSequenceThenLeave(immediateLine, exitLine, hearts));
            return;
        }

        if (secondCustomerRoute)
        {
            string immediateLine = result.success ? SecondRouteSuccessImmediateLine : SecondRouteWeakLine;
            string exitLine = result.success ? SecondRouteSuccessExitLine : null;
            StartCoroutine(ReactionSequenceThenLeave(immediateLine, exitLine, hearts));
            return;
        }

        if (thirdCustomerRoute)
        {
            string immediateLine = result.success ? ThirdRouteSuccessImmediateLine : ThirdRouteWeakLine;
            string exitLine = result.success ? ThirdRouteSuccessExitLine : null;
            StartCoroutine(ReactionSequenceThenLeave(immediateLine, exitLine, hearts));
            return;
        }

        if (fourthCustomerRoute)
        {
            string immediateLine = result.success ? FourthRouteSuccessImmediateLine : FourthRouteWeakLine;
            string exitLine = result.success ? FourthRouteSuccessExitLine : null;
            StartCoroutine(ReactionSequenceThenLeave(immediateLine, exitLine, hearts));
            return;
        }

        StartCoroutine(ReactionSequenceThenLeave(desiredTags, hearts));
    }

    private IEnumerator ReactionSequenceThenLeave(EmotionTag[] desiredTags, int hearts)
    {
        string immediateLine = reactionDatabase != null
            ? reactionDatabase.GetRandomLine(desiredTags, hearts, ReactionPhase.Immediate)
            : "...";

        string exitLine = reactionDatabase != null
            ? reactionDatabase.GetRandomLine(desiredTags, hearts, ReactionPhase.Exit)
            : "...";

        yield return ReactionSequenceThenLeave(immediateLine, exitLine, hearts);
    }

    private IEnumerator ReactionSequenceThenLeave(string immediateLine, int hearts)
    {
        yield return ReactionSequenceThenLeave(immediateLine, null, hearts);
    }

    private IEnumerator ReactionSequenceThenLeave(string immediateLine, string exitLine, int hearts)
    {
        // Show the immediate reaction line.
        ShowReactionBubble(immediateLine);

        yield return new WaitForSeconds(1.2f);

        ClearDialogueBubble();

        yield return new WaitForSeconds(0.5f);

        // Show heart feedback. ShowHearts skips the popup when hearts is 0.
        ShowHearts(hearts);

        yield return new WaitForSeconds(1.0f);

        // Show the exit line when one is available, then leave after the normal pause.
        ShowReactionBubble(exitLine);

        yield return new WaitForSeconds(1.2f);

        ClearDialogueBubble();

        yield return new WaitForSeconds(0.3f);

        state = State.Leaving;
    }

    private void ShowReactionBubble(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        SayLine(line);
    }

    private void ShowHearts(int hearts)
    {
        Debug.Log($"[Customer] Hearts = {hearts}");

        if (heartPopupPrefab == null)
        {
            Debug.LogWarning("[Customer] heartPopupPrefab is NULL");
            return;
        }

        if (hearts <= 0)
        {
            Debug.LogWarning("[Customer] hearts <= 0, popup skipped");
            return;
        }

        Transform target = reactionAnchor != null ? reactionAnchor : transform;

        Debug.Log($"[Customer] Spawning heart popup at {target.position}");

        GameObject popup = Instantiate(heartPopupPrefab);
        HeartPopup popupComp = popup.GetComponent<HeartPopup>();

        if (popupComp == null)
        {
            Debug.LogError("[Customer] HeartPopup component missing on prefab!");
            Destroy(popup);
            return;
        }

        popupComp.Init(target, hearts);
    }

    void LeaveAndDestroy()
    {
        ClearDialogueBubble();

        if (seatManager != null && targetSeat != null)
            seatManager.ReleaseSeat(targetSeat);

        if (interactionUI != null)
            interactionUI.ClearCustomer(this);

        if (loopManager != null)
            loopManager.NotifyCustomerLeft(this);

        Destroy(gameObject);
    }

    void SayLine(string line)
    {
        if (dialogueBubblePrefab == null)
        {
            Debug.LogError("[Customer] dialogueBubblePrefab is NULL");
            return;
        }

        if (string.IsNullOrEmpty(line))
            return;

        ClearDialogueBubble();

        GameObject bubble = Instantiate(dialogueBubblePrefab);
        currentDialogueBubble = bubble;

        DialogueBubble bubbleComp = bubble.GetComponent<DialogueBubble>();
        if (bubbleComp == null)
        {
            Debug.LogError("[Customer] DialogueBubble component missing on prefab!");
            Destroy(bubble);
            currentDialogueBubble = null;
            return;
        }

        Transform followTarget = reactionAnchor != null ? reactionAnchor : transform;
        bubbleComp.Init(followTarget, line);
    }

    void SayRandom(string[] lines)
    {
        if (dialogueBubblePrefab == null)
        {
            Debug.LogError("[Customer] dialogueBubblePrefab is NULL");
            return;
        }

        if (lines == null || lines.Length == 0)
            return;

        string line = Pick(lines);

        if (string.IsNullOrEmpty(line))
            return;

        ClearDialogueBubble();

        GameObject bubble = Instantiate(dialogueBubblePrefab);
        currentDialogueBubble = bubble;

        DialogueBubble bubbleComp = bubble.GetComponent<DialogueBubble>();
        if (bubbleComp == null)
        {
            Debug.LogError("[Customer] DialogueBubble component missing on prefab!");
            Destroy(bubble);
            currentDialogueBubble = null;
            return;
        }

        Transform followTarget = reactionAnchor != null ? reactionAnchor : transform;
        bubbleComp.Init(followTarget, line);
    }

    string Pick(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return "";

        return lines[UnityEngine.Random.Range(0, lines.Length)];
    }

    public Vector3 GetInteractionAnchorPosition()
    {
        return GetInteractionAnchorTransform().position;
    }

    public Transform GetInteractionAnchorTransform()
    {
        return interactionAnchor != null ? interactionAnchor : transform;
    }

    public string GetInteractionHintLine()
    {
        return BuildHintLine();
    }

    private string BuildHintLine()
    {
        if (IsInForcedFirstCustomerRoute())
            return FirstRouteHintLine;

        if (IsInForcedSecondCustomerRoute())
            return SecondRouteHintLine;

        if (IsInForcedThirdCustomerRoute())
            return ThirdRouteHintLine;

        if (IsInForcedFourthCustomerRoute())
            return FourthRouteHintLine;

        if (emotionRequest == null)
            return "손님이 어떤 맛을 원하는지 아직 모르겠습니다.";

        string primary = GetHintSentence(emotionRequest.primaryTag);
        string secondary = GetHintSentence(emotionRequest.secondaryTag);

        if (string.IsNullOrEmpty(secondary))
            return primary;

        return $"{primary}\n그리고 {secondary}";
    }

    private void ClearDialogueBubble()
    {
        if (currentDialogueBubble != null)
        {
            Destroy(currentDialogueBubble);
            currentDialogueBubble = null;
        }
    }

    private string GetHintSentence(EmotionTag tag)
    {
        return tag switch
        {
            EmotionTag.Loneliness => "혼자 남겨진 듯한 맛을 찾고 있어요.",
            EmotionTag.Comfort => "조금 부드럽고 편안한 맛을 원해요.",
            EmotionTag.Awakening => "잠에서 깨어나는 듯한 선명함을 원해요.",
            EmotionTag.Softness => "너무 날카롭지 않은 부드러운 맛이 좋겠어요.",
            EmotionTag.Bitterness => "약간의 쓴맛도 괜찮을 것 같아요.",
            EmotionTag.Heavy => "가볍기보다는 묵직하게 남는 맛을 원해요.",
            EmotionTag.Clarity => "흐리지 않고 맑게 정리되는 맛이 필요해요.",
            EmotionTag.Warmth => "차가운 밤을 데워줄 따뜻한 맛을 원해요.",
            EmotionTag.Regret => "조금은 씁쓸한 여운이 남아도 괜찮아요.",
            EmotionTag.Longing => "멀리 있는 무언가가 떠오르는 맛을 원해요.",
            _ => "손님이 어떤 맛을 원하는지 아직 모르겠습니다."
        };
    }
    
    private string GetRandomUnknownReactionLine(bool miniGameSuccess)
    {
        string fallbackLine = miniGameSuccess
            ? "이름은 모르겠지만, 묘하게 남는 맛이네요."
            : "아직은 조금 흔들리는 맛이네요.";

        string[] lines = miniGameSuccess ? unknownSuccessLines : unknownFailLines;

        if (lines == null || lines.Length == 0)
            return fallbackLine;

        string line = lines[Random.Range(0, lines.Length)];
        return string.IsNullOrEmpty(line) ? fallbackLine : line;
    }

    private string GetFirstRouteUnknownReactionLine(bool miniGameSuccess)
    {
        return miniGameSuccess ? FirstRouteUnknownSuccessLine : FirstRouteWeakLine;
    }

    private string GetSecondRouteUnknownReactionLine(bool miniGameSuccess)
    {
        return miniGameSuccess ? SecondRouteUnknownSuccessLine : SecondRouteWeakLine;
    }

    private string GetThirdRouteUnknownReactionLine(bool miniGameSuccess)
    {
        return miniGameSuccess ? ThirdRouteUnknownSuccessLine : ThirdRouteWeakLine;
    }

    private string GetFourthRouteUnknownReactionLine(bool miniGameSuccess)
    {
        return miniGameSuccess ? FourthRouteUnknownSuccessLine : FourthRouteWeakLine;
    }
}
