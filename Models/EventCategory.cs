using System.ComponentModel.DataAnnotations.Schema;

namespace EventTicketing.Models
{
    [Table("event_categories")]
    public class EventCategory
    {
        [Column("id_event")]
        public int IdEvent { get; set; }

        [Column("id_category")]
        public int IdCategory { get; set; }
    }
}