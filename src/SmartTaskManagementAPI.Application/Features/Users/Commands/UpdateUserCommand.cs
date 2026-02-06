using MediatR;
using SmartTaskManagementAPI.Application.Features.Users.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Users.Commands
{
    public class UpdateUserCommand : IRequest<UserDto>
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class DeactivateUserCommand : IRequest<UserDto>
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
    }

    public class ActivateUserCommand : IRequest<UserDto>
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
    }
}
