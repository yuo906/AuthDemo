using AuthDemo.Data;
using AuthDemo.DTOs;
using AuthDemo.Entities;
using AuthDemo.Services;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    private readonly JwtService _jwtService;

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];

        using var rng = RandomNumberGenerator.Create();
        
        rng.GetBytes(randomBytes);
        
        return Convert.ToBase64String(randomBytes);
    }

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

        var accessToken = _jwtService.GenerateToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpireTime = DateTime.Now.AddDays(7);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenDto dto)
    {   
        var principal = _jwtService.GetPrincipalFromExpiredToken(dto.AccessToken);
        
        var userId = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == int.Parse(userId!));

        if (user == null)
        {
            return Unauthorized("使用者不存在");
        }

        if (user.RefreshToken != dto.RefreshToken)
        {
            return Unauthorized("刷新令牌無效");
        }

        if (user.RefreshTokenExpireTime < DateTime.Now)
        {
            return Unauthorized("刷新令牌已過期");
        }

        var accessToken = _jwtService.GenerateToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpireTime = DateTime.Now.AddDays(7);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.RefreshToken == dto.RefreshToken);

        if (user == null)
        {
            return Ok();
        }

        user.RefreshToken = null;
        user.RefreshTokenExpireTime = null;

        await _context.SaveChangesAsync();

        return Ok("登出成功");
    }
}