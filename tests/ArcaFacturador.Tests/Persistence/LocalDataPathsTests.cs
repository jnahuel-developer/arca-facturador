using System.IO;
using ArcaFacturador.Persistence;

namespace ArcaFacturador.Tests.Persistence;

public class LocalDataPathsTests
{
    [Fact]
    public void GetInvoicePdfFilePath_UsesDeterministicFileName()
    {
        var path = LocalDataPaths.GetInvoicePdfFilePath(42);

        Assert.EndsWith(Path.Combine("facturas", "factura-local-00000042.pdf"), path);
    }

    [Fact]
    public void GetInvoicePdfFilePath_RejectsInvalidInvoiceId()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalDataPaths.GetInvoicePdfFilePath(0));
    }

    [Fact]
    public void WsaaTicketCacheFilePath_UsesApplicationDataDirectory()
    {
        var path = LocalDataPaths.WsaaTicketCacheFilePath;

        Assert.EndsWith(Path.Combine("ArcaFacturador", "wsaa-ticket-cache.json"), path);
    }
}
