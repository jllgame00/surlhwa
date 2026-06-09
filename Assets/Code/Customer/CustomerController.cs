using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CustomerController : MonoBehaviour
{
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
    }

    private void OnDisable()
    {
        if (interactionUI != null)
            interactionUI.ClearCustomer(this);
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

    void Update()
    {
        switch (state)
        {
            case State.MovingToSeat:
                {
                    Vector3 dest = (targetSeat != null && targetSeat.approachMarker != null)
                        ? targetSeat.approachMarker.position
                        : (targetSeat != null ? targetSeat.transform.position : transform.position);

                    MoveToward(dest, onArrive: SitDown);
                    break;
                }

            case State.Sitting:
                state = State.Ordering;
                break;

            case State.Ordering:
                if (interactionUI != null)
                    interactionUI.RegisterCustomer(this);
                else
                    Debug.LogWarning("[Customer] InteractionUIController not found");

                state = State.Waiting;
                break;

            case State.Waiting:
                // InteractionUIController handles DialoguePanel -> BrewPanel -> Confirm.
                // Confirm starts OrderManager.PlaySpecificOrder(); completion calls OnServedWithReaction().
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

    // Legacy serve path.
    public void OnServed(bool success)
    {
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
        if (interactionUI != null)
            interactionUI.ClearCustomer(this);

        EmotionTag[] desiredTags = emotionRequest != null
            ? emotionRequest.ToArray()
            : System.Array.Empty<EmotionTag>();

        if (servedCocktail == null)
        {
            int unknownHearts = result.success ? 1 : 0;
            string unknownLine = GetRandomUnknownReactionLine(result.success);

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
        return interactionAnchor != null ? interactionAnchor.position : transform.position;
    }

    public string GetInteractionHintLine()
    {
        return BuildHintLine();
    }

    private string BuildHintLine()
    {
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
}
