using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase {
    private readonly AppDb _db;
    public AuthController(AppDb db) => _db = db;

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterDto dto) {
        if (_db.Users.Any(u => u.Email == dto.Email)) return Conflict("Email занят");
        var u = new User {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "client",
            FullName = dto.FullName,
            Phone = dto.Phone ?? ""
        };
        _db.Users.Add(u);
        _db.SaveChanges();
        return Ok(new { token = GenToken(u), role = u.Role });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto) {
        var u = _db.Users.FirstOrDefault(x => x.Email == dto.Email);
        if (u == null || !BCrypt.Net.BCrypt.Verify(dto.Password, u.PasswordHash))
            return Unauthorized("Неверный email или пароль");
        return Ok(new { token = GenToken(u), role = u.Role, name = u.FullName });
    }

    private string GenToken(User u) {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SUPER_SECRET_KEY_FOR_RENTAPP_PRACTICE_1234567890"));
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Email, u.Email),
            new Claim(ClaimTypes.Role, u.Role)
        };
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterDto(string Email, string Password, string FullName, string? Phone);
public record LoginDto(string Email, string Password);