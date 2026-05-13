using MediatR;

namespace MyStudents.Domain.Common;

public abstract class BaseEvent : INotification
{
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
}
