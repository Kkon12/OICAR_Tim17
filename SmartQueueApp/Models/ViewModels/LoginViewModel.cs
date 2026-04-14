using System.ComponentModel.DataAnnotations;

namespace SmartQueueApp.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
        public string? ReturnUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

/*Why ViewModel instead of LoginDto: ViewModels are for the View layer
 * — they include UI-specific fields like ErrorMessage and ReturnUrl that the API DTO doesn't need.
 * Separation of concerns.*/