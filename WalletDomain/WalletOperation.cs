namespace WalletDomain;

public record WalletOperation
{
    public OperationType OperationType { get; }

    public decimal Amount { get; }

    public WalletOperation(OperationType operationType, decimal amount = default)
    {
        OperationType = operationType;
        Amount = amount;
    }
}

