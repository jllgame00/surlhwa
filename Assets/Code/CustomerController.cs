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

    // 외부 참조 (간단히 싱글톤/Find로도 되지만, 초반엔 그냥 Find로 타협 가능)
    private OrderManager orderManager;
    private SeatManager seatManager;

    // 나갈 때 목표 (없으면 스폰 위치로 되돌리거나, 특정 ExitPoint Transform 지정)
    public Transform defaultExitPoint;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (standSprite != null) sr.sprite = standSprite;

        orderManager = FindFirstObjectByType<OrderManager>();
        seatManager = FindFirstObjectByType<SeatManager>();
    }

    public void AssignSeat(SeatPoint seat)
    {
        targetSeat = seat;
        if (targetSeat == null)
        {
            // 자리 없으면 그냥 떠나게(또는 대기열 시스템은 나중에)
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
                // 앉자마자 주문 발급(최소 루프)
                state = State.Ordering;
                break;

            case State.Ordering:
                if (orderManager != null) orderManager.IssueOrderTo(this);
                state = State.Waiting;
                break;

            case State.Waiting:
                // UI에서 Serve 눌리면 orderManager가 ResolveOrder 호출 → 여기 OnServed로 들어오게 만들 것
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
        var dir = (dest - transform.position);
        var step = moveSpeed * Time.deltaTime;

        if (dir.magnitude <= step || (Mathf.Abs(dir.x) < arriveThreshold.x && Mathf.Abs(dir.y) < arriveThreshold.y))
        {
            transform.position = dest;
            onArrive?.Invoke();
            return;
        }

        transform.position += dir.normalized * step;

        if (dir.x != 0) transform.localScale = new Vector3(dir.x > 0 ? 1 : -1, 1, 1);
    }

    void SitDown()
    {
        state = State.Sitting;

        if (targetSeat != null && targetSeat.sitMarker != null)
            transform.position = targetSeat.sitMarker.position;

        if (sitSprite != null) sr.sprite = sitSprite;
    }

    Vector3 GetExitDestination()
    {
        if (targetSeat != null && targetSeat.exitMarker != null) return targetSeat.exitMarker.position;
        if (defaultExitPoint != null) return defaultExitPoint.position;
        // fallback: 그냥 왼쪽 밖으로
        return transform.position + Vector3.left * 3f;
    }

    // OrderManager가 성공/실패 처리 후 호출
    public void OnServed(bool success)
    {
        // 성공/실패에 따른 표정/스프라이트 바꾸는 것도 여기 가능(나중에)
        state = State.Leaving;
    }

    void LeaveAndDestroy()
    {
        // 자리 해제
        if (seatManager != null && targetSeat != null)
            seatManager.ReleaseSeat(targetSeat);

        Destroy(gameObject);
    }
}
