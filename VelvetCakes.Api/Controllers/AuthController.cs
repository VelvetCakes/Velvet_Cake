using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VelvetCakes.Api.Models;
using VelvetCakes.Api.DTOs;
using VelvetCakes.Api.Services;

namespace VelvetCakes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Все поля обязательны");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email уже используется");

        var userRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "user");
        if (userRole == null) return StatusCode(500, "Роль 'user' не найдена");

        var user = new User
        {
            FullName = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = userRole.Id,
            IsEmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Отправка подтверждающего письма
        try
        {
            var confirmationLink = $"{_config["FrontendUrl"]}/confirm-email?userId={user.Id}";
            var emailBody = $@"
            <h2>Добро пожаловать в Velvet!</h2>
            <p>Здравствуйте, {dto.Name}!</p>
            <p>Вы успешно зарегистрировались в нашем магазине десертов.</p>
            <p>Для подтверждения email перейдите по ссылке:</p>
            <a href='{confirmationLink}'>Подтвердить email</a>
            <p>Если вы не регистрировались, просто проигнорируйте это письмо.</p>
            <br>
            <p>С любовью, команда Velvet 💕</p>";

            await _emailService.SendEmailAsync(dto.Email, "Добро пожаловать в Velvet!", emailBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email sending failed: {ex.Message}");
            // Не возвращаем ошибку, чтобы регистрация прошла успешно
        }

        return Ok(new { message = "Регистрация успешна! На вашу почту отправлено письмо с подтверждением." });
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
    {
        var user = await _db.Users.FindAsync(dto.UserId);
        if (user == null)
            return BadRequest("Пользователь не найден");

        user.IsEmailConfirmed = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Email подтверждён!" });
    }

    public class ConfirmEmailDto
    {
        public int UserId { get; set; }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Неверный email или пароль");

        var token = GenerateJwtToken(user);
        return Ok(new
        {
            token,
            user = new { user.FullName, user.Email, Role = user.Role.Name }
        });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return NotFound("Пользователь не найден");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest("Неверный текущий пароль");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Пароль успешно изменён" });
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "ThisIsMyVerySecureSecretKey123!");
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddDays(7),
            claims: claims,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private readonly IEmailService _emailService;

    public AuthController(ApplicationDbContext db, IConfiguration config, IEmailService emailService)
    {
        _db = db;
        _config = config;
        _emailService = emailService;
    }
}