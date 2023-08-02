using System.Text.Json;
using StackExchange.Redis;
using WalletDomain;

namespace WalletBusiness;

public class RedisService : IRedisService
{
    private readonly IDatabase db;
    private const string WALLET = "wallet";
    
    public RedisService(string host)
    {
        var redisConnMultiplex = ConnectionMultiplexer.Connect(host);
        db = redisConnMultiplex.GetDatabase();
    }

    public async Task SetWalletBalance(BalanceDetailsModel balance)
    {
        await db.StringSetAsync(WALLET, JsonSerializer.Serialize(balance));
    }

    public async Task<BalanceDetailsModel> GetWalletBalance()
    {
        RedisValue redisValue = await db.StringGetAsync(WALLET);

        if (redisValue.HasValue)
        {
#pragma warning disable
            return JsonSerializer.Deserialize<BalanceDetailsModel>(redisValue) ??
                new BalanceDetailsModel(0, 0, CoinsHelper.InitQuantityDictionary());
#pragma warning restore
        }
        else
        {
            return new BalanceDetailsModel(0, 0, CoinsHelper.InitQuantityDictionary());
        }
    }
}