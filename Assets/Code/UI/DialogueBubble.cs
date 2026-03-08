using TMPro;
using UnityEngine;

public class DialogueBubble : MonoBehaviour
{
    public TMP_Text text;
    public float lifetime = 2.5f;

    Transform target;
    Vector3 offset = new Vector3(0f, 2.3f, 0f);

    public void Init(Transform followTarget, string message)
    {
        target = followTarget;
        text.text = message;
        Destroy(gameObject, lifetime);
    }

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }
}