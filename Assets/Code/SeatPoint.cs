using UnityEngine;

public class SeatPoint : MonoBehaviour
{
    public bool IsOccupied { get; private set; }
    public CustomerController Occupant { get; private set; }

    public Transform sitMarker;        // 앉는 위치
    public Transform approachMarker;   // 의자 앞 도착 (선택)
    public Transform exitMarker;       // 나갈 때 목표 (선택)

    public bool Reserve(CustomerController cc)
    {
        if (IsOccupied) return false;
        IsOccupied = true;
        Occupant = cc;
        return true;
    }

    public void Release()
    {
        IsOccupied = false;
        Occupant = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        if (sitMarker != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(sitMarker.position, 0.06f);
        }
        if (approachMarker != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(approachMarker.position, 0.06f);
        }
        if (exitMarker != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(exitMarker.position, 0.06f);
        }
    }
}
