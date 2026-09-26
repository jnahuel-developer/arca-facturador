namespace ArcaFacturador.Arca.Wsfev1;

public interface IWsfev1Client
{
    Task<WsfeLastAuthorizedResult> GetLastAuthorizedAsync(
        WsfeAuth auth,
        int pointOfSale,
        int receiptType,
        CancellationToken cancellationToken = default);

    Task<WsfeVoucher?> GetVoucherAsync(
        WsfeAuth auth,
        int pointOfSale,
        int receiptType,
        long receiptNumber,
        CancellationToken cancellationToken = default);

    Task<WsfeCaeResponse> RequestCaeAsync(
        WsfeAuth auth,
        WsfeInvoiceRequest request,
        CancellationToken cancellationToken = default);
}
