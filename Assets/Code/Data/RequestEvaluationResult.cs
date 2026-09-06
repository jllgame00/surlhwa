public enum RequestEvaluationStatus
{
    NotApplicable,
    Satisfied,
    Unsatisfied,
    InvalidData
}

public readonly struct RequestEvaluationResult
{
    public RequestEvaluationStatus Status { get; }

    public RequestEvaluationResult(RequestEvaluationStatus status)
    {
        Status = status;
    }
}
