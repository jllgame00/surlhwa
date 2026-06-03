using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GaugeStopMiniGame : MonoBehaviour, IMiniGame
{
    public Image gaugeFill;          // 0~1 fill
    public RectTransform targetZone; // UI·Î Ç¥½Ã
    public RectTransform perfectZone;
    public TMP_Text hintText;

    private float t;      // 0~1
    private float dir = 1f;
    private float speed;
    private float targetMin, targetMax;
    private float perfectMin, perfectMax;

    private Action<MiniGameResult> done;
    private bool running;

    private OrderData currentOrder;
    private BrewSessionData currentBrewSession;

    public void StartGame(OrderData order, Action<MiniGameResult> onDone)
    {
        BrewSessionData fallbackSession = order != null
            ? order.defaultBrewSession
            : null;

        StartGame(order, fallbackSession, onDone);
    }

    public void StartGame(OrderData order, BrewSessionData brewSession, Action<MiniGameResult> onDone)
    {
        done = onDone;
        currentOrder = order;

        currentBrewSession = brewSession != null
            ? brewSession
            : order != null
                ? order.defaultBrewSession
                : null;

        running = true;
        t = 0f;
        dir = 1f;

        RecipeStep step = FindGaugeStep(order);

        float center = UnityEngine.Random.Range(step.targetMin01, step.targetMax01);

        float width = 0.12f;
        targetMin = Mathf.Clamp01(center - width * 0.5f);
        targetMax = Mathf.Clamp01(center + width * 0.5f);

        float pWidth = 0.05f;
        perfectMin = Mathf.Clamp01(center - pWidth * 0.5f);
        perfectMax = Mathf.Clamp01(center + pWidth * 0.5f);

        speed = step.gaugeSpeed;

        UpdateZoneUI();

        if (gaugeFill != null)
            gaugeFill.fillAmount = 0f;

        if (hintText != null)
            hintText.text = "Press Space to stop";

        Debug.Log(
            $"[GaugeStopMiniGame] Start / " +
            $"Order={(currentOrder != null ? currentOrder.orderName : "NULL")} / " +
            $"BrewSession={(currentBrewSession != null ? currentBrewSession.method.ToString() : "NULL")}"
        );
    }

    private void Update()
    {
        if (!running)
            return;

        t += dir * speed * Time.deltaTime;

        if (t >= 1f)
        {
            t = 1f;
            dir = -1f;
        }

        if (t <= 0f)
        {
            t = 0f;
            dir = 1f;
        }

        if (gaugeFill != null)
            gaugeFill.fillAmount = t;

        if (Input.GetKeyDown(KeyCode.Space))
            Resolve();
    }

    private void Resolve()
    {
        running = false;

        bool inGood = t >= targetMin && t <= targetMax;
        bool inPerfect = t >= perfectMin && t <= perfectMax;

        MiniGameType fallbackMethod = currentOrder != null
            ? currentOrder.miniGameType
            : MiniGameType.Stir;

        MiniGameResult result = new MiniGameResult
        {
            success = inGood,
            repDelta = inPerfect ? 2 : inGood ? 1 : 0,
            grade = inPerfect ? "Perfect" : inGood ? "Good" : "Fail",
            brewedComposition = BrewCompositionFactory.FromSession(
                currentBrewSession,
                fallbackMethod
            )
        };

        done?.Invoke(result);
    }

    private RecipeStep FindGaugeStep(OrderData order)
    {
        if (order == null || order.steps == null)
        {
            return CreateFallbackStep();
        }

        foreach (RecipeStep step in order.steps)
        {
            if (step == null)
                continue;

            if (step.type == RecipeStepType.StirGauge ||
                step.type == RecipeStepType.ShakeGauge)
            {
                return step;
            }
        }

        return CreateFallbackStep();
    }

    private RecipeStep CreateFallbackStep()
    {
        return new RecipeStep
        {
            targetMin01 = 0.5f,
            targetMax01 = 1f,
            gaugeSpeed = 0.9f
        };
    }

    private void UpdateZoneUI()
    {
        ApplyZoneAnchor(targetZone, targetMin, targetMax);
        ApplyZoneAnchor(perfectZone, perfectMin, perfectMax);
    }

    private void ApplyZoneAnchor(RectTransform zone, float min, float max)
    {
        if (zone == null)
            return;

        zone.anchorMin = new Vector2(min, 0f);
        zone.anchorMax = new Vector2(max, 1f);
        zone.offsetMin = Vector2.zero;
        zone.offsetMax = Vector2.zero;
    }
}