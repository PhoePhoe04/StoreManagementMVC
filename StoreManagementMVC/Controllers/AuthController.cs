using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreManagementMVC.Data;
using Store.Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace StoreManagementMVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Allow anonymous access to endpoints in this controller (login, forgot, register)
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, ILogger<AuthController> logger, IConfiguration config)
        {
            _db = db;
            _logger = logger;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required.");

            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || user.Password != request.Password)
                return Unauthorized("Invalid username or password.");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                username = user.Username,
                role = user.Role
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username)) return Unauthorized();
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();
            return Ok(new { user.Username, user.Role, user.FullName });
        }

        [HttpPost("forgotpassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Email);
            if (user != null)
            {
                var token = System.Guid.NewGuid().ToString("N");
                _logger.LogInformation("Generated password reset token for user {Username}: {Token}", user.Username, token);
                // In real app persist token and send email link to user
            }

            return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "super_secret_development_key_please_change";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "StoreManagement";
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role ?? "staff")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = System.DateTime.UtcNow.AddHours(6),
                Issuer = jwtIssuer,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
        }
    }
}
