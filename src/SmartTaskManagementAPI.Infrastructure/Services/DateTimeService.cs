using System;
using SmartTaskManagementAPI.Application.Common.Interfaces;

namespace SmartTaskManagementAPI.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}
