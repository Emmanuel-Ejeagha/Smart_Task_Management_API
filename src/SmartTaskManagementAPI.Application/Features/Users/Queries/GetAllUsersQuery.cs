using MediatR;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Application.Features.Users.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Users.Queries
{
    public class GetAllUsersQuery : IRequest<PaginatedResult<UserDto>>
    {
        public Guid TenantId { get; set; }
        public PaginationQuery Pagination { get; set; } = new PaginationQuery();
    }

    public class GetUserByIdQuery : IRequest<UserDto>
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
    }
}
