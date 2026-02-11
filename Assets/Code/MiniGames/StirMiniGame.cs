using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class StirMiniGame : MonoBehaviour, IMiniGame
{
    enum State { Ready, Stirring, Result }
    State state = State.Ready;

    [Header("Gauge Track (UI)")]
    public RectTransform gaugeBGRect;

    [Header("Zones (UI)")]
    public RectTransform targetZone;
    public RectTransform perfectZone;

    [Header("Marker (UI)")]
    public RectTransform marker;

    [Header("Cup / Spoon (UI)")]
    public RectTransform cup;
    public RectTransform spoon;
    public RectTransform spoonInCup;   // 컵 안 위치(빈 RectTransform)

    [Header("Text (UI)")]
    public TMP_Text resultLabel;

    [Header("Timing")]
    public float fillDuration = 3.0f;
    public float resultHold = 0.8f;

    [Header("Spoon Orbit (Stir Motion)")]
    public RectTransform orbitCenter;  // 보통 spoonInCup 또는 cup
    public float orbitRadius = 16f;
    public float orbitSpeed = 7f;
    public float orbitYScale = 0.65f;
    public float wobbleDeg = 10f;

    [Header("Spoon Pose")]
    public float spoonBaseAngleDeg = 90f; // 세로로 세우기(필요하면 -90)

    float t;
    float targetMin, targetMax;
    float perfectMin, perfectMax;

    float orbitAngle;

    Action<MiniGameResult> onDone;

    public void StartGame(OrderData order, Action<MiniGameResult> done)
    {
        onDone = done;
        AutoBindIfMissing();

        state = State.Ready;
        t = 0f;
        orbitAngle = 0f;

        if (resultLabel != null)
        {
            resultLabel.gameObject.SetActive(true);
            resultLabel.text = "Press SPACE to start";
        }

        // 판정 구간
        float center = UnityEngine.Random.Range(0.55f, 0.95f);
        targetMin = Mathf.Clamp01(center - 0.08f);
        targetMax = Mathf.Clamp01(center + 0.08f);
        perfectMin = Mathf.Clamp01(center - 0.03f);
        perfectMax = Mathf.Clamp01(center + 0.03f);

        PlaceZone(targetZone, targetMin, targetMax);
        PlaceZone(perfectZone, perfectMin, perfectMax);

        UpdateMarker();

        // 공전 중심 기본값
        if (orbitCenter == null) orbitCenter = spoonInCup != null ? spoonInCup : cup;

        // 스푼 초기 위치: 처음부터 컵 안 + 세로 각도
        if (spoon != null && spoonInCup != null)
        {
            spoon.anchoredPosition = spoonInCup.anchoredPosition;
            spoon.localRotation = Quaternion.Euler(0f, 0f, spoonBaseAngleDeg);
        }

        Debug.Log($"[StirMiniGame] refs: bg={(gaugeBGRect ? gaugeBGRect.name : "NULL")}, marker={(marker ? marker.name : "NULL")}, spoon={(spoon ? spoon.name : "NULL")}, orbitCenter={(orbitCenter ? orbitCenter.name : "NULL")}");
    }

    void Update()
    {
        if (state == State.Ready)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (resultLabel != null) resultLabel.text = "";
                state = State.Stirring;
            }
            return;
        }

        if (state == State.Stirring)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, fillDuration);

            if (t >= 1f)
            {
                t = 1f;
                UpdateMarker();
                ResolveTimeoutFail();
                return;
            }

            UpdateMarker();
            OrbitSpoon();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ResolveByStop();
            }
        }
    }

    void UpdateMarker()
    {
        if (marker == null || gaugeBGRect == null) return;

        float trackHeight = gaugeBGRect.rect.height;
        float markerHalf = marker.rect.height * 0.5f;

        float yMin = markerHalf;
        float yMax = trackHeight - markerHalf;

        float y = Mathf.Lerp(yMin, yMax, t);

        Vector2 pos = marker.anchoredPosition;
        pos.y = y;
        marker.anchoredPosition = pos;
    }

    void OrbitSpoon()
    {
        if (spoon == null || orbitCenter == null) return;

        orbitAngle += orbitSpeed * Time.deltaTime;

        float x = Mathf.Cos(orbitAngle) * orbitRadius;
        float y = Mathf.Sin(orbitAngle) * (orbitRadius * orbitYScale);

        spoon.anchoredPosition = orbitCenter.anchoredPosition + new Vector2(x, y);

        // 풍차 자전 금지: 기본각 + 워블만
        float wobble = Mathf.Sin(orbitAngle * 2f) * wobbleDeg;
        spoon.localRotation = Quaternion.Euler(0f, 0f, spoonBaseAngleDeg + wobble);
    }

    void ResolveByStop()
    {
        state = State.Result;

        bool perfect = t >= perfectMin && t <= perfectMax;
        bool good = t >= targetMin && t <= targetMax;

        MiniGameResult result = new MiniGameResult
        {
            success = good || perfect,
            repDelta = perfect ? 2 : (good ? 1 : 0),
            grade = perfect ? "Perfect" : (good ? "Good" : "Fail")
        };

        ShowResult(result.grade);
        StartCoroutine(FinishAfterDelay(result, resultHold));
    }

    void ResolveTimeoutFail()
    {
        state = State.Result;

        MiniGameResult result = new MiniGameResult
        {
            success = false,
            repDelta = 0,
            grade = "Fail"
        };

        ShowResult("Fail");
        StartCoroutine(FinishAfterDelay(result, resultHold));
    }

    IEnumerator FinishAfterDelay(MiniGameResult result, float delay)
    {
        yield return new WaitForSeconds(delay);
        onDone?.Invoke(result);
    }

    void ShowResult(string text)
    {
        if (resultLabel == null) return;
        resultLabel.text = text;
        resultLabel.gameObject.SetActive(true);
    }

    void PlaceZone(RectTransform zone, float min, float max)
    {
        if (zone == null) return;

        zone.anchorMin = new Vector2(0f, min);
        zone.anchorMax = new Vector2(1f, max);
        zone.offsetMin = Vector2.zero;
        zone.offsetMax = Vector2.zero;
    }

    void AutoBindIfMissing()
    {
        if (gaugeBGRect == null)
            gaugeBGRect = transform.Find("UI/GaugeRoot/GaugeBG")?.GetComponent<RectTransform>();

        if (marker == null)
            marker = transform.Find("UI/GaugeRoot/GaugeBG/GaugeFill")?.GetComponent<RectTransform>();

        if (targetZone == null)
            targetZone = transform.Find("UI/GaugeRoot/GaugeBG/TargetZone")?.GetComponent<RectTransform>();

        if (perfectZone == null)
            perfectZone = transform.Find("UI/GaugeRoot/GaugeBG/PerfectZone")?.GetComponent<RectTransform>();

        if (resultLabel == null)
            resultLabel = transform.Find("UI/ResultLabel")?.GetComponent<TMP_Text>();

        if (cup == null)
            cup = transform.Find("UI/Cup")?.GetComponent<RectTransform>();

        if (spoon == null)
            spoon = transform.Find("UI/Spoon")?.GetComponent<RectTransform>();

        if (spoonInCup == null)
            spoonInCup = transform.Find("UI/SpoonInCup")?.GetComponent<RectTransform>();

        // orbitCenter는 StartGame에서 spoonInCup/cup로 결정
    }
}
