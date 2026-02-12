using AutoMapper;
using SmartTaskManagement.Application.Features.Reminders.Dtos;
using SmartTaskManagement.Application.Features.WorkItems.Dtos;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Mappings;

public class WorkItemProfile : Profile
{
    public WorkItemProfile()
    {
        CreateMap<WorkItem, WorkItemDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
            .ForMember(dest => dest.IsOverdue, opt => opt.Ignore())
            .ForMember(dest => dest.ProgressPercentage, opt => opt.Ignore());

        CreateMap<Reminder, ReminderDto>()
            .ForMember(dest => dest.WorkItemTitle, opt => opt.MapFrom(src => src.WorkItem!.Title));
    }
}
