using Confluent.Kafka;
using RedLockNet;
using WalletBusiness;
using WalletDomain;

public class CreditWalletWorker : BackgroundService
{
    private const string TOPIC = "WalletOperations";
    private const string WALLET = "wallet";

    private readonly ILogger<CreditWalletWorker> logger;
    private readonly IConsumer<String, WalletOperation> consumer;
    private readonly IRedisService redisService;
    private readonly IDistributedLockFactory redLockFactory;

    public CreditWalletWorker(
        ILogger<CreditWalletWorker> logger,
        IConsumer<String, WalletOperation> consumer,
        IRedisService redisService,
        IDistributedLockFactory redLockFactory
        )
    {
        this.logger = logger;
        this.consumer = consumer;
        this.redisService = redisService;
        this.redLockFactory = redLockFactory;
    }

    protected virtual async Task HandleMessage(decimal amount, CancellationToken token)
    {
        try
        {
            var expiry = TimeSpan.FromSeconds(30);

            await using (var redLock = await redLockFactory.CreateLockAsync(WALLET, expiry))
            {
                if (redLock.IsAcquired)
                {
                    DecimalValue credit = new DecimalValue(amount);
                    BalanceDetailsModel balance = await redisService.GetWalletBalance();

                    var creditCoins = CoinsHelper.GetCoinsThatAddUpAmountInf(CoinsHelper.Denominations, credit.Nanos);
                    var creditCoinsQty = CoinsHelper.CoinListToQuantityDictionary(creditCoins);
                    var newCoinsQtys = CoinsHelper.AddCoinQuantities(balance.ToQuantityDictionary(), creditCoinsQty);

                    await redisService.SetWalletBalance(new BalanceDetailsModel(balance.Pounds + credit.Units, newCoinsQtys));
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
        consumer.Subscribe(TOPIC);

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
        base.Dispose();
    }
}