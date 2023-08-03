using System;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using RedLockNet;
using WalletDomain;

namespace WalletBusiness.Tests
{
	public class WalletServiceTest
	{
        private readonly Mock<ILogger<WalletService>> logger;
        private readonly Mock<IProducer<String, WalletOperation>> producer;
        private readonly Mock<IRedisService> redis;
        private readonly Mock<IDistributedLockFactory> redLockFactory;

        private readonly WalletService sut;

        public WalletServiceTest()
        {
            logger = new Mock<ILogger<WalletService>>();
            producer = new Mock<IProducer<string, WalletOperation>>();
            redis = new Mock<IRedisService>();
            redLockFactory = new Mock<IDistributedLockFactory>();

            redLockFactory.Setup(x => x.CreateLockAsync(It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken?>()))
            .ReturnsAsync(new MockRedlock());

            sut = new WalletService(logger.Object, producer.Object, redis.Object, redLockFactory.Object);
        }

        public static IEnumerable<object[]> Data_for_Balance_is_returned()
        {
            yield return new object[]
            {
                new BalanceDetailsModel
                {
                    Total = 0m,
                    Pounds = 0,
                    OnePenny = 0,
                    TwoPence = 0,
                    FivePence = 0,
                    TenPence = 0,
                    TwentyPence = 0,
                    FiftyPence = 0
                }
            };
            yield return new object[]
            {
                new BalanceDetailsModel
                {
                    Total = 10.30m,
                    Pounds = 10,
                    OnePenny = 0,
                    TwoPence = 0,
                    FivePence = 0,
                    TenPence = 1,
                    TwentyPence = 1,
                    FiftyPence = 0
                }
            };
            yield return new object[]
            {
                new BalanceDetailsModel
                {
                    Total = 1.23m,
                    Pounds = 1,
                    OnePenny = 1,
                    TwoPence = 1,
                    FivePence = 0,
                    TenPence = 0,
                    TwentyPence = 1,
                    FiftyPence = 0
                }
            };
        }

        public static IEnumerable<object[]> Data_for_Money_withdrawal_updates_balance_correctly()
        {
            yield return new object[]
            {
                new BalanceDetailsModel
                {
                    Total = 100.20m,
                    Pounds = 100,
                    OnePenny = 0,
                    TwoPence = 0,
                    FivePence = 0,
                    TenPence = 0,
                    TwentyPence = 1,
                    FiftyPence = 0
                },
                new BalanceDetailsModel
                {
                    Total = 50.20m,
                    Pounds = 50,
                    OnePenny = 0,
                    TwoPence = 0,
                    FivePence = 0,
                    TenPence = 0,
                    TwentyPence = 1,
                    FiftyPence = 0
                },
                new BalanceDetailsModel
                {
                    Total = 50m,
                    Pounds = 50,
                    OnePenny = 0,
                    TwoPence = 0,
                    FivePence = 0,
                    TenPence = 0,
                    TwentyPence = 0,
                    FiftyPence = 0
                }
            };
            yield return new object[]
            {
                new BalanceDetailsModel
                {
                    Total = 100.20m,
                    Pounds = 100,
                    OnePenny = 0,
                    TwoPence = 0,
                    FivePence = 0,
                    TenPence = 0,
                    TwentyPence = 1,
                    FiftyPence = 0
                },
                new BalanceDetailsModel
                {
                    Total = 60m,
                    Pounds = 60,
                    OnePenny = 0,
                    TwoPence = 0,
                    FivePence = 0,
                    TenPence = 0,
                    TwentyPence = 0,
                    FiftyPence = 0
                },
                new BalanceDetailsModel
                {
                    Total = 40.20m,
                    Pounds = 40,
                    OnePenny = 0,
                    TwoPence = 0,
                    FivePence = 0,
                    TenPence = 0,
                    TwentyPence = 1,
                    FiftyPence = 0
                }
            };
        }

        [Theory]
        [MemberData(nameof(Data_for_Balance_is_returned))]
        public async void Balance_is_returned(BalanceDetailsModel balance)
        {
            redis.Setup(s => s.GetWalletBalance()).ReturnsAsync(balance);

            var result = await sut.GetBalance();
            Assert.Equal(balance, result);
        }

        [Theory]
        [MemberData(nameof(Data_for_Money_withdrawal_updates_balance_correctly))]
        public async void Money_withdrawal_updates_balance_correctly(
            BalanceDetailsModel initialBalance, BalanceDetailsModel newBalance, BalanceDetailsModel withdrawn)
        {
            redis.Setup(s => s.GetWalletBalance()).ReturnsAsync(initialBalance);

            var result = await sut.WithdrawMoney(withdrawn.Total);
            Assert.Equal(withdrawn, result);
            redis.Verify(s => s.SetWalletBalance(newBalance), Times.Exactly(1));
        }
    }
}

