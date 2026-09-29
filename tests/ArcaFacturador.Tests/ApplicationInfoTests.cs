namespace ArcaFacturador.Tests;

public class ApplicationInfoTests
{
    [Fact]
    public void ProductName_IsTheExpectedName()
    {
        Assert.Equal("ARCA Facturador", ApplicationInfo.ProductName);
    }

    [Fact]
    public void Version_IsTheMvpStableVersion()
    {
        Assert.Equal("1.0.0", ApplicationInfo.Version);
    }
}
