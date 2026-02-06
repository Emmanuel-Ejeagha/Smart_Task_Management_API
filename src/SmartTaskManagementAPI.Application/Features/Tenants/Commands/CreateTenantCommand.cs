using MediatR;
using SmartTaskManagementAPI.Application.Features.Tenants.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Tenants.Commands
{
    public class CreateTenantCommand : IRequest<TenantDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    public class UpdateTenantCommand : IRequest<TenantDto>
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class DeactivateTenantCommand : IRequest<TenantDto>
    {
        public Guid TenantId { get; set; }
    }
}
