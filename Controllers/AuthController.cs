using AuthDemo.Data;
using AuthDemo.DTOs;
using AuthDemo.Entities;
using AuthDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    private readonly JwtService _jwtService;

    public AuthController(
        AppDbContext context,
        JwtService jwtService
    )
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var exists = await _context.Users
            .AnyAsync(x => x.Account == dto.Account);

        if (exists)
        {
            return BadRequest("帳號已存在");
        }

        var user = new User
        {
            Account = dto.Account,
            Name = dto.Name,

            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return Ok("註冊成功");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Account == dto.Account);

        if (user == null)
        {
            return Unauthorized("帳號或密碼錯誤");
        }

        var valid = BCrypt.Net.BCrypt.Verify(
            dto.Password,
            user.PasswordHash
        );

        if (!valid)
        {
            return Unauthorized("帳號或密碼錯誤");
        }

        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            Token = token
        });
    }
}