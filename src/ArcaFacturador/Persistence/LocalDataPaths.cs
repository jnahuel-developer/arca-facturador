using System.IO;

namespace ArcaFacturador.Persistence;

public static class LocalDataPaths
{
    private static string ApplicationDataDirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcaFacturador");

    public static string DatabaseFilePath => Path.Combine(
        ApplicationDataDirectoryPath,
        "arca-facturador.db");

    public static string InvoicePdfDirectoryPath => Path.Combine(
        ApplicationDataDirectoryPath,
        "facturas");

    public static string WsaaTicketCacheFilePath => Path.Combine(
        ApplicationDataDirectoryPath,
        "wsaa-ticket-cache.json");

    public static string GetInvoicePdfFilePath(long invoiceId)
    {
        if (invoiceId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(invoiceId));
        }

        return Path.Combine(InvoicePdfDirectoryPath, $"factura-local-{invoiceId:00000000}.pdf");
    }
}
