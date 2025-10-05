using System.ComponentModel.DataAnnotations;

namespace AuthServer.Data.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relacionamento
        public ICollection<Character> Characters { get; set; } = new List<Character>();
    }
}