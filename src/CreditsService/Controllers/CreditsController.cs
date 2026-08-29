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
    public async Task<ActionResult<CreditResponse>> Create(CreateCreditRequest request)
    {
        var credit = await creditService.CreateAsync(request);

        return Created($"/api/credits/{credit.Id}", credit);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CreditResponse>>> GetAll()
    {
        var credits = await creditService.GetAllAsync();

        return Ok(credits);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CreditResponse>> GetById(Guid id)
    {
        var credit = await creditService.GetByIdAsync(id);

        return Ok(credit);
    }

    [HttpPost("{id:guid}/repay")]
    public async Task<ActionResult<CreditResponse>> Repay(Guid id, RepayCreditRequest request)
    {
        var credit = await creditService.RepayAsync(id, request);

        return Ok(credit);
    }
}
