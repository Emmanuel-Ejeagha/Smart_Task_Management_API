namespace SmartTaskManagement.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when an entity is not found
/// </summary>
public class NotFoundException : ApplicationException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public NotFoundException(string entityName, object entityId)
        : base($"Entity {entityName} with ID {entityId} was not found")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    public NotFoundException(string message)
        : base(message)
    {
        EntityName = "Unknown";
        EntityId = "Unknown";
    }

    public override string ErrorCode => "NOT_FOUND";
}