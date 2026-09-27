namespace ArcaFacturador.Domain;

public static class InvoiceIssueDateRules
{
    public const int MaximumPreviousDays = 10;

    public static DateOnly MinimumAllowed(DateOnly today) =>
        today.AddDays(-MaximumPreviousDays);

    public static DateOnly MaximumAllowed(DateOnly today) => today;

    public static bool TryValidate(DateOnly issueDate, DateOnly today, out string errorMessage)
    {
        if (issueDate > MaximumAllowed(today))
        {
            errorMessage = "La fecha de factura no puede ser futura.";
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
