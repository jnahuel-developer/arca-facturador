using ArcaFacturador.Domain;

namespace ArcaFacturador.Tests.Domain;

public class ServiceDateRulesTests
{
    [Fact]
    public void ForIssueDate_UsesTheWholeCurrentMonth()
    {
        var dates = ServiceDateRules.ForIssueDate(new DateOnly(2026, 9, 25));

        Assert.Equal(new DateOnly(2026, 9, 1), dates.ServiceFrom);
        Assert.Equal(new DateOnly(2026, 9, 30), dates.ServiceTo);
        Assert.Equal(new DateOnly(2026, 9, 25), dates.PaymentDueDate);
    }

    [Theory]
    [InlineData(2024, 2, 29)]
    [InlineData(2025, 2, 28)]
    public void ForIssueDate_CalculatesFebruaryLastDay(
        int year,
        int month,
        int expectedLastDay)
    {
        var dates = ServiceDateRules.ForIssueDate(new DateOnly(year, month, 10));

        Assert.Equal(new DateOnly(year, month, expectedLastDay), dates.ServiceTo);
    }

    [Fact]
    public void ForIssueDate_HandlesTheDecemberYearBoundary()
    {
        var issueDate = new DateOnly(2026, 12, 15);

        var dates = ServiceDateRules.ForIssueDate(issueDate);

        Assert.Equal(new DateOnly(2026, 12, 1), dates.ServiceFrom);
        Assert.Equal(new DateOnly(2026, 12, 31), dates.ServiceTo);
        Assert.Equal(issueDate, dates.PaymentDueDate);
    }
}
