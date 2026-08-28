using AccountsService.DTOs.Accounts;
using AccountsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountsService.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public sealed class AccountsController(IAccountService accountService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request)
    {
        var account = await accountService.CreateAsync(request);

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetAll()
    {
        var accounts = await accountService.GetAllAsync();

        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountResponse>> GetById(Guid id)
    {
        var account = await accountService.GetByIdAsync(id);

        return Ok(account);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        await accountService.CloseAsync(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/deposit")]
    public async Task<ActionResult<AccountResponse>> Deposit(Guid id, AccountAmountRequest request)
    {
        var account = await accountService.DepositAsync(id, request);

        return Ok(account);
    }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult<AccountResponse>> Withdraw(Guid id, AccountAmountRequest request)
    {
        var account = await accountService.WithdrawAsync(id, request);

        return Ok(account);
    }

    [HttpGet("{id:guid}/operations")]
    public async Task<ActionResult<IReadOnlyList<AccountOperationResponse>>> GetOperations(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var operations = await accountService.GetOperationsAsync(id, page, pageSize);

        return Ok(operations);
    }
}
