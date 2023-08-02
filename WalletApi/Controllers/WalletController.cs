namespace EquitiWalletApp.Controllers;

using Microsoft.AspNetCore.Mvc;
using WalletBusiness;
using WalletDomain;

[ApiController]
[Route("[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWalletService wallet;

    public WalletController(
        ILogger<WalletController> logger,
        IWalletService wallet)
    {
        this.wallet = wallet;
    }

    [HttpGet]
    [Route("Balance")]
    public async Task<BalanceDetailsModel> GetBalance()
    {
        return await wallet.GetBalance();
    }

    [HttpPut]
    [Route("Credit")]
    public async Task<IActionResult> AddMoney(decimal amount)
    {
        await wallet.AddMoney(amount);
        return Accepted();
    }

    [HttpPut]
    [Route("Debit")]
    public async Task<BalanceDetailsModel> WithdrawMoney(decimal amount)
    {
        return await wallet.WithdrawMoney(amount);
    }
}
