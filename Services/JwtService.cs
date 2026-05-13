using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthDemo.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AuthDemo.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        // 建立 JWT Payload 內的使用者資料
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        // 產生 JWT 簽章用的密鑰
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!)
        );

        // 建立簽章憑證
        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256 // HS256 對稱加密，簽名和驗證使用相同的金鑰，較快
        );                                // RS256 非對稱加密，簽名和驗證使用不同的金鑰，較安全 (公鑰用於加密，私鑰用於解密)

        // 建立 JWT Token
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])
            ),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}