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
    private OrderManager orderManager;
    private SeatManager seatManager;
    private UIManager ui;

    // 나갈 때 목표
    public Transform defaultExitPoint;

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private GameObject dialogueBubblePrefab;

    [Header("Emotion Reaction")]
    [SerializeField] private GuestReactionDatabase reactionDatabase;
    [SerializeField] private GuestEmotionRequest emotionRequest = new GuestEmotionRequest();
    [SerializeField] private Transform reactionAnchor;

    private static EmotionTag lastGeneralGuestPrimaryTag = EmotionTag.Loneliness;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (standSprite != null) sr.sprite = standSprite;

        orderManager = FindFirstObjectByType<OrderManager>();
        seatManager = FindFirstObjectByType<SeatManager>();
        ui = FindFirstObjectByType<UIManager>();
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
                    var dest = (targetSeat != null && targetSeat.approachMarker != null)
                        ? targetSeat.approachMarker.position
                        : (targetSeat != null ? targetSeat.transform.position : transform.position);

                    MoveToward(dest, onArrive: SitDown);
                    break;
                }

            case State.Sitting:
                state = State.Ordering;
                break;

            case State.Ordering:
                if (orderManager != null) orderManager.IssueOrderTo(this);
                state = State.Waiting;
                break;

            case State.Waiting:
                // Serve 누르면 OrderManager가 처리 후 OnServed / OnServedWithReaction 콜백
                break;

            case State.Leaving:
                {
                    Vector3 dest = GetExitDestination();
                    MoveToward(dest, onArrive: LeaveAndDestroy);
                    break;
                }
        }

        // y값 기준 정렬
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
    }

    void MoveToward(Vector3 dest, System.Action onArrive)
    {
        var dir = (dest - transform.position);
        var step = moveSpeed * Time.deltaTime;

        if (dir.magnitude <= step || (Mathf.Abs(dir.x) < arriveThreshold.x && Mathf.Abs(dir.y) < arriveThreshold.y))
        {
            Debug.Log("[Customer] Arrived at seat");
            transform.position = dest;
            onArrive?.Invoke();
            return;
        }

        transform.position += dir.normalized * step;

        if (dir.x != 0) transform.localScale = new Vector3(dir.x > 0 ? 1 : -1, 1, 1);
    }

    void SitDown()
    {
        Debug.Log("[Customer] SitDown called");

        state = State.Sitting;

        if (targetSeat != null && targetSeat.sitMarker != null)
            transform.position = targetSeat.sitMarker.position;

        if (sitSprite != null) sr.sprite = sitSprite;

        AssignRandomEmotionForGeneralGuest();

        // 입장/착석 랜덤 대사
        SayRandom(dialogueData != null ? dialogueData.onEnterLines : null);
    }

    Vector3 GetExitDestination()
    {
        if (targetSeat != null && targetSeat.exitMarker != null) return targetSeat.exitMarker.position;
        if (defaultExitPoint != null) return defaultExitPoint.position;
        return transform.position + Vector3.left * 3f;
    }

    // 기존 구조 유지용
    public void OnServed(bool success)
    {
        if (dialogueData != null)
        {
            var lines = success ? dialogueData.onSuccessLines : dialogueData.onFailLines;
            SayRandom(lines);
        }

        state = State.Leaving;
    }

    // 새 반응 시스템용
    public void OnServedWithReaction(OrderData servedOrder, MiniGameResult result)
    {
        EmotionTag[] desiredTags = emotionRequest != null
            ? emotionRequest.ToArray()
            : System.Array.Empty<EmotionTag>();

        EmotionTag[] servedTags = (servedOrder != null &&
                           servedOrder.targetCocktail != null &&
                           servedOrder.targetCocktail.emotionTags != null)
            ? servedOrder.targetCocktail.emotionTags
            : System.Array.Empty<EmotionTag>();

        bool recipeInTolerance = result.success;

        int hearts = GuestReactionResolver.CalculateHearts(desiredTags, servedTags, recipeInTolerance);

        string immediateLine = reactionDatabase != null
            ? reactionDatabase.GetRandomLine(desiredTags, hearts, ReactionPhase.Immediate)
            : "...";

        ShowReactionBubble(immediateLine);
        ShowHearts(hearts);

        StartCoroutine(ExitReactionThenLeave(desiredTags, hearts));
    }

    private IEnumerator ExitReactionThenLeave(EmotionTag[] desiredTags, int hearts)
    {
        yield return new WaitForSeconds(1.5f);

        string exitLine = reactionDatabase != null
            ? reactionDatabase.GetRandomLine(desiredTags, hearts, ReactionPhase.Exit)
            : "...";

        ShowReactionBubble(exitLine);
        ShowHearts(hearts);

        yield return new WaitForSeconds(1.5f);

        state = State.Leaving;
    }

    private void ShowReactionBubble(string line)
    {
        if (string.IsNullOrEmpty(line)) return;
        SayLine(line);
    }

    private void ShowHearts(int hearts)
    {
        Debug.Log($"[Customer] Hearts = {hearts}");

        // 나중에 하트 팝업 프리팹 붙이면 여기서 처리
        // ex) Instantiate(heartPopupPrefab, reactionAnchor != null ? reactionAnchor.position : transform.position, Quaternion.identity);
    }

    void LeaveAndDestroy()
    {
        if (seatManager != null && targetSeat != null)
            seatManager.ReleaseSeat(targetSeat);

        Destroy(gameObject);
    }

    // ---------- Dialogue helpers ----------
    void SayLine(string line)
    {
        if (dialogueBubblePrefab == null)
        {
            Debug.LogError("[Customer] dialogueBubblePrefab is NULL");
            return;
        }

        if (string.IsNullOrEmpty(line)) return;

        GameObject bubble = Instantiate(dialogueBubblePrefab);

        var bubbleComp = bubble.GetComponent<DialogueBubble>();
        if (bubbleComp == null)
        {
            Debug.LogError("[Customer] DialogueBubble component missing on prefab!");
            Destroy(bubble);
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

        Debug.Log("[Customer] SayRandom called");

        if (lines == null || lines.Length == 0) return;

        string line = Pick(lines);
        if (string.IsNullOrEmpty(line)) return;

        GameObject bubble = Instantiate(dialogueBubblePrefab);

        var bubbleComp = bubble.GetComponent<DialogueBubble>();
        if (bubbleComp == null)
        {
            Debug.LogError("[Customer] DialogueBubble component missing on prefab!");
            Destroy(bubble);
            return;
        }

        bubbleComp.Init(transform, line);
    }

    string Pick(string[] lines)
    {
        if (lines == null || lines.Length == 0) return "";
        return lines[UnityEngine.Random.Range(0, lines.Length)];
    }
}