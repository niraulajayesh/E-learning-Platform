namespace DataLayer.Entities;

/// <summary>
/// Abstract base class providing common audit fields for all entities.
/// </summary>
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Base class with a Guid primary key and audit fields.
/// </summary>
public abstract class BaseGuidEntity : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
