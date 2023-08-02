using WalletDomain;

namespace WalletBusiness;

public interface IWalletService
{
    Task<BalanceDetailsModel> GetBalance();
    Task<bool> AddMoney(decimal amount);
    Task<BalanceDetailsModel> WithdrawMoney(decimal amount);
}

