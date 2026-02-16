using Microsoft.AspNetCore.Mvc;
using NetToDo.Models;
using NetToDo.Data;
using NetToDo.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace NetToDo.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;

    public ProfileController(AppDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new { user.Name, user.Email, user.AvatarUrl });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.Name = dto.Name ?? user.Name;
        if (dto.Email != null && dto.Email != user.Email)
        {
            user.Email = dto.Email;
            user.AvatarUrl = _authService.GetGravatarUrl(dto.Email);
        }

        if (!string.IsNullOrEmpty(dto.Password))
        {
            user.PasswordHash = _authService.HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync();
        return Ok(new { user.Name, user.Email, user.AvatarUrl });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record UpdateProfileDto(string? Name, string? Email, string? Password);
