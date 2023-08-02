using Confluent.Kafka;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using WalletBusiness;
using WalletDomain;

public class CreditWalletWorker : BackgroundService
{
    private const string _operationTopic = "WalletOperations";

    private readonly ILogger<CreditWalletWorker> logger;
    private readonly IConsumer<String, WalletOperation> consumer;
    private IRedisService redisService;

    private readonly RedLockFactory redlockFactory;
    private const string WALLET = "wallet";

    public CreditWalletWorker(
        ILogger<CreditWalletWorker> logger,
        IConsumer<String, WalletOperation> consumer,
        IRedisService redisService
        )
    {
        this.logger = logger;
        this.consumer = consumer;
        this.redisService = redisService;
        
        var redisConnMultiplex = ConnectionMultiplexer.Connect("localhost:6379");

        var multiplexers = new List<RedLockMultiplexer> { redisConnMultiplex };
        redlockFactory = RedLockFactory.Create(multiplexers);
    }

    protected virtual async Task HandleMessage(decimal amount, CancellationToken token)
    {
        try
        {
            var expiry = TimeSpan.FromSeconds(30);

            await using (var redLock = await redlockFactory.CreateLockAsync(WALLET, expiry))
            {
                if (redLock.IsAcquired)
                {
                    DecimalValue credit = new DecimalValue(amount);
                    BalanceDetailsModel balance = await redisService.GetWalletBalance();

                    int pounds = balance.Pounds;
                    decimal total = balance.Total;

                    total += amount;
                    pounds += credit.Units;

                    var creditCoins = CoinsHelper.GetCoinsThatAddUpAmountInf(CoinsHelper.Denominations, credit.Nanos);
                    var creditCoinsQty = CoinsHelper.CoinListToQuantityDictionary(creditCoins);
                    var newCoinsQtys = CoinsHelper.AddCoinQuantities(balance.ToQuantityDictionary(), creditCoinsQty);

                    await redisService.SetWalletBalance(new BalanceDetailsModel(total, pounds, newCoinsQtys));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Credit transacton failed.");
            throw new Exception("Credit transacton failed.", ex);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        consumer.Subscribe(_operationTopic);

        while (!token.IsCancellationRequested)
        {
            var result = consumer.Consume(token);
            await HandleMessage(result.Message.Value.Amount, token);
        }

        consumer.Close();
    }

    public override void Dispose()
    {
        consumer.Dispose();
        redlockFactory.Dispose();
        base.Dispose();
    }
}