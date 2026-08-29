using CreditsService.DTOs.CreditTariffs;
using CreditsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditsService.Controllers;

[ApiController]
[Authorize(Roles = "Client,Employee")]
[Route("api/credit-tariffs")]
public sealed class CreditTariffsController(ICreditTariffService creditTariffService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<CreditTariffResponse>> Create(CreateCreditTariffRequest request)
    {
        var tariff = await creditTariffService.CreateAsync(request);

        return CreatedAtAction(nameof(GetById), new { id = tariff.Id }, tariff);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CreditTariffResponse>>> GetAll()
    {
        var tariffs = await creditTariffService.GetAllAsync();

        return Ok(tariffs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CreditTariffResponse>> GetById(Guid id)
    {
        var tariff = await creditTariffService.GetByIdAsync(id);

        return Ok(tariff);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<CreditTariffResponse>> Update(Guid id, UpdateCreditTariffRequest request)
    {
        var tariff = await creditTariffService.UpdateAsync(id, request);

        return Ok(tariff);
    }
}
