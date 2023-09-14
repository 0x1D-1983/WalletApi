using RedLockNet;

namespace WalletBusiness.Tests;

public class MockRedlock : IRedLock
{
    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }

    public string Resource { get; }
    public string LockId { get; }
    public bool IsAcquired => true;
    public RedLockStatus Status => RedLockStatus.Acquired;
    public RedLockInstanceSummary InstanceSummary => new RedLockInstanceSummary();
    public int ExtendCount { get; }
}