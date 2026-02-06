using MediatR;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Application.Features.Tenants.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Tenants.Queries
{
    public class GetAllTenantsQuery : IRequest<PaginatedResult<TenantDto>>
    {
        public PaginationQuery Pagination { get; set; } = new PaginationQuery();
    }

    public class GetTenantByIdQuery : IRequest<TenantDto>
    {
        public Guid TenantId { get; set; }
    }
}