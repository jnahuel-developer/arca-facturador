namespace ArcaFacturador.Domain;

public static class InvoiceIssueDateRules
{
    public const int MaximumPreviousDays = 10;

    public static DateOnly MinimumAllowed(DateOnly today, DateOnly? lastAuthorizedIssueDate = null)
    {
        var minimumAllowedByDays = today.AddDays(-MaximumPreviousDays);
        return lastAuthorizedIssueDate is { } lastIssueDate && lastIssueDate > minimumAllowedByDays
            ? lastIssueDate
            : minimumAllowedByDays;
    }

    public static DateOnly MaximumAllowed(DateOnly today) => today;

    public static bool TryValidate(
        DateOnly issueDate,
        DateOnly today,
        out string errorMessage,
        DateOnly? lastAuthorizedIssueDate = null)
    {
        if (issueDate > MaximumAllowed(today))
        {
            errorMessage = "La fecha de factura no puede ser futura.";
            return false;
        }

        if (lastAuthorizedIssueDate is { } lastIssueDate && issueDate < lastIssueDate)
        {
            errorMessage = $"La fecha de factura no puede ser anterior al último comprobante autorizado en ARCA ({lastIssueDate:dd/MM/yyyy}).";
            return false;
        }

        if (issueDate < MinimumAllowed(today))
        {
            errorMessage = "La fecha de factura no puede tener más de 10 días corridos hacia atrás.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
