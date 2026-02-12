using AutoMapper;
using SmartTaskManagement.Application.Features.Tenants.Dtos;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Mappings;

public class TenantProfile : Profile
{
    public TenantProfile()
    {
        CreateMap<Tenant, TenantDto>()
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted))
            .ForMember(dest => dest.WorkItemCount, opt => opt.Ignore());
    }
}