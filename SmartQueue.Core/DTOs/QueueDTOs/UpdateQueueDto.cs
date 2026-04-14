using System.ComponentModel.DataAnnotations;

namespace SmartQueue.Core.DTOs.QueueDTOs
{
    public class UpdateQueueDto
    {
        [Required(ErrorMessage = "Queue name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Queue name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [Range(1, 120,
            ErrorMessage = "Default service time must be between 1 and 120 minutes.")]
        public int DefaultServiceMinutes { get; set; }
    }
}