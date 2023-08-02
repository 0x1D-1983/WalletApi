using WalletDomain;

namespace WalletBusiness;

public interface IRedisService
{
    Task<BalanceDetailsModel> GetWalletBalance();
    Task SetWalletBalance(BalanceDetailsModel balance);
}