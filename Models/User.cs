using EventTicketing.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventTicketing.Models
{
    [Table("users")]
    public class User : BaseEntity
    {
        [Key]
        [Column("id_user")]
        public int IdUser { get; set; }

        [Required]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Column("role")]
        public string Role { get; set; } = "Customer"; // "Customer" sau "Organizer"

        [Column("created_at")]
        public new DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}