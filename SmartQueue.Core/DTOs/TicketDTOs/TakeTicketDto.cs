using System.ComponentModel.DataAnnotations;

namespace SmartQueue.Core.DTOs.TicketDTOs
{
    public class TakeTicketDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid QueueId is required.")]
        public int QueueId { get; set; }

        // null = anonymous kiosk user — intentional, no [Required]
        public string? UserId { get; set; }
    }
}
/*Why UserId is nullable in TakeTicketDto: Anonymous kiosk users don't have an account — their ticket simply has no UserId.
 * Registered mobile app users pass their UserId to link the ticket to their account.*/
