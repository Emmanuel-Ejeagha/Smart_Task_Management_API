using MediatR;
using Microsoft.Extensions.Logging;

namespace SmartTaskManagement.Infrastructure.Services;

/// <summary>
/// Dispatches domain events to their handlers
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<object> domainEvents, CancellationToken cancellationToken = default);
}

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IMediator mediator,
        ILogger<DomainEventDispatcher> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task DispatchEventsAsync(IEnumerable<object> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchEventAsync(domainEvent, cancellationToken);
        }
    }

    private async Task DispatchEventAsync(object domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Dispatching domain event {EventType}", domainEvent.GetType().Name);

            // Use reflection to call MediatR's Publish method with the correct type
            var mediatorType = _mediator.GetType();
            var method = mediatorType.GetMethod("Publish", new[] { domainEvent.GetType(), cancellationToken.GetType() });
            
            if (method != null)
            {
                await (Task)method.Invoke(_mediator, new[] { domainEvent, cancellationToken })!;
                _logger.LogInformation("Dispatched domain event {EventType} successfully", domainEvent.GetType().Name);
            }
            else
            {
                _logger.LogWarning("No Publish method found for event type {EventType}", domainEvent.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching domain event {EventType}", domainEvent.GetType().Name);
            // Don't throw - domain event dispatch failure shouldn't break the main operation
        }
    }
}