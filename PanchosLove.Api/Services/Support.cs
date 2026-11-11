namespace PanchosLove.Api.Services;

/// <summary>Error de negocio controlado, se devuelve como JSON con el código indicado.</summary>
public class AppException(int statusCode, string message, object? details = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public object? Details { get; } = details;
}

/// <summary>Argentina no usa horario de verano: UTC-3 fijo (evita problemas de IDs de zona horaria Windows/Linux).</summary>
public static class Clock
{
    public const int UtcOffsetHours = -3;
    public static DateTime Now => DateTime.UtcNow.AddHours(UtcOffsetHours);
    public static DateTime ToLocal(DateTime utc) => utc.AddHours(UtcOffsetHours);
    public static DateTime ToUtc(DateOnly localDay) => localDay.ToDateTime(TimeOnly.MinValue).AddHours(-UtcOffsetHours);
}
