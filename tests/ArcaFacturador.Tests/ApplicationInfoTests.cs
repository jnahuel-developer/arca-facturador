namespace ArcaFacturador.Tests;

public class ApplicationInfoTests
{
    [Fact]
    public void ProductName_IsTheExpectedName()
    {
        Assert.Equal("ARCA Facturador", ApplicationInfo.ProductName);
    }
}
