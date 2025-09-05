namespace DataPlane.Sdk.Core.Domain.Model;

/// <summary>
///     Represents an entity with a state of type <typeparamref name="TState" /> and tracks state transitions.
/// </summary>
/// <typeparam name="TState">An enumeration type representing the possible states of the entity.</typeparam>
/// <param name="id">The unique identifier for the entity.</param>
public class StatefulEntity<TState>(string id) : Identifiable(id) where TState : Enum
{
    public required TState State { get; set; }
    public int StateCount { get; private set; }
    public DateTime StateTimestamp { get; private set; } = DateTime.UtcNow;
    public string? ErrorDetail { get; } = null;
    public bool IsPending { get; } = false;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    protected void Transition(TState targetState)
    {
        StateCount = State.Equals(targetState) ? StateCount + 1 : 1;
        State = targetState;
        StateTimestamp = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
