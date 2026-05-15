using System;

namespace MyStudents.Application.Common.Exceptions;

public class BusinessException : Exception
{
    public string ErrorCode { get; }
    public IDictionary<string, string>? Parameters { get; }

    public BusinessException(string errorCode, IDictionary<string, string>? parameters = null) 
        : base(errorCode)
    {
        ErrorCode = errorCode;
        Parameters = parameters;
    }
}
