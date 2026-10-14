namespace MyStudents.Application.Common.Exceptions;

public class ConflictException(string errorCode) : Exception(errorCode)
{
    public string ErrorCode { get; } = errorCode;
}

