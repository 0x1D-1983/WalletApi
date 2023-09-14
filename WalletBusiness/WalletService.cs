using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;
using WalletDomain;

namespace WalletBusiness;

public class WalletService : IWalletService
{
    private readonly ILogger<WalletService> logger;
    private readonly IDistributedLockFactory redLockFactory;
    private readonly IDatabase db;

    private const string WALLET = "wallet";

    public WalletService(
        ILogger<WalletService> logger,
        IDistributedLockFactory redLockFactory,
        IDatabase db)
    {
        this.logger = logger;
        this.redLockFactory = redLockFactory;
        this.db = db;
    }

    public async Task<bool> AddMoney(decimal amount)
    {
        try
        {
            var expiry = TimeSpan.FromSeconds(30);

            await using (var redLock = await redLockFactory.CreateLockAsync(WALLET, expiry))
            {
                if (!redLock.IsAcquired)
                {
                    throw new Exception("Failed acquiring mutex access.");
                }

                DecimalValue credit = new DecimalValue(amount);
                BalanceDetailsModel balance = await GetBalance();

                var creditCoins = CoinsHelper.GetCoinsThatAddUpAmountInf(CoinsHelper.Denominations, credit.Nanos);
                var creditCoinsQty = CoinsHelper.CoinListToQuantityDictionary(creditCoins);
                var newCoinsQtys = CoinsHelper.AddCoinQuantities(balance.ToQuantityDictionary(), creditCoinsQty);

                var newBalance = new BalanceDetailsModel(balance.Pounds + credit.Units, newCoinsQtys);

                return await db.StringSetAsync(WALLET, JsonSerializer.Serialize(balance));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Credit transacton failed.");
            throw new Exception("Credit transacton failed.", ex);
        }
    }

    public async Task<BalanceDetailsModel> WithdrawMoney(decimal amount)
    {
        try
        {
            var expiry = TimeSpan.FromSeconds(30);
            await using (var redLock = await redLockFactory.CreateLockAsync(WALLET, expiry))
            {
                if (!redLock.IsAcquired)
                {
                    throw new Exception("Failed acquiring mutex access.");
                }

                DecimalValue debit = new DecimalValue(amount);
                BalanceDetailsModel balance = await GetBalance();

                if (debit.Units > balance.Pounds) throw new InvalidOperationException("Not enough money in wallet.");

                var balanceCoinsQtys = balance.ToQuantityDictionary();

                var debitCoins = CoinsHelper.GetCoinsThatAddUpAmount(
                    balanceCoinsQtys.Keys.ToList(), balanceCoinsQtys.Values.ToList(), debit.Nanos);

                // if nanos is present in amount and if no coins available to fulfill it then round up to nearest pound
                if (debit.Nanos > 0 && debitCoins.Count == 0 && balance.Pounds >= debit.Units + 1)
                {
                    amount = debit.Units + 1;
                    debit = new DecimalValue(amount);
                }

                var debitCoinsQty = CoinsHelper.CoinListToQuantityDictionary(debitCoins);
                var newBalanceCoinsQtys = CoinsHelper.SubtractCoinQuantities(balanceCoinsQtys, debitCoinsQty);

                var newBalance = new BalanceDetailsModel(balance.Pounds - debit.Units, newBalanceCoinsQtys);

                await db.StringSetAsync(WALLET, JsonSerializer.Serialize(newBalance));

                return new BalanceDetailsModel(debit.Units, debitCoinsQty);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Debit transacton failed.");
            throw;
        }
    }

    public async Task<BalanceDetailsModel> GetBalance()
    {
        try
        {
            var expiry = TimeSpan.FromSeconds(30);

            await using (var redLock = await redLockFactory.CreateLockAsync(WALLET, expiry))
            {
                if (!redLock.IsAcquired)
                {
                    throw new Exception("Failed acquiring mutex access.");
                }

                RedisValue redisValue = await db.StringGetAsync(WALLET);

                if (redisValue.HasValue)
                {
#pragma warning disable
                    return JsonSerializer.Deserialize<BalanceDetailsModel>(redisValue) ??
                        new BalanceDetailsModel(0, CoinsHelper.InitQuantityDictionary());
#pragma warning restore
                }
                else
                {
                    return new BalanceDetailsModel(0, CoinsHelper.InitQuantityDictionary());
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Balance operation failed.");
            throw new Exception("Balance operation failed.", ex);
        }
    }
}