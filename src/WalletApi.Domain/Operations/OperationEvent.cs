namespace WalletApi.Domain.Operations;

public class OperationEvent
{
    public long Id { get; private set; }
    public string OperationId { get; private set; } = default!;
    public int SequenceNumber { get; private set; }
    public OperationEventType Type { get; private set; }
    public OperationStatus? FromStatus { get; private set; }
    public OperationStatus ToStatus { get; private set; }
    public string Message { get; private set; } = default!;
    public DateTimeOffset OccurredAt { get; private set; }

    private OperationEvent() { }

    internal OperationEvent(
        string operationId,
        int sequenceNumber,
        OperationEventType type,
        OperationStatus? fromStatus,
        OperationStatus toStatus,
        string message,
        DateTimeOffset occurredAt)
    {
        OperationId = operationId;
        SequenceNumber = sequenceNumber;
        Type = type;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Message = message;
        OccurredAt = occurredAt;
    }
}
