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
                // Serve 누르면 OrderManager가 처리 후 OnServed 콜백
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

        // 입장/착석 랜덤 대사
        SayRandom(dialogueData != null ? dialogueData.onEnterLines : null);
    }

    Vector3 GetExitDestination()
    {
        if (targetSeat != null && targetSeat.exitMarker != null) return targetSeat.exitMarker.position;
        if (defaultExitPoint != null) return defaultExitPoint.position;
        return transform.position + Vector3.left * 3f;
    }

    // OrderManager가 성공/실패 처리 후 호출
    public void OnServed(bool success)
    {
        // 성공/실패 랜덤 대사
        if (dialogueData != null)
        {
            var lines = success ? dialogueData.onSuccessLines : dialogueData.onFailLines;
            SayRandom(lines);
        }

        state = State.Leaving;
    }

    void LeaveAndDestroy()
    {
        if (seatManager != null && targetSeat != null)
            seatManager.ReleaseSeat(targetSeat);

        Destroy(gameObject);
    }

    // ---------- Dialogue helpers ----------
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