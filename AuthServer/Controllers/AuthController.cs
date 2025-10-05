using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthServer.Data;
using AuthServer.Data.Models;
using BCrypt.Net;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validar entrada
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username e password são obrigatórios" });

            if (request.Username.Length < 3 || request.Username.Length > 50)
                return BadRequest(new { message = "Username deve ter entre 3 e 50 caracteres" });

            if (request.Password.Length < 6)
                return BadRequest(new { message = "Password deve ter no mínimo 6 caracteres" });

            // Verificar se usuário já existe
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return Conflict(new { message = "Username já existe" });

            // Criar novo usuário com senha hashada
            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuário criado com sucesso", userId = user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Validar entrada
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username e password são obrigatórios" });

            // Buscar usuário
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
                return Unauthorized(new { message = "Username ou password inválidos" });

            // Verificar senha
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Username ou password inválidos" });

            // Gerar JWT token
            var token = GenerateJwtToken(user);

            return Ok(new { token, userId = user.Id, username = user.Username });
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var secretKey = _configuration["JwtSettings:SecretKey"];
                
                if (string.IsNullOrEmpty(secretKey))
                    return StatusCode(500, new { message = "Configuração de JWT inválida" });

                var key = Encoding.ASCII.GetBytes(secretKey);

                tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "userId");
                
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized(new { message = "Token inválido" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Unauthorized(new { message = "Usuário não encontrado" });

                return Ok(new { valid = true, userId = user.Id, username = user.Username });
            }
            catch
            {
                return Unauthorized(new { valid = false, message = "Token inválido" });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JwtSettings:SecretKey"];
            var expirationHours = _configuration["JwtSettings:ExpirationHours"];
            
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(expirationHours))
                throw new InvalidOperationException("Configuração de JWT não encontrada");

            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("userId", user.Id.ToString()),
                    new Claim("username", user.Username),
                    new Claim(ClaimTypes.Name, user.Username)
                }),
                Expires = DateTime.UtcNow.AddHours(int.Parse(expirationHours)),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ValidateTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}