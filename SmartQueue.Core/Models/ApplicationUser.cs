using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Net.Sockets;
using System;

namespace SmartQueue.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        // Add this navigation property
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}