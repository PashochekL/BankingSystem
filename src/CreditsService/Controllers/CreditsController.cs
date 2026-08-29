using CreditsService.DTOs.Credits;
using CreditsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditsService.Controllers;

[ApiController]
[Authorize(Roles = "Client,Employee")]
[Route("api/credits")]
public sealed class CreditsController(ICreditService creditService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreditResponse>> Create(CreateCreditRequest request, CancellationToken cancellationToken)
    {
        var credit = await creditService.CreateAsync(request, cancellationToken);

        return Created($"/api/credits/{credit.Id}", credit);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CreditResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var credits = await creditService.GetAllAsync(cancellationToken);

        return Ok(credits);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CreditResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var credit = await creditService.GetByIdAsync(id, cancellationToken);

        return Ok(credit);
    }

    [HttpPost("{id:guid}/repay")]
    public async Task<ActionResult<CreditResponse>> Repay(
        Guid id,
        RepayCreditRequest request,
        CancellationToken cancellationToken)
    {
        var credit = await creditService.RepayAsync(id, request, cancellationToken);

        return Ok(credit);
    }

    [HttpGet("{id:guid}/operations")]
    public async Task<ActionResult<IReadOnlyList<CreditOperationResponse>>> GetOperations(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var operations = await creditService.GetOperationsAsync(id, page, pageSize, cancellationToken);

        return Ok(operations);
    }
}
