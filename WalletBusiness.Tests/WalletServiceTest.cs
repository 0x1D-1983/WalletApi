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

        public static IEnumerable<object[]> MembersData_for_Balance()
        {
            yield return new object[]
            {
                new BalanceDetailsModel
                {
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

        public static IEnumerable<object[]> MembersData_for_Debit()
        {
            yield return new object[]
            {
                50m,
                new BalanceDetailsModel
                {
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
                40.20m,
                new BalanceDetailsModel
                {
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

        public static IEnumerable<object[]> MembersData_for_Credit()
        {
            yield return new object[]
            {
                0m,
                new Message<String, WalletOperation>
                {
                    Key = "1",
                    Value = new WalletOperation(OperationType.Credit, 0m)
                }
            };
            yield return new object[]
            {
                10.30m,
                new Message<String, WalletOperation>
                {
                    Key = "1",
                    Value = new WalletOperation(OperationType.Credit, 10.30m)
                }
            };
            yield return new object[]
            {
                0.01m,
                new Message<String, WalletOperation>
                {
                    Key = "1",
                    Value = new WalletOperation(OperationType.Credit, 0.01m)
                }
            };
        }

        [Theory]
        [MemberData(nameof(MembersData_for_Balance))]
        public async void Balance_is_returned(BalanceDetailsModel balance)
        {
            redis.Setup(s => s.GetWalletBalance()).ReturnsAsync(balance);

            var result = await sut.GetBalance();
            Assert.Equal(balance, result);
        }

        [Theory]
        [MemberData(nameof(MembersData_for_Credit))]
        public async void Adding_money_sends_message_to_operations_topic(
            decimal amount,
            Message<String, WalletOperation> message)
        {
            string topic = "WalletOperations";

            producer.Setup(s => s.ProduceAsync(topic, message, default)).
                ReturnsAsync(new DeliveryResult<String, WalletOperation>());
            producer.Setup(s => s.Flush((CancellationToken)default));

            var result = await sut.AddMoney(amount);
            Assert.True(result);
            producer.Verify(s => s.ProduceAsync(topic,
                It.IsAny<Message<String, WalletOperation>>(), default), Times.Exactly(1));
            producer.Verify(s => s.Flush((CancellationToken)default), Times.Exactly(1));
        }

        [Fact]
        public async void Adding_money_returns_false_when_exception()
        {
            string topic = "WalletOperations";

            producer.Setup(s => s.ProduceAsync(topic, It.IsAny<Message<String, WalletOperation>>(), default))
                .ThrowsAsync(new Exception("Random"));

            var result = await sut.AddMoney(10.30m);
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(MembersData_for_Debit))]
        public async void Money_withdrawal_updates_balance_correctly(
            decimal amount, BalanceDetailsModel initialBalance,
            BalanceDetailsModel newBalance, BalanceDetailsModel withdrawn)
        {
            redis.Setup(s => s.GetWalletBalance()).ReturnsAsync(initialBalance);

            var result = await sut.WithdrawMoney(amount);
            Assert.Equal(withdrawn, result);
            redis.Verify(s => s.SetWalletBalance(newBalance), Times.Exactly(1));
        }
    }
}

