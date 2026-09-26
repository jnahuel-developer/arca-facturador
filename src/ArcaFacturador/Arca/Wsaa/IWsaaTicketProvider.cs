namespace ArcaFacturador.Arca.Wsaa;

public interface IWsaaTicketProvider
{
    Task<WsaaLoginTicket> GetTicketAsync(CancellationToken cancellationToken = default);
}
