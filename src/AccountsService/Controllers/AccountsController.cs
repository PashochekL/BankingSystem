using AccountsService.DTOs.Accounts;
using AccountsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountsService.Controllers;

[ApiController]
[Authorize(Roles = "Client,Employee")]
[Route("api/accounts")]
public sealed class AccountsController(IAccountService accountService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await accountService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var accounts = await accountService.GetAllAsync(cancellationToken);

        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var account = await accountService.GetByIdAsync(id, cancellationToken);

        return Ok(account);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await accountService.CloseAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/deposit")]
    public async Task<ActionResult<AccountResponse>> Deposit(
        Guid id,
        AccountAmountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await accountService.DepositAsync(id, request, cancellationToken);

        return Ok(account);
    }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult<AccountResponse>> Withdraw(
        Guid id,
        AccountAmountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await accountService.WithdrawAsync(id, request, cancellationToken);

        return Ok(account);
    }

    [HttpGet("{id:guid}/operations")]
    public async Task<ActionResult<IReadOnlyList<AccountOperationResponse>>> GetOperations(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var operations = await accountService.GetOperationsAsync(id, page, pageSize, cancellationToken);

        return Ok(operations);
    }
}
