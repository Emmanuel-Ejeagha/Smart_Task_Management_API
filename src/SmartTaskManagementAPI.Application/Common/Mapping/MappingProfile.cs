using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;
using AutoMapper;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Domain.Enums;

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

        //  Task to TaskListDto
        CreateMap<TaskEntity, TaskListDto>()
            .ForMember(dest => dest.PriorityDisplay,
                opt => opt.MapFrom(src => src.Priority.GetDisplayName()))
            .ForMember(dest => dest.StatusDisplay,
                opt => opt.MapFrom(src => src.Status.GetDisplayName()))
            .ForMember(dest => dest.IsOverdue,
                opt => opt.MapFrom(src => src.IsOverDue()));
    }
    
}
