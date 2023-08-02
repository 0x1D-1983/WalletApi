using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using WalletDomain;

namespace WalletBusiness;

public class WalletService : IWalletService, IDisposable
{
    private readonly ILogger<WalletService> logger;
    private IProducer<String, WalletOperation> producer;
    private IRedisService redisService;
    private string topic = "WalletOperations";

    private readonly RedLockFactory redlockFactory;
    private const string WALLET = "wallet";

    public WalletService(
        ILogger<WalletService> logger,
        IProducer<String, WalletOperation> producer,
        IRedisService redisService)
    {
        this.logger = logger;
        this.producer = producer;
        this.redisService = redisService;

        var redisConnMultiplex = ConnectionMultiplexer.Connect("localhost:6379");

        var multiplexers = new List<RedLockMultiplexer> { redisConnMultiplex };
        redlockFactory = RedLockFactory.Create(multiplexers);
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
        await using (var redLock = await redlockFactory.CreateLockAsync(WALLET, expiry))
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

    public void Dispose()
    {
        producer.Dispose();
        redlockFactory.Dispose();
    }
}