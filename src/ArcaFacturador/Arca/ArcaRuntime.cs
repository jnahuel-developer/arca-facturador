using ArcaFacturador.Arca.Wsaa;
using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Domain;

namespace ArcaFacturador.Arca;

public sealed class ArcaRuntime(
    ArcaOperationPreview preview,
    IWsaaTicketProvider ticketProvider,
    IWsfev1Client wsfeClient,
    IDisposable? disposable = null) : IDisposable
{
    public ArcaOperationPreview Preview { get; } = preview;

    public FiscalConfiguration FiscalConfiguration => Preview.FiscalConfiguration;

    public IWsaaTicketProvider TicketProvider { get; } = ticketProvider;

    public IWsfev1Client WsfeClient { get; } = wsfeClient;

    public void Dispose()
    {
        disposable?.Dispose();
    }
}
