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

    // 외부 참조
    private SeatManager seatManager;

    // 나갈 때 목표
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
                // InteractionUIController에서
                // ... -> DialoguePanel -> BrewPanel -> Confirm 흐름 처리
                // Confirm 시 OrderManager.PlaySpecificOrder() 호출
                // 완료 시 OnServedWithReaction() 콜백
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

        // 자동 입장 말풍선은 현재 UI 흐름과 겹치므로 비활성 유지
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

    // 기존 구조 유지용
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

    // 기존 OrderData 기반 호출이 남아 있어도 깨지지 않게 유지
    public void OnServedWithReaction(OrderData servedOrder, MiniGameResult result)
    {
        CocktailData fallbackCocktail = servedOrder != null
            ? servedOrder.targetCocktail
            : null;

        OnServedWithReaction(fallbackCocktail, result);
    }

    // 새 구조: 실제 서빙된 CocktailData 기준으로 반응 계산
    public void OnServedWithReaction(CocktailData servedCocktail, MiniGameResult result)
    {
        if (interactionUI != null)
            interactionUI.ClearCustomer(this);

        EmotionTag[] desiredTags = emotionRequest != null
            ? emotionRequest.ToArray()
            : System.Array.Empty<EmotionTag>();

        EmotionTag[] servedTags = (servedCocktail != null && servedCocktail.emotionTags != null)
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

        // 1) 첫 반응 대사
        ShowReactionBubble(immediateLine);

        // 2) 잠깐 보여주기
        yield return new WaitForSeconds(1.2f);

        // 3) 말풍선 제거
        ClearDialogueBubble();

        // 4) 텀
        yield return new WaitForSeconds(0.5f);

        // 5) 하트 표시
        ShowHearts(hearts);

        // 6) 하트 보여준 뒤 텀
        yield return new WaitForSeconds(1.0f);

        string exitLine = reactionDatabase != null
            ? reactionDatabase.GetRandomLine(desiredTags, hearts, ReactionPhase.Exit)
            : "...";

        // 7) 퇴장 전 대사
        ShowReactionBubble(exitLine);

        // 8) 잠깐 보여주기
        yield return new WaitForSeconds(1.2f);

        // 9) 말풍선 제거
        ClearDialogueBubble();

        // 10) 마지막 텀
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
            return "오늘은 어떤 한 잔이 좋을까요.";

        string primary = GetHintSentence(emotionRequest.primaryTag);
        string secondary = GetHintSentence(emotionRequest.secondaryTag);

        if (string.IsNullOrEmpty(secondary))
            return primary;

        return $"{primary}\n그리고… {secondary}";
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
            EmotionTag.Loneliness => "조용한 쪽이면 좋겠어요.",
            EmotionTag.Comfort => "조금 부드러운 쪽이면 좋겠어요.",
            EmotionTag.Awakening => "머리가 맑아지는 느낌이면 좋겠네요.",
            EmotionTag.Softness => "너무 날카롭지 않은 쪽이 좋아요.",
            EmotionTag.Bitterness => "조금 씁쓸해도 괜찮아요.",
            EmotionTag.Heavy => "가볍지 않은 쪽이 오늘은 맞을 것 같아요.",
            EmotionTag.Clarity => "복잡하지 않고 맑은 쪽이 필요해요.",
            EmotionTag.Warmth => "조금 따뜻한 기분이 들면 좋겠어요.",
            EmotionTag.Regret => "쉽게 달래는 맛은 아니었으면 해요.",
            EmotionTag.Longing => "조금 오래 남는 느낌이면 좋겠어요.",
            _ => "오늘은 어떤 한 잔이 좋을까요."
        };
    }
}