
using Microsoft.AspNetCore.Http;

namespace MediMateService.Shared;
public class ServiceException : Exception
{
    public int StatusCode { get; }
    public string? BusinessCode { get; }
    public string? Field { get; }

    public ServiceException(int statusCode, string? businessCode, string message, string? field = null)
        : base(message)
    {
        StatusCode = statusCode;
        BusinessCode = businessCode;
        Field = field;
    }
}

public class NotFoundException : ServiceException
{
    public NotFoundException(string message, string? businessCode = null, string? field = null)
        : base(StatusCodes.Status404NotFound, businessCode, message, field)
    {
    }
}

public class BadRequestException : ServiceException
{
    public BadRequestException(string message, string? businessCode = null, string? field = null)
        : base(StatusCodes.Status400BadRequest, businessCode, message, field)
    {
    }
}

public class ConflictException : ServiceException
{
    public ConflictException(string message, string? businessCode = null, string? field = null)
        : base(StatusCodes.Status409Conflict, businessCode, message, field)
    {
    }
}

public class ForbiddenException : ServiceException
{
    public ForbiddenException(string message, string? businessCode = null, string? field = null)
        : base(StatusCodes.Status403Forbidden, businessCode, message, field)
    {
    }
}