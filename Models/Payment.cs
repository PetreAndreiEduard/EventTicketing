using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventTicketing.Models
{
    [Table("payments")]
    public class Payment : BaseEntity
    {
        [Key]
        [Column("id_payment")]
        public int IdPayment { get; set; }

        [Column("id_ticket")]
        public int IdTicket { get; set; }

        [Required]
        [Column("stripe_transaction_id")]
        public string StripeTransactionId { get; set; } = string.Empty;

        [Column("amount")]
        public decimal Amount { get; set; }

        [Required]
        [Column("payment_status")]
        public string PaymentStatus { get; set; } = "Succeeded";

        [Required]
        [Column("card_last4")]
        public string CardLast4 { get; set; } = string.Empty;

        [Column("created_at")]
        public new DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}