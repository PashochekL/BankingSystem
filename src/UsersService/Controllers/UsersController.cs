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
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
    {
        var user = await userService.CreateAsync(request);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var user = await userService.GetByIdAsync(userId);

        return Ok(user);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await userService.GetByIdAsync(id);

        return Ok(user);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll()
    {
        var users = await userService.GetAllAsync();

        return Ok(users);
    }

    [HttpPatch("{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id)
    {
        await userService.BlockAsync(id);

        return NoContent();
    }
}
