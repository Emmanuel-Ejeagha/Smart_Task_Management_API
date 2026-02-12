using AutoMapper;
using SmartTaskManagement.Application.Features.Reminders.Dtos;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Mappings;

public class ReminderProfile : Profile
{
    public ReminderProfile()
    {
        CreateMap<Reminder, ReminderDto>()
            .ForMember(dest => dest.WorkItemTitle, opt => opt.MapFrom(src => src.WorkItem != null ? src.WorkItem.Title : string.Empty))
            .ForMember(dest => dest.IsPending, opt => opt.MapFrom(src => src.IsPending()))
            .ForMember(dest => dest.IsDue, opt => opt.MapFrom(src => src.IsDue()));
    }
}