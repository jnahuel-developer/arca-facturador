namespace ArcaFacturador.Arca;

public sealed record ArcaConnectionTestResult(
    ArcaOperationPreview Preview,
    long LastAuthorizedReceiptNumber);
