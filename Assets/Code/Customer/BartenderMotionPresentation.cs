using UnityEngine;

/// <summary>Local hand-rest and acknowledgment. No locomotion, service or progression authority.</summary>
[DisallowMultipleComponent]
public sealed class BartenderMotionPresentation : MonoBehaviour
{
    [Min(1)] public float idleCadence = 10;
    [Range(.1f, 2)] public float acknowledgmentDuration = .65f;
    private SpriteRenderer body;
    private Sprite original;
    private CustomerMotionLibrary art;
    private CustomerController listeningTo;
    private float acknowledgeUntil;

    private void Awake()
    {
        body = GetComponent<SpriteRenderer>();
        if (body == null) { enabled = false; return; }
        original = body.sprite;
        art = Resources.Load<CustomerMotionLibrary>("CustomerMotionLibrary");
    }
    public void Attend(CustomerController guest) => listeningTo = guest;
    public void Acknowledge(CustomerController guest)
    {
        listeningTo = guest;
        acknowledgeUntil = Time.time + acknowledgmentDuration;
    }
    private void LateUpdate()
    {
        body.flipX = listeningTo != null && listeningTo.transform.position.x > transform.position.x;
        Sprite pose = null;
        if (art != null)
        {
            if (Time.time < acknowledgeUntil) pose = art.bartenderAcknowledge;
            // During dialogue the bartender listens without a repeating hand loop.
            else if (listeningTo == null && Time.time % idleCadence < .4f) pose = art.bartenderRest;
        }
        body.sprite = pose != null ? pose : original;
    }
    private void OnDisable()
    {
        if (body != null) { body.sprite = original; body.flipX = false; }
        acknowledgeUntil = 0;
    }
}
