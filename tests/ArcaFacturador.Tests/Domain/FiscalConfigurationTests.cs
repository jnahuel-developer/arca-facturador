using ArcaFacturador.Domain;

namespace ArcaFacturador.Tests.Domain;

public class FiscalConfigurationTests
{
    [Fact]
    public void Constructor_NormalizesAValidCuit()
    {
        var configuration = new FiscalConfiguration("20-12345678-6", 1);

        Assert.Equal("20123456786", configuration.Cuit);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99_999)]
    public void Constructor_AcceptsPointOfSaleLimits(int pointOfSale)
    {
        var configuration = new FiscalConfiguration("20123456786", pointOfSale);

        Assert.Equal(pointOfSale, configuration.PointOfSale);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100_000)]
    public void Constructor_RejectsPointOfSaleOutsideAllowedRange(int pointOfSale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FiscalConfiguration("20123456786", pointOfSale));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2012345678")]
    [InlineData("20123456787")]
    [InlineData("20-A2345678-6")]
    public void Constructor_RejectsInvalidCuit(string cuit)
    {
        Assert.Throws<ArgumentException>(() => new FiscalConfiguration(cuit, 1));
    }
}
