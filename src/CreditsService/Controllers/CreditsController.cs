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
}
