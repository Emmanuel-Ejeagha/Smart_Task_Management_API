using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;
using AutoMapper;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Domain.Enums;
using SmartTaskManagementAPI.Application.Features.Users.DTOs;
using SmartTaskManagementAPI.Domain.Entities;
using SmartTaskManagementAPI.Application.Features.Tenants.DTOs;

namespace SmartTaskManagementAPI.Application.Common.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
         // Task to TaskDto
        CreateMap<TaskEntity, TaskDto>()
            .ForMember(dest => dest.PriorityDisplay, 
                opt => opt.MapFrom(src => src.Priority.GetDisplayName()))
            .ForMember(dest => dest.StatusDisplay, 
                opt => opt.MapFrom(src => src.Status.GetDisplayName()))
            .ForMember(dest => dest.CreatedBy, 
                opt => opt.MapFrom(src => src.CreatedBy.HasValue ? src.CreatedBy.Value.ToString() : string.Empty))
            .ForMember(dest => dest.UpdatedBy, 
                opt => opt.MapFrom(src => src.UpdatedBy.HasValue ? src.UpdatedBy.Value.ToString() : string.Empty));

        // TaskEntity to TaskListDto
        CreateMap<TaskEntity, TaskListDto>()
            .ForMember(dest => dest.PriorityDisplay,
                opt => opt.MapFrom(src => src.Priority.GetDisplayName()))
            .ForMember(dest => dest.StatusDisplay,
                opt => opt.MapFrom(src => src.Status.GetDisplayName()));
            // .ForMember(dest => dest.IsOverdue, 
                // opt => opt.MapFrom(src => src.IsOverdue()));

        // Tenant to TenantDto
        CreateMap<Tenant, TenantDto>()
            .ForMember(dest => dest.CreatedBy, 
                opt => opt.MapFrom(src => src.CreatedBy.HasValue ? src.CreatedBy.Value.ToString() : string.Empty))
            .ForMember(dest => dest.UpdatedBy, 
                opt => opt.MapFrom(src => src.UpdatedBy.HasValue ? src.UpdatedBy.Value.ToString() : string.Empty))
            .ForMember(dest => dest.UserCount, 
                opt => opt.MapFrom(src => src.Users.Count(u => !u.IsDeleted)))
            .ForMember(dest => dest.TaskCount, 
                opt => opt.MapFrom(src => src.Tasks.Count(t => !t.IsDeleted)));

        // User to UserDto
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.TenantName, 
                opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty))
            .ForMember(dest => dest.CreatedBy, 
                opt => opt.MapFrom(src => src.CreatedBy.HasValue ? src.CreatedBy.Value.ToString() : string.Empty))
            .ForMember(dest => dest.UpdatedBy, 
                opt => opt.MapFrom(src => src.UpdatedBy.HasValue ? src.UpdatedBy.Value.ToString() : string.Empty));
    }
}
