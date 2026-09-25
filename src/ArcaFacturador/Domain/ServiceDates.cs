namespace ArcaFacturador.Domain;

public readonly record struct ServiceDates(
    DateOnly ServiceFrom,
    DateOnly ServiceTo,
    DateOnly PaymentDueDate);
