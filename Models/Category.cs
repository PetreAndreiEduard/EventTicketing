using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventTicketing.Models
{
    [Table("categories")]
    public class Category
    {
        [Key]
        [Column("id_category")]
        public int IdCategory { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }
    }
}