using TMPro;
using UnityEngine;

public class HeartPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text heartText;
    [SerializeField] private float lifeTime = 1.2f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);

    private const char HeartSymbol = '\u2665'; // ♥

    private Transform followTarget;

    public void Init(Transform target, int hearts)
    {
        followTarget = target;

        if (heartText != null)
        {
            int clampedHearts = Mathf.Clamp(hearts, 0, 3);
            heartText.text = new string(HeartSymbol, clampedHearts);
        }

        UpdatePosition();
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (followTarget == null)
            return;

        transform.position = followTarget.position + offset;
    }
}