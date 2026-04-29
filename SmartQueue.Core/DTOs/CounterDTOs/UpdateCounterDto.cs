using System.ComponentModel.DataAnnotations;

namespace SmartQueue.Core.DTOs.CounterDTOs
{
    public class UpdateCounterDto
    {
        [Required(ErrorMessage = "Counter name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Counter name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        // Proslijedi null za odjavu/unassign
        public string? AssignedUserId { get; set; }
    }
}