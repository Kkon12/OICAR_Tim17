using System.ComponentModel.DataAnnotations;

namespace SmartQueue.Core.DTOs.CounterDTOs
{
    public class CreateCounterDto
    {
        [Required(ErrorMessage = "Counter name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Counter name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "A valid QueueId is required.")]
        public int QueueId { get; set; }

        // Optional — counter can be created without an assigned Djelatnik
        public string? AssignedUserId { get; set; }
    }
}