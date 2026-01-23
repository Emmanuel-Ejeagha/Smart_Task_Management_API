using System;
using Microsoft.AspNetCore.Identity;

namespace SmartTaskManagementAPI.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
