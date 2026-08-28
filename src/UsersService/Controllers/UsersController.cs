using Microsoft.AspNetCore.Mvc;
using UsersService.DTOs.Users;
using UsersService.Services;

namespace UsersService.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
    {
        var user = await userService.CreateAsync(request);
        if (user is null)
        {
            return Conflict();
        }

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await userService.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

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
        var isBlocked = await userService.BlockAsync(id);
        if (!isBlocked)
        {
            return NotFound();
        }

        return NoContent();
    }
}
