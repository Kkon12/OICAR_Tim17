using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.AuthDTOs
{

    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}

/* Why IsActive: Admins need to be able to see and deactivate staff accounts 
 * — a Djelatnik who leaves the institution should not be deleted (historical ticket data would be lost) 
 * but should be deactivated. This flag enables that without data loss.
 
Why CreatedAt on user response: Useful for Admin — shows when each staff member was added to the system. 
 * Also useful for audit purposes.*/
