namespace ArcaFacturador.Arca.Wsaa;

public interface IWsaaTicketCache
{
    WsaaLoginTicket? Load(WsaaOptions options, DateTimeOffset now);

    void Save(WsaaOptions options, WsaaLoginTicket ticket);

    void Clear(WsaaOptions options);
}
