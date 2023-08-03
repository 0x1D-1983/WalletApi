using System.Net;
using EquitiWalletApp.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WalletBusiness;
using WalletDomain;
using static Confluent.Kafka.ConfigPropertyNames;

namespace WalletApi.Tests;

public class WalletControllerTest
{
    private readonly Mock<ILogger<WalletController>> logger;
    private readonly Mock<IWalletService> wallet;

    private readonly WalletController controller;

    public WalletControllerTest()
    {
        logger = new Mock<ILogger<WalletController>>();
        wallet = new Mock<IWalletService>();

        controller = new WalletController(logger.Object, wallet.Object);
    }

    [Fact]
    public async void Balance_is_returned_from_wallet()
    {
        var balance = new BalanceDetailsModel
        {
            Pounds = 12,
            TenPence = 1,
            OnePenny = 2
        };

        wallet.Setup(s => s.GetBalance()).ReturnsAsync(balance);

        var actionResult = await controller.GetBalance();
        var contentResult = actionResult.Value;

        Assert.Equal(balance, contentResult);
    }

    [Fact]
    public async void Money_is_added()
    {
        decimal amount = 12.34m;
        wallet.Setup(s => s.AddMoney(amount)).ReturnsAsync(true);

        var actionResult = await controller.AddMoney(amount);

        Assert.IsType<AcceptedResult>(actionResult);
    }

    [Fact]
    public async void Adding_money_returns_Teapot_status()
    {
        wallet.Setup(s => s.AddMoney(0)).ReturnsAsync(false);

        var actionResult = await controller.AddMoney(0) as ObjectResult;

        Assert.Equal(StatusCodes.Status418ImATeapot, actionResult.StatusCode);
    }

    [Fact]
    public async void Money_is_withdrawn()
    {
        decimal amount = 12.34m;
        var returned = new BalanceDetailsModel
        {
            Pounds = 12,
            TenPence = 1,
            TwentyPence = 1,
            TwoPence = 2
        };

        wallet.Setup(s => s.WithdrawMoney(amount)).ReturnsAsync(returned);

        var actionResult = await controller.WithdrawMoney(amount);
        var contentResult = actionResult.Value;

        //Assert.IsType<AcceptedResult>(actionResult);
        Assert.Equal(returned, contentResult);
    }

    [Fact]
    public async void Money_withdrawal_returns_Teapot_status()
    {
        wallet.Setup(s => s.WithdrawMoney(0)).ThrowsAsync(
            new InvalidOperationException("Not enough money in wallet."));

        var actionResult = await controller.WithdrawMoney(0);

        Assert.Equal(StatusCodes.Status418ImATeapot,
            ((ObjectResult)actionResult.Result).StatusCode);
    }
}
