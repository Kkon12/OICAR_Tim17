using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.CounterDTOs
{
    public class AssignUserDto
    {
        public string UserId { get; set; } = string.Empty;
    }
}

/*Why separate AssignUserDto: Assigning a Djelatnik to a counter is a distinct business action
 * from updating counter name/details — keeps API intentions clear and auditable.*/