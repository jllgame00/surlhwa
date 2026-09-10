using UnityEngine;

/// <summary>
/// Shared wrapped-angle input metric from I5. It is intentionally a small
/// tracker, not a generalized input framework; I9B reuses it for short
/// Serving Glass GentleStir actions.
/// </summary>
public sealed class StirTechniqueTracker
{
    private const float DirectionEpsilon = 0.1f;

    private readonly float maximumAngularJumpDeg;
    private bool hasReferenceAngle;
    private float previousAngleDeg;
    private int lastDirection;
    private int validMotionSamples;
    private int directionChanges;
    private float validMotionSeconds;

    public float ElapsedSeconds { get; private set; }
    public float TotalRotationDegrees { get; private set; }
    /// <summary>
    /// Diagnostic-only count of implausible cursor jumps rejected by the
    /// existing input guard. It never participates in technique grading.
    /// </summary>
    public int RejectedJumpCount { get; private set; }
    public float Revolutions => TotalRotationDegrees / 360f;
    public float AverageAngularSpeedDegPerSecond => validMotionSeconds > 0f
        ? TotalRotationDegrees / validMotionSeconds
        : 0f;

    public float Consistency
    {
        get
        {
            if (ElapsedSeconds <= 0f || validMotionSamples <= 0)
                return 0f;

            float continuity = Mathf.Clamp01(validMotionSeconds / ElapsedSeconds);
            float directionStability = validMotionSamples <= 1
                ? 1f
                : Mathf.Clamp01(1f - ((float)directionChanges / (validMotionSamples - 1)));

            return Mathf.Clamp01((continuity * 0.5f) + (directionStability * 0.5f));
        }
    }

    public StirTechniqueTracker(float maximumAngularJumpDeg)
    {
        this.maximumAngularJumpDeg = Mathf.Max(1f, maximumAngularJumpDeg);
    }

    public void Reset()
    {
        hasReferenceAngle = false;
        previousAngleDeg = 0f;
        lastDirection = 0;
        validMotionSamples = 0;
        directionChanges = 0;
        validMotionSeconds = 0f;
        ElapsedSeconds = 0f;
        TotalRotationDegrees = 0f;
        RejectedJumpCount = 0;
    }

    public void RecordIdle(float deltaTime)
    {
        ElapsedSeconds += Mathf.Max(0f, deltaTime);
        hasReferenceAngle = false;
    }

    public bool RecordAngle(float angleDeg, float deltaTime)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        ElapsedSeconds += safeDeltaTime;

        if (!IsFinite(angleDeg))
        {
            hasReferenceAngle = false;
            return false;
        }

        if (!hasReferenceAngle)
        {
            previousAngleDeg = angleDeg;
            hasReferenceAngle = true;
            return false;
        }

        float deltaAngle = Mathf.DeltaAngle(previousAngleDeg, angleDeg);
        previousAngleDeg = angleDeg;

        float absoluteDelta = Mathf.Abs(deltaAngle);
        if (absoluteDelta > maximumAngularJumpDeg)
        {
            RejectedJumpCount++;
            return false;
        }

        if (absoluteDelta <= DirectionEpsilon || safeDeltaTime <= 0f)
            return false;

        int direction = deltaAngle > 0f ? 1 : -1;
        if (lastDirection != 0 && direction != lastDirection)
            directionChanges++;

        lastDirection = direction;
        validMotionSamples++;
        validMotionSeconds += safeDeltaTime;
        TotalRotationDegrees += absoluteDelta;
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
