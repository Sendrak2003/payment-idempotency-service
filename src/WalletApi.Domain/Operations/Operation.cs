using WalletApi.Domain.Exceptions;

namespace WalletApi.Domain.Operations;

public enum ReceiptOutcome
{
    Applied,
    DuplicateReplay,
    IgnoredContradictory
}

public class Operation
{
    private readonly List<OperationEvent> _events = new();

    public string OperationId { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public string? Description { get; private set; }
    public OperationStatus Status { get; private set; }
    public string? ProviderPaymentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public uint Version { get; private set; }

    public IReadOnlyCollection<OperationEvent> Events => _events;

    private Operation() { }

    public static Operation Create(string operationId, decimal amount, string currency, string? description, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            throw new InvalidAmountException("operationId is required.");
        }

        if (amount <= 0)
        {
            throw new InvalidAmountException("amount must be a positive value.");
        }

        var operation = new Operation
        {
            OperationId = operationId,
            Amount = amount,
            Currency = currency,
            Description = description,
            Status = OperationStatus.Created,
            CreatedAt = now,
            UpdatedAt = now
        };

        operation.AppendEvent(OperationEventType.Created, null, OperationStatus.Created, "Operation created", now);
        return operation;
    }

    public bool TryStartProcessing(DateTimeOffset now)
    {
        if (Status != OperationStatus.Created)
        {
            return false;
        }

        var from = Status;
        Status = OperationStatus.Processing;
        UpdatedAt = now;
        AppendEvent(OperationEventType.Processing, from, Status, "Submission intent persisted, awaiting provider result", now);
        return true;
    }

    public void RecordProviderAcceptance(string providerPaymentId, DateTimeOffset now)
    {
        if (Status is OperationStatus.Completed or OperationStatus.Rejected)
        {
            return;
        }

        if (ProviderPaymentId is not null)
        {
            return;
        }

        ProviderPaymentId = providerPaymentId;
        UpdatedAt = now;
    }

    public ReceiptOutcome ApplyReceipt(string providerPaymentId, OperationStatus receiptStatus, string? message, DateTimeOffset now)
    {
        if (receiptStatus is not (OperationStatus.Completed or OperationStatus.Rejected))
        {
            throw new ArgumentOutOfRangeException(nameof(receiptStatus), "Receipt status must be Completed or Rejected.");
        }

        if (ProviderPaymentId is not null && !string.Equals(ProviderPaymentId, providerPaymentId, StringComparison.Ordinal))
        {
            throw new ReceiptConflictException(
                $"Receipt providerPaymentId '{providerPaymentId}' does not match operation providerPaymentId '{ProviderPaymentId}'.");
        }

        if (Status is OperationStatus.Completed or OperationStatus.Rejected)
        {
            return Status == receiptStatus ? ReceiptOutcome.DuplicateReplay : ReceiptOutcome.IgnoredContradictory;
        }

        var from = Status;
        ProviderPaymentId ??= providerPaymentId;
        Status = receiptStatus;
        UpdatedAt = now;
        AppendEvent(
            receiptStatus == OperationStatus.Completed ? OperationEventType.Completed : OperationEventType.Rejected,
            from,
            Status,
            string.IsNullOrWhiteSpace(message) ? $"Provider receipt confirmed {receiptStatus}" : message,
            now);
        return ReceiptOutcome.Applied;
    }

    private void AppendEvent(OperationEventType type, OperationStatus? fromStatus, OperationStatus toStatus, string message, DateTimeOffset now)
    {
        var sequenceNumber = _events.Count + 1;
        _events.Add(new OperationEvent(OperationId, sequenceNumber, type, fromStatus, toStatus, message, now));
    }
}
