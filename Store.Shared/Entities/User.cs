using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Store.Shared.Entities
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("username")]
        [Required]
        public string Username { get; set; } = string.Empty;

        [Column("password")]
        [Required] 
        public string Password { get; set; } = string.Empty;

        [Column("full_name")]
        public string? FullName { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("role")]
        public string Role { get; set; } = "customer"; 

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? ResetToken { get; set; }
        public DateTime? ResetExpiry { get; set; }
    }
}
