using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class StirMiniGame : MonoBehaviour, IMiniGame
{
    [Header("Gauge")]
    public Image gaugeFill;
    public RectTransform targetZone;
    public RectTransform perfectZone;

    [Header("Result")]
    public TMP_Text resultLabel;

    float t;
    float dir = 1f;
    float speed;

    float targetMin, targetMax;
    float perfectMin, perfectMax;

    bool running;
    Action<MiniGameResult> onDone;

    public void StartGame(OrderData order, Action<MiniGameResult> done)
    {
        onDone = done;
        running = true;
        t = 0f;
        dir = 1f;

        // 임시값 (나중에 RecipeStep에서 뽑아오면 됨)
        speed = 0.9f;

        float center = UnityEngine.Random.Range(0.5f, 1f);
        targetMin = center - 0.08f;
        targetMax = center + 0.08f;

        perfectMin = center - 0.03f;
        perfectMax = center + 0.03f;

        PlaceZone(targetZone, targetMin, targetMax);
        PlaceZone(perfectZone, perfectMin, perfectMax);

        resultLabel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!running) return;

        t += dir * speed * Time.deltaTime;
        if (t >= 1f) { t = 1f; dir = -1f; }
        if (t <= 0f) { t = 0f; dir = 1f; }

        gaugeFill.fillAmount = t;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Resolve();
        }
    }

    void Resolve()
    {
        running = false;

        bool perfect = t >= perfectMin && t <= perfectMax;
        bool good = t >= targetMin && t <= targetMax;

        MiniGameResult result = new MiniGameResult
        {
            success = good || perfect,
            repDelta = perfect ? 2 : (good ? 1 : 0),
            grade = perfect ? "Perfect" : (good ? "Good" : "Fail")
        };

        ShowResult(result.grade);
        StartCoroutine(FinishAfterDelay(result, 0.8f));
    }

    System.Collections.IEnumerator FinishAfterDelay(MiniGameResult result, float delay)
    {
        yield return new WaitForSeconds(delay);
        onDone?.Invoke(result);
    }

    void ShowResult(string text)
    {
        resultLabel.text = text;
        resultLabel.gameObject.SetActive(true);
    }

    void PlaceZone(RectTransform zone, float min, float max)
    {
        zone.anchorMin = new Vector2(0f, min);
        zone.anchorMax = new Vector2(1f, max);
        zone.offsetMin = Vector2.zero;
        zone.offsetMax = Vector2.zero;
    }
}
