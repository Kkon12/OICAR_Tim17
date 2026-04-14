namespace SmartQueue.Core.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        public string UserId { get; set; } = string.Empty;

        // Navigation
        public ApplicationUser User { get; set; } = null!;

        // Helper
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}