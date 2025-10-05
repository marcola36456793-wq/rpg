using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AuthServer.Data;
using AuthServer.Data.Models;

namespace AuthServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CharactersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CharactersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCharacters()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token inválido" });

            var characters = await _context.Characters
                .Where(c => c.UserId == userId.Value)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Race,
                    c.Class,
                    c.CreatedAt,
                    c.LastLoginAt
                })
                .ToListAsync();

            return Ok(characters);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCharacter(int id)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token inválido" });

            var character = await _context.Characters
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId.Value);

            if (character == null)
                return NotFound(new { message = "Personagem não encontrado" });

            return Ok(new
            {
                character.Id,
                character.Name,
                character.Race,
                character.Class,
                character.PositionX,
                character.PositionY,
                character.PositionZ,
                character.CreatedAt,
                character.LastLoginAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCharacter([FromBody] CreateCharacterRequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token inválido" });

            // Validar entrada
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3 || request.Name.Length > 30)
                return BadRequest(new { message = "Nome deve ter entre 3 e 30 caracteres" });

            var validRaces = new[] { "Humano", "Elfo", "Orc" };
            if (!validRaces.Contains(request.Race))
                return BadRequest(new { message = "Raça inválida. Escolha: Humano, Elfo ou Orc" });

            // CORREÇÃO: Aceitar tanto Class quanto CharacterClass
            string characterClass = !string.IsNullOrEmpty(request.Class) 
                ? request.Class 
                : request.CharacterClass;

            if (string.IsNullOrEmpty(characterClass))
                return BadRequest(new { message = "Classe é obrigatória" });

            var validClasses = new[] { "Guerreiro", "Mago", "Arqueiro" };
            if (!validClasses.Contains(characterClass))
                return BadRequest(new { message = "Classe inválida. Escolha: Guerreiro, Mago ou Arqueiro" });

            // Verificar se já existe personagem com esse nome (global)
            if (await _context.Characters.AnyAsync(c => c.Name == request.Name))
                return Conflict(new { message = "Nome de personagem já existe" });

            // Limitar número de personagens por usuário (opcional, MVP: 3)
            var characterCount = await _context.Characters.CountAsync(c => c.UserId == userId.Value);
            if (characterCount >= 3)
                return BadRequest(new { message = "Limite de 3 personagens atingido" });

            // Criar personagem
            var character = new Character
            {
                UserId = userId.Value,
                Name = request.Name,
                Race = request.Race,
                Class = characterClass,
                PositionX = 0f,
                PositionY = 0f,
                PositionZ = 0f,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Characters.Add(character);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Personagem criado com sucesso",
                characterId = character.Id,
                character.Name,
                character.Race,
                character.Class
            });
        }

        [HttpPut("{id}/position")]
        public async Task<IActionResult> UpdatePosition(int id, [FromBody] UpdatePositionRequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token inválido" });

            var character = await _context.Characters
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId.Value);

            if (character == null)
                return NotFound(new { message = "Personagem não encontrado" });

            character.PositionX = request.X;
            character.PositionY = request.Y;
            character.PositionZ = request.Z;
            character.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Posição atualizada" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCharacter(int id)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token inválido" });

            var character = await _context.Characters
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId.Value);

            if (character == null)
                return NotFound(new { message = "Personagem não encontrado" });

            _context.Characters.Remove(character);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Personagem deletado com sucesso" });
        }

        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");
            if (userIdClaim == null)
                return null;

            if (int.TryParse(userIdClaim.Value, out int userId))
                return userId;

            return null;
        }
    }

    public class CreateCharacterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Race { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string CharacterClass { get; set; } = string.Empty; // Aceitar ambos
    }

    public class UpdatePositionRequest
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}
