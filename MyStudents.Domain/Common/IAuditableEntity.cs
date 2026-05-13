using System.ComponentModel.DataAnnotations.Schema;

namespace MyStudents.Domain.Common;

public interface IAuditableEntity
{
    Guid CreatedBy { get; set; }
    DateTime CreatedOn { get; }
    Guid LastModifiedBy { get; set; }
    DateTime? LastModifiedOn { get; set; }
}

public interface ISoftDelete
{
    DateTime? DeletedOn { get; set; }
    Guid? DeletedBy { get; set; }
}

public abstract class AuditableEntity : Entity, IAuditableEntity, ISoftDelete
{
    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_on")]
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [Column("last_modified_by")]
    public Guid LastModifiedBy { get; set; }

    [Column("last_modified_on")]
    public DateTime? LastModifiedOn { get; set; }

    [Column("deleted_on")]
    public DateTime? DeletedOn { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }
}
