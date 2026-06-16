using System.Linq;
using UnityEngine;

public class SeatManager : MonoBehaviour
{
    [SerializeField] private bool randomizeSeats = true;
    [SerializeField] private bool avoidSameSeatTwice = true;

    private SeatPoint[] seats;
    private SeatPoint lastAssignedSeat;

    private void Awake()
    {
        seats = GameObject.FindGameObjectsWithTag("SeatPoint")
            .Select(go => go.GetComponent<SeatPoint>())
            .Where(s => s != null)
            .ToArray();

        Debug.Log($"[SeatManager] Found seats: {seats.Length}");
    }

    public SeatPoint GetFreeSeat(CustomerController cc)
    {
        var freeSeats = seats
            .Where(s => s != null && !s.IsOccupied)
            .ToList();

        if (freeSeats.Count == 0)
            return null;

        if (avoidSameSeatTwice && freeSeats.Count > 1 && lastAssignedSeat != null)
        {
            freeSeats.Remove(lastAssignedSeat);
        }

        SeatPoint selectedSeat = randomizeSeats
            ? freeSeats[Random.Range(0, freeSeats.Count)]
            : freeSeats[0];

        if (selectedSeat.Reserve(cc))
        {
            lastAssignedSeat = selectedSeat;
            return selectedSeat;
        }

        return null;
    }

    public void ReleaseSeat(SeatPoint seat)
    {
        if (seat != null)
            seat.Release();
    }
}