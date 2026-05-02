using System.ComponentModel.DataAnnotations;

namespace HelpDeskSystem.Domain.Entities
{
    public class WebhookDelivery
    {
        public int Id { get; set; }

        [Required]
        public int SubscriptionId { get; set; }

        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public string Payload { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Signature { get; set; }

        public int AttemptCount { get; set; }

        public DateTime? NextAttemptAtUtc { get; set; }

        public DateTime? DeliveredAtUtc { get; set; }

        [StringLength(1000)]
        public string? LastError { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty; // Pending, Processing, Delivered, Failed

        [Required]
        public DateTime CreatedAtUtc { get; set; }

        // Navigation properties
        public WebhookSubscription Subscription { get; set; } = null!;
    }
}
