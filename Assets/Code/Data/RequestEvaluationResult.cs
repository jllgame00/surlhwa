public enum RequestEvaluationStatus
{
    NotApplicable,
    Satisfied,
    Unsatisfied,
    InvalidData
}

public readonly struct RequestEvaluationResult
{
    public GuestRequestType RequestType { get; }
    public RequestEvaluationStatus Status { get; }
    public bool IsApplicable => Status == RequestEvaluationStatus.Satisfied ||
                                Status == RequestEvaluationStatus.Unsatisfied;

    public RequestEvaluationResult(GuestRequestType requestType, RequestEvaluationStatus status)
    {
        RequestType = requestType;
        Status = status;
    }
}
