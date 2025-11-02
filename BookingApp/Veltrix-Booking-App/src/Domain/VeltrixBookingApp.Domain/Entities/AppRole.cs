using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VeltrixBookingApp.Domain.Entities
{
    public class AppRole : IdentityRole<Guid>
    {
        public string? FullName { get; set; }
    }
}
