using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventTicketing.Models
{
    [Table("tickets")]
    public class Ticket
    {
        [Key]
        [Column("id_ticket")]
        public int IdTicket { get; set; }

        [Column("id_user")]
        public int IdUser { get; set; }

        [Column("id_event")]
        public int IdEvent { get; set; }

        [Required]
        [Column("ticket_code")]
        public string TicketCode { get; set; } = string.Empty;

        [Required]
        [Column("protected_qr_path")]
        public string ProtectedQrPath { get; set; } = string.Empty;

        [Required]
        [Column("status")]
        public string Status { get; set; } = "Valid";

        [Column("purchase_date")]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
    }
}