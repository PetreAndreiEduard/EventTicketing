using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventTicketing.Models
{
    [Table("events")]
    public class Event
    {
        [Key]
        [Column("id_event")]
        public int IdEvent { get; set; }

        [Column("id_organizer")]
        public int IdOrganizer { get; set; }

        [Required]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column("location")]
        public string Location { get; set; } = string.Empty;

        [Column("event_date")]
        public DateTime EventDate { get; set; }

        [Column("ticket_price")]
        public decimal TicketPrice { get; set; }

        [Column("available_seats")]
        public int AvailableSeats { get; set; }

        [Required]
        [Column("poster_image_path")]
        public string PosterImagePath { get; set; } = string.Empty;
    }
}