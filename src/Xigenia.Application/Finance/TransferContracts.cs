namespace Xigenia.Application.Finance;

public sealed record CreateTransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    DateOnly MovementDate,
    string? Note);

public sealed record TransferResponse(
    Guid TransferGroupId,
    MoneyMovementResponse SourceLeg,
    MoneyMovementResponse DestinationLeg);
