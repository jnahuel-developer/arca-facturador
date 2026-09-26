using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Arca.Wsfev1;

public sealed record WsfeAuthorizationOutcome(InvoiceRecord Invoice, WsfeCaeResponse Response);
