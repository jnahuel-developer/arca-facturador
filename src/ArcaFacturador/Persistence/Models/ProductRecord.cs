namespace ArcaFacturador.Persistence.Models;

public sealed record ProductRecord(
    long Id,
    string Code,
    string Description,
    string Unit,
    long UnitPriceCents);
