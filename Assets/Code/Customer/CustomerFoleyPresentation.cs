using UnityEngine;

/// Distance-driven footfalls; no sources, timers or completion callbacks own the customer.
public sealed class CustomerFoleyPresentation : MonoBehaviour
{
    [Min(.1f)] public float strideDistance = .56f;
    private CustomerController owner;
    private CustomerController.State previousState;
    private Vector3 previousPosition;
    private float distance;
    private bool right, companion;
    public void Configure(CustomerController customer)
    {
        owner = customer; previousState = customer.CurrentState;
        previousPosition = customer.transform.position;
        companion = customer.gameObject != gameObject;
        distance = companion ? strideDistance * .5f : 0;
    }
    private void LateUpdate()
    {
        if (owner == null) return;
        var state = owner.CurrentState;
        float moved = Vector3.Distance(previousPosition, owner.transform.position);
        previousPosition = owner.transform.position;
        var sounds = DemoAudioController.Current?.Sounds;
        float pan = Mathf.Clamp(transform.position.x / 20, -.25f, .25f);
        float level = companion ? .55f : 1;
        if (Time.timeScale > 0 && (state == CustomerController.State.MovingToSeat || state == CustomerController.State.Leaving))
        {
            // Ignore fixture/restore teleports; never synthesize a burst of missed steps.
            if (moved < Mathf.Max(1, owner.moveSpeed * Time.deltaTime * 2)) distance += moved;
            if (distance >= strideDistance)
            {
                distance %= strideDistance; right = !right;
                sounds?.Play(right ? BarSound.FootRight : BarSound.FootLeft, pan, level);
            }
        }
        if (state != previousState)
        {
            if (state == CustomerController.State.Sitting) sounds?.Play(BarSound.Seat, pan, level);
            if (state == CustomerController.State.Standing) sounds?.Play(BarSound.Stand, pan, level);
            previousState = state;
        }
    }
}
