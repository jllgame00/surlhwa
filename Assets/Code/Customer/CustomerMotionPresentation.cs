using UnityEngine;

/// <summary>Reads the customer's existing lifecycle. Never moves its root or completes gameplay.</summary>
[DisallowMultipleComponent]
public sealed class CustomerMotionPresentation : MonoBehaviour
{
    [Min(1)] public float walkFramesPerSecond = 7;
    [Min(1)] public float idleCadence = 7;
    [Min(.1f)] public float reactionDuration = .8f;
    public CustomerController Owner { get; private set; }
    public string PresentedPose { get; private set; }
    public int WalkFrame { get; private set; }
    private SpriteRenderer body;
    private Sprite original;
    private CustomerMotionLibrary.Poses poses;
    private Vector3 lastPosition;
    private float walked, reactionUntil;
    private bool quiet;

    public void Configure(CustomerController owner, Sprite source)
    {
        Owner = owner;
        body = GetComponent<SpriteRenderer>();
        original = source;
        var library = Resources.Load<CustomerMotionLibrary>("CustomerMotionLibrary");
        poses = library != null && source != null ? library.Find(source.name) : null;
        quiet = source != null && source.name == "Hamlet";
        lastPosition = owner.transform.position;
        walked = 0;
        var foley = GetComponent<CustomerFoleyPresentation>();
        if (foley == null) foley = gameObject.AddComponent<CustomerFoleyPresentation>();
        foley.Configure(owner);
    }

    private void Start()
    {
        if (Owner == null)
        {
            var owner = GetComponent<CustomerController>();
            if (owner != null) Configure(owner, owner.standSprite);
        }
    }

    public void React()
    {
        reactionUntil = Time.time + reactionDuration * (quiet ? .65f : 1f);
        // The pair shares one service. Its companion never duplicates this contact.
        if (Owner != null && Owner.gameObject == gameObject)
            DemoAudioController.Current?.Sounds?.Play(BarSound.GlassSet);
    }

    private void LateUpdate()
    {
        if (Owner == null || body == null || original == null) return;
        float distance = Vector3.Distance(lastPosition, Owner.transform.position);
        lastPosition = Owner.transform.position;
        var state = Owner.CurrentState;
        bool moving = state == CustomerController.State.MovingToSeat || state == CustomerController.State.Leaving;
        Sprite frame = original;
        PresentedPose = "Entering";
        if (moving)
        {
            // Distance drives cadence: pause and arrival cannot keep cycling a stationary foot.
            walked += distance * walkFramesPerSecond / Mathf.Max(.1f, Owner.moveSpeed);
            WalkFrame = Mathf.FloorToInt(walked) % 4;
            if (poses?.walk != null && poses.walk.Length > WalkFrame && poses.walk[WalkFrame] != null)
                frame = poses.walk[WalkFrame];
            PresentedPose = state == CustomerController.State.Leaving ? "Leaving" : "Walking";
            body.flipX = !Owner.FacingLeft;
        }
        else
        {
            // A three-quarter seated view faces inward toward the central bartender.
            bool inwardLeft = Owner.transform.position.x >= 0;
            body.flipX = !inwardLeft;
            switch (state)
            {
                case CustomerController.State.Turning:
                    frame = poses?.turn; PresentedPose = "Turning"; break;
                case CustomerController.State.Sitting:
                    frame = Owner.StateElapsed < Owner.sitDuration * .45f ? poses?.turn : poses?.seated;
                    PresentedPose = "Sitting"; break;
                case CustomerController.State.Standing:
                    frame = Owner.StateElapsed < Owner.standDuration * .45f ? poses?.seated : poses?.turn;
                    PresentedPose = "Standing"; break;
                case CustomerController.State.Ordering:
                case CustomerController.State.Waiting:
                    bool reacting = Time.time < reactionUntil;
                    bool listening = !quiet && Owner.IsPatiencePausedForNarrativeDialogue && Time.time % idleCadence < .32f;
                    frame = reacting ? poses?.reaction : listening ? poses?.listening : poses?.seated;
                    PresentedPose = reacting ? "ServiceReaction" : listening ? "Listening" : "SeatedIdle";
                    break;
            }
        }
        body.sprite = frame != null ? frame : original;
    }

    private void OnDisable()
    {
        if (body != null) { body.sprite = original; body.flipX = false; }
        reactionUntil = 0;
    }
}
