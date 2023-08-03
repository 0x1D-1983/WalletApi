using System.Text.Json;
using Confluent.Kafka;
using Moq;
using StackExchange.Redis;
using WalletDomain;
using static Confluent.Kafka.ConfigPropertyNames;

namespace WalletBusiness.Tests
{
	public class RedisServiceTests
	{
        private readonly Mock<IDatabase> db;
        private readonly RedisService sut;

        private const string WALLET = "wallet";

        public RedisServiceTests()
		{
            db = new Mock<IDatabase>();
            sut = new RedisService(db.Object);
		}

        public static IEnumerable<object[]> Test_data()
        {
            yield return new object[]
            {
                new BalanceDetailsModel
                {
                    Pounds = 14,
                    OnePenny = 1,
                    TwoPence = 2,
                    FivePence = 30,
                    TenPence = 5,
                    TwentyPence = 10,
                    FiftyPence = 2
                }
            };
        }

        [Theory]
        [MemberData(nameof(Test_data))]
        public async void Balance_is_set_in_redis_db(BalanceDetailsModel balance)
        {
            string json = JsonSerializer.Serialize(balance);

            db.Setup(s => s.StringSetAsync(WALLET, json, null, false, When.Always, CommandFlags.None)).ReturnsAsync(true);

            var result = await sut.SetWalletBalance(balance);
            Assert.True(result);
            db.Verify(s => s.StringSetAsync(WALLET, json, null, false, When.Always, CommandFlags.None), Times.Exactly(1));
        }

        [Theory]
        [MemberData(nameof(Test_data))]
        public async void Balance_is_returned_from_redis_db(BalanceDetailsModel balance)
        {
            string json = JsonSerializer.Serialize(balance);

            db.Setup(s => s.StringGetAsync(WALLET, CommandFlags.None)).ReturnsAsync(
                new RedisValue(json));
            
            var result = await sut.GetWalletBalance();
            Assert.Equal(balance, result);
            db.Verify(s => s.StringGetAsync(WALLET, CommandFlags.None), Times.Exactly(1));
        }

        [Fact]
        public async void Null_redis_value_results_zero_balance()
        {
            var zeroBalance = new BalanceDetailsModel();
            string json = JsonSerializer.Serialize(zeroBalance);

            db.Setup(s => s.StringGetAsync(WALLET, CommandFlags.None)).ReturnsAsync(
                RedisValue.Null);

            var result = await sut.GetWalletBalance();
            Assert.Equal(zeroBalance, result);
            db.Verify(s => s.StringGetAsync(WALLET, CommandFlags.None), Times.Exactly(1));
        }

        [Fact]
        public async void Empty_string_redis_value_results_zero_balance()
        {
            var zeroBalance = new BalanceDetailsModel();
            string json = JsonSerializer.Serialize(zeroBalance);

            db.Setup(s => s.StringGetAsync(WALLET, CommandFlags.None)).ReturnsAsync(
                RedisValue.EmptyString);

            var result = await sut.GetWalletBalance();
            Assert.Equal(zeroBalance, result);
            db.Verify(s => s.StringGetAsync(WALLET, CommandFlags.None), Times.Exactly(1));
        }
    }
}

