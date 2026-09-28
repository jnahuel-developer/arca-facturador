using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Tests.Arca.Wsfev1;

public class WsfeInvoiceRequestFactoryTests
{
    [Fact]
    public void CreateFacturaCServices_BuildsExpectedRequest()
    {
        var factory = new WsfeInvoiceRequestFactory();

        var request = factory.CreateFacturaCServices(CreateInvoice(), pointOfSale: 3, nextReceiptNumber: 42);

        Assert.Equal(3, request.PointOfSale);
        Assert.Equal(Wsfev1Constants.FacturaC, request.ReceiptType);
        Assert.Equal(Wsfev1Constants.ConceptoServicios, request.Concept);
        Assert.Equal(Wsfev1Constants.DocumentoConsumidorFinal, request.DocumentType);
        Assert.Equal(Wsfev1Constants.DocumentoNumeroConsumidorFinal, request.DocumentNumber);
        Assert.Equal(42, request.ReceiptNumber);
        Assert.Equal(new DateOnly(2026, 9, 23), request.ReceiptDate);
        Assert.Equal(new DateOnly(2026, 9, 1), request.ServiceFrom);
        Assert.Equal(new DateOnly(2026, 9, 30), request.ServiceTo);
        Assert.Equal(new DateOnly(2026, 9, 25), request.PaymentDueDate);
        Assert.Equal(1250m, request.TotalAmount);
        Assert.Equal(1250m, request.NetAmount);
        Assert.Equal(0m, request.VatAmount);
        Assert.Equal(Wsfev1Constants.MonedaPesos, request.CurrencyId);
        Assert.Equal(Wsfev1Constants.CondicionIvaConsumidorFinal, request.ReceiverVatConditionId);
    }

    [Fact]
    public void CreateFacturaCServices_RejectsAlreadyAuthorizedInvoice()
    {
        var factory = new WsfeInvoiceRequestFactory();

        Assert.Throws<InvalidOperationException>(
            () => factory.CreateFacturaCServices(CreateInvoice() with { Status = InvoiceStatus.Authorized }, 1, 1));
    }

    private static InvoiceRecord CreateInvoice() => new(
        Id: 7,
        ReceiptNumber: null,
        IssueDate: new DateOnly(2026, 9, 23),
        ServiceFrom: new DateOnly(2026, 9, 1),
        ServiceTo: new DateOnly(2026, 9, 30),
        PaymentDueDate: new DateOnly(2026, 9, 25),
        AmountCents: 125_000,
        Status: InvoiceStatus.Pending,
        Cae: null,
        CaeExpirationDate: null,
        PdfPath: null);
}
