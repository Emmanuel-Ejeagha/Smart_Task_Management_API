using System;

namespace SmartTaskManagementAPI.Application.Common.Interfaces;

public interface IDateTimeService
{
    DateTime Now { get; }
    DateTime UtcNown  { get; }
}
