using System.Linq;
using UnityEngine;

public class SeatManager : MonoBehaviour
{
    private SeatPoint[] seats;

    private void Awake()
    {
        seats = GameObject.FindGameObjectsWithTag("SeatPoint")
            .Select(go => go.GetComponent<SeatPoint>())
            .Where(s => s != null)
            .ToArray();
    }

    public SeatPoint GetFreeSeat(CustomerController cc)
    {
        foreach (var s in seats)
        {
            if (!s.IsOccupied && s.Reserve(cc))
                return s;
        }
        return null;
    }

    public void ReleaseSeat(SeatPoint seat)
    {
        if (seat != null) seat.Release();
    }
}
