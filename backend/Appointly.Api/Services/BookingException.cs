namespace Appointly.Api.Services;

public sealed class BookingException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
