using MyStudents.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MyStudents.Domain.Entities;

[Table("subjects")]
public class Subject : AuditableEntity, IAggregateRoot
{
	[Column("name")]
	public string Name { get; set; } = string.Empty;

	[Column("description")]
	public string Description { get; set; } = string.Empty;
}
