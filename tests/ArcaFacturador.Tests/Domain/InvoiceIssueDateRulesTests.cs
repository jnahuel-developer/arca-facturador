using ArcaFacturador.Domain;

namespace ArcaFacturador.Tests.Domain;

public class InvoiceIssueDateRulesTests
{
    [Theory]
    [InlineData(2026, 9, 15)]
    [InlineData(2026, 9, 25)]
    public void TryValidate_AcceptsTodayAndPreviousTenDays(int year, int month, int day)
    {
        var result = InvoiceIssueDateRules.TryValidate(
            new DateOnly(year, month, day),
            new DateOnly(2026, 9, 25),
            out var errorMessage);

        Assert.True(result);
        Assert.Equal(string.Empty, errorMessage);
    }

    [Fact]
    public void TryValidate_RejectsFutureIssueDate()
    {
        var result = InvoiceIssueDateRules.TryValidate(
            new DateOnly(2026, 9, 26),
            new DateOnly(2026, 9, 25),
            out var errorMessage);

        Assert.False(result);
        Assert.Equal("La fecha de factura no puede ser futura.", errorMessage);
    }

    [Fact]
    public void TryValidate_RejectsIssueDateOlderThanTenDays()
    {
        var result = InvoiceIssueDateRules.TryValidate(
            new DateOnly(2026, 9, 14),
            new DateOnly(2026, 9, 25),
            out var errorMessage);

        Assert.False(result);
        Assert.Equal("La fecha de factura no puede tener más de 10 días corridos hacia atrás.", errorMessage);
    }
}
