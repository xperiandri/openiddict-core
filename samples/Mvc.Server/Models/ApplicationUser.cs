using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;

namespace Mvc.Server.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser<Guid> { }

    public class UserRole : IdentityRole<Guid> { }
}
