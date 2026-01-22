using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GaugeStopMiniGame : MonoBehaviour, IMiniGame
{
    public Image gaugeFill;          // 0~1 fill
    public RectTransform targetZone; // UI로 표시
    public RectTransform perfectZone;
    public TMP_Text hintText;

    private float t;      // 0~1
    private float dir = 1f;
    private float speed;
    private float targetMin, targetMax;
    private float perfectMin, perfectMax;

    private System.Action<MiniGameResult> done;
    private bool running;

    public void StartGame(OrderData order, System.Action<MiniGameResult> onDone)
    {
        done = onDone;
        running = true;

        // order.steps 중 StirGauge/ShakeGauge step 하나를 가져온다고 가정(처음엔 첫 번째로)
        var step = FindGaugeStep(order);

        // 50~100% 범위에서 랜덤 존 생성
        var center = Random.Range(step.targetMin01, step.targetMax01);
        var width = 0.12f; // Good zone 폭(튜닝)
        targetMin = Mathf.Clamp01(center - width * 0.5f);
        targetMax = Mathf.Clamp01(center + width * 0.5f);

        var pWidth = 0.05f; // Perfect zone 폭(튜닝)
        perfectMin = Mathf.Clamp01(center - pWidth * 0.5f);
        perfectMax = Mathf.Clamp01(center + pWidth * 0.5f);

        speed = step.gaugeSpeed;

        // UI 존 위치 갱신(AnchoredPosition로 바꾸거나, Layout에 맞게 변환)
        UpdateZoneUI();

        if (hintText) hintText.text = "Press Space to stop";
    }

    void Update()
    {
        if (!running) return;

        // 게이지 왕복
        t += dir * speed * Time.deltaTime;
        if (t >= 1f) { t = 1f; dir = -1f; }
        if (t <= 0f) { t = 0f; dir = 1f; }

        if (gaugeFill) gaugeFill.fillAmount = t;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Resolve();
        }
    }

    void Resolve()
    {
        running = false;

        bool inGood = (t >= targetMin && t <= targetMax);
        bool inPerfect = (t >= perfectMin && t <= perfectMax);

        var result = new MiniGameResult
        {
            success = inGood,
            repDelta = inPerfect ? 2 : (inGood ? 1 : 0),
            grade = inPerfect ? "Perfect" : (inGood ? "Good" : "Fail")
        };

        done?.Invoke(result);
    }

    RecipeStep FindGaugeStep(OrderData order)
    {
        foreach (var s in order.steps)
            if (s.type == RecipeStepType.StirGauge || s.type == RecipeStepType.ShakeGauge)
                return s;

        // fallback
        return new RecipeStep { targetMin01 = 0.5f, targetMax01 = 1f, gaugeSpeed = 0.9f };
    }

    void UpdateZoneUI()
    {
        // UI 구현 방식에 따라 다름:
        // - targetZone을 Slider 위에 배치하고 anchorMin/Max를 targetMin/Max로 맞추는 방식 추천
        // 여기선 “개념”만: 실제 UI 레이아웃에 맞춰 연결해줄게.
    }
}
