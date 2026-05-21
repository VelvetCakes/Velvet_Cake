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
    private readonly IEmailService _emailService;

    public AuthController(ApplicationDbContext db, IConfiguration config, IEmailService emailService)
    {
        _db = db;
        _config = config;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            Console.WriteLine($"=== REGISTER ATTEMPT ===");
            Console.WriteLine($"Email: {dto?.Email}");
            Console.WriteLine($"Name: {dto?.Name}");

            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Все поля обязательны" });

            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email уже используется" });

            if (dto.Password.Length < 6)
                return BadRequest(new { message = "Пароль должен содержать не менее 6 символов" });

            var userRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "user");
            if (userRole == null)
                return StatusCode(500, new { message = "Роль 'user' не найдена" });

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

            bool emailSent = false;
            string errorMessage = null;

            if (_emailService != null)
            {
                try
                {
                    var frontendUrl = _config["FrontendUrl"] ?? "https://velvetcakes.github.io";
                    var confirmationLink = $"{frontendUrl}/confirm-email.html?userId={user.Id}";
                    var emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; background-color: #f9f0ff; padding: 20px;'>
                            <div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; padding: 30px;'>
                                <h2 style='color: #E85FB9; text-align: center;'>Добро пожаловать в Velvet!</h2>
                                <p>Здравствуйте, <strong>{dto.Name}</strong>!</p>
                                <p>Вы отправили заявку на регистрацию в нашем магазине десертов.</p>
                                <p><strong style='color: #E85FB9;'>Для завершения регистрации необходимо подтвердить ваш email.</strong></p>
                                <div style='text-align: center; margin: 30px 0;'>
                                    <a href='{confirmationLink}' style='background: #E85FB9; color: white; padding: 12px 30px; text-decoration: none; border-radius: 40px; font-weight: bold;'>Подтвердить email</a>
                                </div>
                                <p>Или скопируйте ссылку в браузер:</p>
                                <p style='background: #f5f5f5; padding: 10px; border-radius: 8px; word-break: break-all;'>{confirmationLink}</p>
                                <p>Ссылка действительна в течение 24 часов.</p>
                                <br>
                                <p style='color: #666;'>Если вы не регистрировались, просто проигнорируйте это письмо.</p>
                                <hr style='margin: 20px 0; border: none; border-top: 1px solid #eee;'>
                                <p style='text-align: center; color: #999; font-size: 12px;'>С любовью, команда Velvet 💕</p>
                            </div>
                        </body>
                        </html>";

                    await _emailService.SendEmailAsync(dto.Email, "Подтверждение регистрации в Velvet", emailBody);
                    emailSent = true;
                    Console.WriteLine($"Confirmation email sent to {dto.Email}");
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    Console.WriteLine($"Email sending failed: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }

            if (emailSent)
            {
                return Ok(new
                {
                    success = true,
                    message = "Регистрация требует подтверждения! На вашу почту отправлено письмо. Перейдите по ссылке в письме, чтобы завершить регистрацию.",
                    requiresConfirmation = true,
                    email = dto.Email
                });
            }
            else
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message = $"Не удалось отправить письмо подтверждения. Ошибка: {errorMessage}. Пожалуйста, попробуйте позже или свяжитесь с поддержкой.",
                    emailError = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION in Register: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, new { message = $"Внутренняя ошибка сервера: {ex.Message}" });
        }
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
    {
        var user = await _db.Users.FindAsync(dto.UserId);
        if (user == null)
            return BadRequest(new { message = "Пользователь не найден" });

        if (user.IsEmailConfirmed)
            return BadRequest(new { message = "Email уже подтверждён" });

        user.IsEmailConfirmed = true;
        user.EmailConfirmationToken = null;
        await _db.SaveChangesAsync();

        Console.WriteLine($"User {user.Email} confirmed email successfully - registration completed!");

        return Ok(new
        {
            success = true,
            message = "Email подтверждён! Регистрация завершена. Теперь вы можете войти в аккаунт.",
            email = user.Email
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Неверный email или пароль" });

        if (!user.IsEmailConfirmed)
        {
            return Unauthorized(new
            {
                message = "Регистрация не завершена! Подтвердите email, перейдя по ссылке в письме.",
                requiresConfirmation = true,
                email = user.Email,
                userId = user.Id
            });
        }

        var token = GenerateJwtToken(user);
        return Ok(new
        {
            token,
            user = new { user.FullName, user.Email, Role = user.Role.Name }
        });
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return BadRequest(new { message = "Пользователь не найден" });

        if (user.IsEmailConfirmed)
            return BadRequest(new { message = "Email уже подтверждён, регистрация завершена" });

        try
        {
            var frontendUrl = _config["FrontendUrl"] ?? "https://velvetcakes.github.io";
            var confirmationLink = $"{frontendUrl}/confirm-email.html?userId={user.Id}";
            var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; background-color: #f9f0ff; padding: 20px;'>
                    <div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; padding: 30px;'>
                        <h2 style='color: #E85FB9; text-align: center;'>Подтверждение регистрации в Velvet</h2>
                        <p>Здравствуйте, <strong>{user.FullName}</strong>!</p>
                        <p>Вы запросили повторную отправку письма с подтверждением email.</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{confirmationLink}' style='background: #E85FB9; color: white; padding: 12px 30px; text-decoration: none; border-radius: 40px; font-weight: bold;'>Подтвердить email</a>
                        </div>
                        <p>Или скопируйте ссылку в браузер:</p>
                        <p style='background: #f5f5f5; padding: 10px; border-radius: 8px; word-break: break-all;'>{confirmationLink}</p>
                        <br>
                        <p style='color: #666;'>Если вы не регистрировались, просто проигнорируйте это письмо.</p>
                        <hr style='margin: 20px 0; border: none; border-top: 1px solid #eee;'>
                        <p style='text-align: center; color: #999; font-size: 12px;'>С любовью, команда Velvet 💕</p>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(user.Email, "Подтверждение регистрации в Velvet", emailBody);

            return Ok(new
            {
                success = true,
                message = "Письмо с подтверждением отправлено повторно. Проверьте почту, чтобы завершить регистрацию."
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Resend confirmation failed: {ex.Message}");
            return StatusCode(500, new { message = "Ошибка отправки письма: " + ex.Message });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Неверный текущий пароль" });

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
}

public class ResendConfirmationDto
{
    public string Email { get; set; } = string.Empty;
}

public class ConfirmEmailDto
{
    public int UserId { get; set; }
}