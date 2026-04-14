using System.ComponentModel.DataAnnotations;

namespace SmartQueue.Core.DTOs.AuthDTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "First name must be between 2 and 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Last name must be between 2 and 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;
    }
}