using WalletDomain;

namespace WalletBusiness;

public interface IRedisService
{
    Task<BalanceDetailsModel> GetWalletBalance();
    Task<bool> SetWalletBalance(BalanceDetailsModel balance);
}