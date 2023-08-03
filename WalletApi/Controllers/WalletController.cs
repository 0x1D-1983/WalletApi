namespace EquitiWalletApp.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WalletBusiness;
using WalletDomain;

[ApiController]
[Route("[controller]")]
public class WalletController : ControllerBase
{
    private readonly ILogger<WalletController> logger;
    private readonly IWalletService wallet;

    public WalletController(
        ILogger<WalletController> logger,
        IWalletService wallet)
    {
        this.logger = logger;
        this.wallet = wallet;
    }

    [HttpGet]
    [Route("Balance")]
    public async Task<ActionResult<BalanceDetailsModel>> GetBalance()
    {
        return await wallet.GetBalance();
    }

    [HttpPut]
    [Route("Credit")]
    public async Task<IActionResult> AddMoney(decimal amount)
    {
        if (await wallet.AddMoney(amount))
        {
            return Accepted();
        }
        else
        {
            return StatusCode(StatusCodes.Status418ImATeapot,
                "Adding money to the wallet failed, please try again later.");
        }
    }

    [HttpPut]
    [Route("Debit")]
    public async Task<ActionResult<BalanceDetailsModel>> WithdrawMoney(decimal amount)
    {
        try
        {
            return await wallet.WithdrawMoney(amount);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return StatusCode(StatusCodes.Status418ImATeapot, ex.Message);
        }
    }
}
