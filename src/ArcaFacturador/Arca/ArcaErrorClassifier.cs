namespace ArcaFacturador.Arca;

public static class ArcaErrorClassifier
{
    public static ArcaServiceErrorKind Classify(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return ArcaServiceErrorKind.Unknown;
        }

        if (message.Contains("CEE ya posee un TA valido", StringComparison.OrdinalIgnoreCase))
        {
            return ArcaServiceErrorKind.ExistingValidTicket;
        }

        if (message.Contains("ORA-01034", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ORACLE not available", StringComparison.OrdinalIgnoreCase)
            || message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return ArcaServiceErrorKind.RemoteUnavailable;
        }

        if (message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || message.Contains("No such host", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return ArcaServiceErrorKind.Recoverable;
        }

        return ArcaServiceErrorKind.Unknown;
    }
}
