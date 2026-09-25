namespace ArcaFacturador.Domain;

public static class ServiceDateRules
{
    public static ServiceDates ForIssueDate(DateOnly issueDate)
    {
        var serviceFrom = new DateOnly(issueDate.Year, issueDate.Month, 1);
        var serviceTo = serviceFrom.AddMonths(1).AddDays(-1);

        return new ServiceDates(serviceFrom, serviceTo, issueDate);
    }
}
