using System.ComponentModel.DataAnnotations;

namespace AuthServer.Data.Models
{
    public class Character
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(30)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Race { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Class { get; set; } = string.Empty;

        public float PositionX { get; set; } = 0f;
        public float PositionY { get; set; } = 0f;
        public float PositionZ { get; set; } = 0f;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

        // Relacionamento
        public User User { get; set; } = null!;
    }
}