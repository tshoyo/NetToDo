using Microsoft.AspNetCore.Mvc;
using NetToDo.Models;
using NetToDo.Data;
using NetToDo.Services;
using Microsoft.EntityFrameworkCore;

namespace NetToDo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;

    public AuthController(AppDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists.");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = _authService.HashPassword(dto.Password),
            AvatarUrl = _authService.GetGravatarUrl(dto.Email)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Token = _authService.GenerateToken(user) });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !_authService.VerifyPassword(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        return Ok(new { Token = _authService.GenerateToken(user) });
    }
}

public record RegisterDto(string Name, string Email, string Password);
public record LoginDto(string Email, string Password);
