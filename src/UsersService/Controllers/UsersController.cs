using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsersService.DTOs.Users;
using UsersService.Services;

namespace UsersService.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(
    IUserService userService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Client,Employee")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var user = await userService.GetByIdAsync(userId, cancellationToken);

        return Ok(user);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await userService.GetByIdAsync(id, cancellationToken);

        return Ok(user);
    }

    [HttpGet]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await userService.GetAllAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPatch("{id:guid}/block")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Block(Guid id, CancellationToken cancellationToken)
    {
        await userService.BlockAsync(id, cancellationToken);

        return NoContent();
    }
}
