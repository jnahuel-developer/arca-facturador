using ArcaFacturador.Persistence.Models;
using ArcaFacturador.Presentation;
using ArcaFacturador.Arca;
using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Domain;

namespace ArcaFacturador.Tests.Presentation;

public class InvoiceFormViewModelTests
{
    [Theory]
    [InlineData("125000", 12_500_000)]
    [InlineData("125000,50", 12_500_050)]
    [InlineData("125000.50", 12_500_050)]
    [InlineData("1.250", 125_000)]
    public void TryPrepareInvoice_AcceptsValidAmounts(string amountText, long expectedAmountCents)
    {
        var viewModel = CreateViewModel();
        viewModel.AmountText = amountText;

        var result = viewModel.TryPrepareInvoice(out var invoice);

        Assert.True(result);
        Assert.NotNull(invoice);
        Assert.Equal(expectedAmountCents, invoice.AmountCents);
        Assert.Equal(InvoiceStatus.Pending, invoice.Status);
        Assert.Null(invoice.ReceiptNumber);
        Assert.Null(invoice.Cae);
        Assert.Null(invoice.CaeExpirationDate);
        Assert.Null(invoice.PdfPath);
    }

    [Fact]
    public void TryPrepareInvoice_UsesSelectedIssueDateAndTodayAsPaymentDueDate()
    {
        var viewModel = CreateViewModel();
        viewModel.IssueDatePickerDate = new DateTime(2026, 9, 23);
        viewModel.AmountText = "50000";

        viewModel.TryPrepareInvoice(out var invoice);

        Assert.NotNull(invoice);
        Assert.Equal(new DateOnly(2026, 9, 23), invoice.IssueDate);
        Assert.Equal(new DateOnly(2026, 9, 1), invoice.ServiceFrom);
        Assert.Equal(new DateOnly(2026, 9, 30), invoice.ServiceTo);
        Assert.Equal(new DateOnly(2026, 9, 25), invoice.PaymentDueDate);
    }

    [Fact]
    public void TryPrepareInvoice_RejectsFutureIssueDate()
    {
        var viewModel = CreateViewModel();
        viewModel.IssueDatePickerDate = new DateTime(2026, 9, 26);
        viewModel.AmountText = "50000";

        var result = viewModel.TryPrepareInvoice(out var invoice);

        Assert.False(result);
        Assert.Null(invoice);
        Assert.Equal("La fecha de factura no puede ser futura.", viewModel.ValidationMessage);
    }

    [Fact]
    public void TryPrepareInvoice_RejectsIssueDateOlderThanTenDays()
    {
        var viewModel = CreateViewModel();
        viewModel.IssueDatePickerDate = new DateTime(2026, 9, 14);
        viewModel.AmountText = "50000";

        var result = viewModel.TryPrepareInvoice(out var invoice);

        Assert.False(result);
        Assert.Null(invoice);
        Assert.Equal("La fecha de factura no puede tener más de 10 días corridos hacia atrás.", viewModel.ValidationMessage);
    }

    [Fact]
    public void IssueDatePickerDate_ExposesAllowedRange()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(new DateTime(2026, 9, 15), viewModel.IssueDateMinimumDate);
        Assert.Equal(new DateTime(2026, 9, 25), viewModel.IssueDateMaximumDate);
    }

    [Theory]
    [InlineData("", "Ingresá el importe de la factura.")]
    [InlineData("texto", "Ingresá un importe válido, por ejemplo 125000,00.")]
    [InlineData("0", "El importe debe ser mayor que cero.")]
    [InlineData("-10", "El importe debe ser mayor que cero.")]
    [InlineData("10,999", "El importe puede tener como máximo dos decimales.")]
    public void TryPrepareInvoice_RejectsInvalidAmounts(string amountText, string expectedMessage)
    {
        var viewModel = CreateViewModel();
        viewModel.AmountText = amountText;

        var result = viewModel.TryPrepareInvoice(out var invoice);

        Assert.False(result);
        Assert.Null(invoice);
        Assert.Equal(expectedMessage, viewModel.ValidationMessage);
    }

    [Fact]
    public void MarkAsSaved_ClearsAmountAndShowsStatus()
    {
        var viewModel = CreateViewModel();
        var invoice = new InvoiceRecord(
            Id: 7,
            ReceiptNumber: null,
            IssueDate: new DateOnly(2026, 9, 25),
            ServiceFrom: new DateOnly(2026, 9, 1),
            ServiceTo: new DateOnly(2026, 9, 30),
            PaymentDueDate: new DateOnly(2026, 9, 25),
            AmountCents: 12_500_000,
            Status: InvoiceStatus.Pending,
            Cae: null,
            CaeExpirationDate: null,
            PdfPath: @"C:\Facturas\factura-local-00000007.pdf");

        viewModel.AmountText = "125000";
        viewModel.TryPrepareInvoice(out _);
        viewModel.MarkAsSaved(invoice);

        Assert.Equal(string.Empty, viewModel.AmountText);
        Assert.Null(viewModel.AmountPreview);
        Assert.Contains("#7", viewModel.StatusMessage);
        Assert.Contains("PDF local generado", viewModel.StatusMessage);
        Assert.False(viewModel.HasValidationMessage);
    }

    [Fact]
    public void BuildElectronicConfirmationMessage_WhenProduction_WarnsRealInvoice()
    {
        var viewModel = CreateViewModel();
        viewModel.AmountText = "1250";
        viewModel.TryPrepareInvoice(out var invoice);
        var preview = new ArcaOperationPreview(
            ArcaEnvironmentName.Produccion,
            new FiscalConfiguration("20111111112", 1),
            "appsettings.Local.json");

        var message = viewModel.BuildElectronicConfirmationMessage(invoice!, preview);

        Assert.Contains("FACTURA ELECTRÓNICA REAL", message);
        Assert.Contains("20111111112", message);
        Assert.Contains("Punto de venta: 1", message);
    }

    [Fact]
    public void MarkElectronicEmissionCompleted_WhenAuthorized_ClearsAmountAndShowsCae()
    {
        var viewModel = CreateViewModel();
        viewModel.AmountText = "1250";
        var invoice = new InvoiceRecord(
            Id: 3,
            ReceiptNumber: 43,
            IssueDate: new DateOnly(2026, 9, 25),
            ServiceFrom: new DateOnly(2026, 9, 1),
            ServiceTo: new DateOnly(2026, 9, 30),
            PaymentDueDate: new DateOnly(2026, 9, 25),
            AmountCents: 125_000,
            Status: InvoiceStatus.Authorized,
            Cae: "74370123456789",
            CaeExpirationDate: new DateOnly(2026, 10, 5),
            PdfPath: @"C:\Facturas\factura.pdf");
        var result = new ArcaEmissionResult(
            ArcaEmissionStatus.Authorized,
            invoice,
            new WsfeCaeResponse("A", "A", 43, invoice.Cae, invoice.CaeExpirationDate, [], []));

        viewModel.MarkElectronicEmissionCompleted(result);

        Assert.Equal(string.Empty, viewModel.AmountText);
        Assert.Contains("autorizada", viewModel.StatusMessage);
        Assert.Contains("74370123456789", viewModel.StatusMessage);
    }

    [Fact]
    public void MarkElectronicEmissionCompleted_WhenRejected_KeepsAmountAndShowsErrors()
    {
        var viewModel = CreateViewModel();
        viewModel.AmountText = "1250";
        var invoice = new InvoiceRecord(
            Id: 3,
            ReceiptNumber: null,
            IssueDate: new DateOnly(2026, 9, 25),
            ServiceFrom: new DateOnly(2026, 9, 1),
            ServiceTo: new DateOnly(2026, 9, 30),
            PaymentDueDate: new DateOnly(2026, 9, 25),
            AmountCents: 125_000,
            Status: InvoiceStatus.Rejected,
            Cae: null,
            CaeExpirationDate: null,
            PdfPath: null);
        var result = new ArcaEmissionResult(
            ArcaEmissionStatus.Rejected,
            invoice,
            new WsfeCaeResponse("R", "R", 43, null, null, [], [new WsfeMessage(10016, "Rechazado")]));

        viewModel.MarkElectronicEmissionCompleted(result);

        Assert.Equal("1250", viewModel.AmountText);
        Assert.Contains("rechazó", viewModel.StatusMessage);
        Assert.Contains("10016", viewModel.StatusMessage);
    }

    [Fact]
    public void LoadFrequentPrices_SelectingOneFillsInvoiceAmount()
    {
        var viewModel = CreateViewModel();
        var product = new ProductRecord(
            Id: 3,
            Code: "0001",
            Description: "Honorarios por servicio",
            Unit: "Otras unidades",
            UnitPriceCents: 125_000);

        viewModel.LoadFrequentPrices([product]);
        viewModel.SelectedFrequentPrice = viewModel.FrequentPrices.Single();

        Assert.Equal("1250", viewModel.AmountText);
        Assert.Equal("1250", viewModel.FrequentPriceAmountText);
    }

    [Fact]
    public void TryPrepareNewFrequentPrice_UsesFixedProductData()
    {
        var viewModel = CreateViewModel();
        viewModel.FrequentPriceAmountText = "1500,50";

        var result = viewModel.TryPrepareNewFrequentPrice(out var product);

        Assert.True(result);
        Assert.NotNull(product);
        Assert.Equal(0, product.Id);
        Assert.Equal("0001", product.Code);
        Assert.Equal("Honorarios por servicio", product.Description);
        Assert.Equal("Otras unidades", product.Unit);
        Assert.Equal(150_050, product.UnitPriceCents);
    }

    [Fact]
    public void TryPrepareSelectedFrequentPrice_RequiresSelection()
    {
        var viewModel = CreateViewModel();
        viewModel.FrequentPriceAmountText = "1500";

        var result = viewModel.TryPrepareSelectedFrequentPrice(out var product);

        Assert.False(result);
        Assert.Null(product);
        Assert.Equal("Seleccioná un importe frecuente para editar.", viewModel.CatalogValidationMessage);
    }

    [Fact]
    public void TryPrepareArcaConfiguration_WhenProductionRequiresExplicitConfirmation()
    {
        var viewModel = CreateViewModel();
        viewModel.ArcaEnvironment = ArcaEnvironmentName.Produccion;
        viewModel.ArcaRepresentedCuit = "27354180753";
        viewModel.ArcaPointOfSaleText = "1";
        viewModel.ArcaPfxPath = @"C:\ARCA\produccion\certificado.pfx";

        var result = viewModel.TryPrepareArcaConfiguration(out var form);

        Assert.False(result);
        Assert.Null(form);
        Assert.Contains("producción", viewModel.ArcaConfigurationValidationMessage);
    }

    [Fact]
    public void TryPrepareArcaConfiguration_WhenProductionConfirmed_ReturnsForm()
    {
        var viewModel = CreateViewModel();
        viewModel.ArcaEnvironment = ArcaEnvironmentName.Produccion;
        viewModel.ArcaRepresentedCuit = "27354180753";
        viewModel.ArcaPointOfSaleText = "1";
        viewModel.ArcaPfxPath = @"C:\ARCA\produccion\certificado.pfx";
        viewModel.ArcaPfxPassword = "secret";
        viewModel.ArcaAllowProduction = true;

        var result = viewModel.TryPrepareArcaConfiguration(out var form);

        Assert.True(result);
        Assert.NotNull(form);
        Assert.Equal(ArcaEnvironmentName.Produccion, form.Environment);
        Assert.True(form.AllowProduction);
    }

    private static InvoiceFormViewModel CreateViewModel() =>
        new(() => new DateOnly(2026, 9, 25));
}
