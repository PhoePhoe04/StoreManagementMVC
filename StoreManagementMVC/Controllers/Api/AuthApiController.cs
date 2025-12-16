using BCrypt.Net;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using Store.Shared.Dtos;
using Store.Shared.DTOs;
using Store.Shared.Entities;
using StoreManagementMVC.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace StoreManagementMVC.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthApiController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Username đã tồn tại.");
            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var user = new User
            {
                Username = dto.Username,
                Password = hash,
                FullName = dto.FullName,
                Role = "customer"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            var customer = new Customer
            {
                Name = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                UserId = user.UserId
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                return Unauthorized("Sai username hoặc mật khẩu");
            var token = GenerateJwtToken(user);
            return Ok(new AuthResponse { Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role ?? "customer")
            };
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return Ok(); // Giả thành công

            var otp = new Random().Next(100000, 999999).ToString();
            user.ResetToken = otp;
            user.ResetExpiry = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("MyStore", _config["Smtp:From"]));
            emailMessage.To.Add(new MailboxAddress("", dto.Email));
            emailMessage.Subject = "Mã OTP đặt lại mật khẩu";
            emailMessage.Body = new TextPart("plain")
            {
                Text = $"Mã OTP của bạn: {otp}\nHết hạn sau 10 phút."
            };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"]!), false);
            await client.AuthenticateAsync(_config["Smtp:Username"], _config["Smtp:Password"]);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);

            return Ok();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email
                    && u.ResetToken == dto.Otp
                    && u.ResetExpiry > DateTime.UtcNow);

            if (user == null)
                return BadRequest("OTP sai hoặc hết hạn.");

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.ResetToken = null;
            user.ResetExpiry = null;
            await _context.SaveChangesAsync();

            return Ok();
        }

        public class AuthResponse
        {
            public string Token { get; set; } = string.Empty;
        }
    }
}