namespace Domain.Common;

public class BaseEntity<T>
{
    public T Id { get; set; }

    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int? LastModifiedByUserId { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
}