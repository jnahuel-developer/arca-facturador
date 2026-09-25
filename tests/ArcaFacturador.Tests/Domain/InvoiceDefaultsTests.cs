using ArcaFacturador.Domain;

namespace ArcaFacturador.Tests.Domain;

public class InvoiceDefaultsTests
{
    [Fact]
    public void Values_MatchTheDefinedMvpInvoice()
    {
        Assert.Equal("Factura C", InvoiceDefaults.ReceiptType);
        Assert.Equal("Consumidor final", InvoiceDefaults.CustomerType);
        Assert.Equal("Transferencia bancaria", InvoiceDefaults.PaymentMethod);
        Assert.Equal("Servicios", InvoiceDefaults.Concept);
        Assert.Equal("0001", InvoiceDefaults.ProductCode);
        Assert.Equal("Honorarios por servicio", InvoiceDefaults.ProductDescription);
        Assert.Equal(1m, InvoiceDefaults.Quantity);
        Assert.Equal("Otras unidades", InvoiceDefaults.Unit);
    }
}
