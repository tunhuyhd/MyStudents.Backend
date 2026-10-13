namespace MyStudents.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException() : base("Administrator access is required.") { }
}
