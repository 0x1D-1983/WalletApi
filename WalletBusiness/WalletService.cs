using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using RedLockNet;
using WalletDomain;

namespace WalletBusiness;

public class WalletService : IWalletService
{
    private readonly ILogger<WalletService> logger;
    private IProducer<String, WalletOperation> producer;
    private readonly IRedisService redisService;
    private readonly IDistributedLockFactory redLockFactory;

    private const string topic = "WalletOperations";
    private const string WALLET = "wallet";

    public WalletService(
        ILogger<WalletService> logger,
        IProducer<String, WalletOperation> producer,
        IRedisService redisService,
        IDistributedLockFactory redLockFactory)
    {
        this.logger = logger;
        this.producer = producer;
        this.redisService = redisService;
        this.redLockFactory = redLockFactory;
    }

    public async Task<bool> AddMoney(decimal amount)
    {
        try
        {
            var message = new Message<String, WalletOperation>
            {
                Key = "1",
                Value = new WalletOperation(OperationType.Credit, amount)
            };

            await producer.ProduceAsync(topic, message);
            producer.Flush();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending credit operation to Kafka");
            return false;
        }
    }

    public async Task<BalanceDetailsModel> WithdrawMoney(decimal amount)
    {
        var expiry = TimeSpan.FromSeconds(30);
        await using (var redLock = await redLockFactory.CreateLockAsync(WALLET, expiry))
        {
            DecimalValue debit = new DecimalValue(amount);
            BalanceDetailsModel balance = await redisService.GetWalletBalance();

            var balanceCoinsQtys = balance.ToQuantityDictionary();

            var debitCoins = CoinsHelper.GetCoinsThatAddUpAmount(
                balanceCoinsQtys.Keys.ToList(), balanceCoinsQtys.Values.ToList(), debit.Nanos);
            var debitCoinsQty = CoinsHelper.CoinListToQuantityDictionary(debitCoins);
            var newBalanceCoinsQtys = CoinsHelper.SubtractCoinQuantities(balanceCoinsQtys, debitCoinsQty);

            await redisService.SetWalletBalance(new BalanceDetailsModel(
                balance.Total - amount, balance.Pounds - debit.Units, newBalanceCoinsQtys));

            return new BalanceDetailsModel(amount, debit.Units, debitCoinsQty);
        }
    }

    public async Task<BalanceDetailsModel> GetBalance()
    {
        return await redisService.GetWalletBalance();
    }
}