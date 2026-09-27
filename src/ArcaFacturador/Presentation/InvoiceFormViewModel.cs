using System.ComponentModel;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ArcaFacturador.Arca;
using ArcaFacturador.Domain;
using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Presentation;

public sealed class InvoiceFormViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo ArgentineCulture = CultureInfo.GetCultureInfo("es-AR");
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    private static readonly NumberStyles AmountStyles = NumberStyles.Number;
    private readonly Func<DateOnly> _todayProvider;
    private string _amountText = string.Empty;
    private DateOnly _issueDate;
    private DateOnly _serviceFrom;
    private DateOnly _serviceTo;
    private string? _validationMessage;
    private string? _statusMessage;
    private string? _amountPreview;
    private string _frequentPriceAmountText = string.Empty;
    private string? _catalogValidationMessage;
    private string? _catalogStatusMessage;
    private FrequentPriceItem? _selectedFrequentPrice;
    private string _arcaEnvironment = ArcaEnvironmentName.Homologacion;
    private string _arcaRepresentedCuit = string.Empty;
    private string _arcaPointOfSaleText = "1";
    private string _arcaPfxPath = string.Empty;
    private string _arcaPfxPassword = string.Empty;
    private bool _arcaAllowProduction;
    private string? _arcaConfigurationValidationMessage;
    private string? _arcaConfigurationStatusMessage;
    private InvoiceHistoryItem? _selectedInvoiceHistoryItem;
    private string? _invoiceHistoryValidationMessage;
    private string? _invoiceHistoryStatusMessage;

    public InvoiceFormViewModel(Func<DateOnly>? todayProvider = null)
    {
        _todayProvider = todayProvider ?? Today;
        _issueDate = CurrentDate;
        RefreshServiceDates();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FrequentPriceItem> FrequentPrices { get; } = [];

    public ObservableCollection<InvoiceHistoryItem> InvoiceHistory { get; } = [];

    public IReadOnlyList<string> ArcaEnvironments { get; } =
    [
        ArcaEnvironmentName.Homologacion,
        ArcaEnvironmentName.Produccion,
    ];

    public string ReceiptType => InvoiceDefaults.ReceiptType;

    public string CustomerType => InvoiceDefaults.CustomerType;

    public string PaymentMethod => InvoiceDefaults.PaymentMethod;

    public string Concept => InvoiceDefaults.Concept;

    public string ProductCode => InvoiceDefaults.ProductCode;

    public string ProductDescription => InvoiceDefaults.ProductDescription;

    public decimal Quantity => InvoiceDefaults.Quantity;

    public string Unit => InvoiceDefaults.Unit;

    public DateOnly IssueDate
    {
        get => _issueDate;
        private set
        {
            if (!SetField(ref _issueDate, value))
            {
                return;
            }

            RefreshServiceDates();
            ClearMessages();
            OnPropertyChanged(nameof(IssueDateText));
            OnPropertyChanged(nameof(IssueDatePickerDate));
        }
    }

    public string IssueDateText => FormatDate(IssueDate);

    public DateTime? IssueDatePickerDate
    {
        get => IssueDate.ToDateTime(TimeOnly.MinValue);
        set
        {
            if (value is null)
            {
                ValidationMessage = "Seleccioná la fecha de factura.";
                return;
            }

            IssueDate = DateOnly.FromDateTime(value.Value.Date);
        }
    }

    public DateTime IssueDateMinimumDate =>
        InvoiceIssueDateRules.MinimumAllowed(CurrentDate).ToDateTime(TimeOnly.MinValue);

    public DateTime IssueDateMaximumDate =>
        InvoiceIssueDateRules.MaximumAllowed(CurrentDate).ToDateTime(TimeOnly.MinValue);

    public DateOnly ServiceFrom
    {
        get => _serviceFrom;
        private set
        {
            if (SetField(ref _serviceFrom, value))
            {
                OnPropertyChanged(nameof(ServiceFromText));
            }
        }
    }

    public string ServiceFromText => FormatDate(ServiceFrom);

    public DateOnly ServiceTo
    {
        get => _serviceTo;
        private set
        {
            if (SetField(ref _serviceTo, value))
            {
                OnPropertyChanged(nameof(ServiceToText));
            }
        }
    }

    public string ServiceToText => FormatDate(ServiceTo);

    public DateOnly PaymentDueDate => CurrentDate;

    public string PaymentDueDateText => FormatDate(PaymentDueDate);

    public string ProductSummary => $"{ProductCode} - {ProductDescription}";

    public string QuantitySummary => $"{Quantity:N0} {Unit}";

    public string AmountText
    {
        get => _amountText;
        set
        {
            if (SetField(ref _amountText, value))
            {
                ClearMessages();
            }
        }
    }

    public string? AmountPreview
    {
        get => _amountPreview;
        private set => SetField(ref _amountPreview, value);
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (SetField(ref _validationMessage, value))
            {
                OnPropertyChanged(nameof(HasValidationMessage));
            }
        }
    }

    public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetField(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public string FrequentPriceAmountText
    {
        get => _frequentPriceAmountText;
        set
        {
            if (SetField(ref _frequentPriceAmountText, value))
            {
                ClearCatalogMessages();
            }
        }
    }

    public FrequentPriceItem? SelectedFrequentPrice
    {
        get => _selectedFrequentPrice;
        set
        {
            if (!SetField(ref _selectedFrequentPrice, value) || value is null)
            {
                return;
            }

            AmountText = value.AmountText;
            _frequentPriceAmountText = value.AmountText;
            OnPropertyChanged(nameof(FrequentPriceAmountText));
            ClearCatalogMessages();
        }
    }

    public string? CatalogValidationMessage
    {
        get => _catalogValidationMessage;
        private set
        {
            if (SetField(ref _catalogValidationMessage, value))
            {
                OnPropertyChanged(nameof(HasCatalogValidationMessage));
            }
        }
    }

    public bool HasCatalogValidationMessage => !string.IsNullOrEmpty(CatalogValidationMessage);

    public string? CatalogStatusMessage
    {
        get => _catalogStatusMessage;
        private set
        {
            if (SetField(ref _catalogStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasCatalogStatusMessage));
            }
        }
    }

    public bool HasCatalogStatusMessage => !string.IsNullOrEmpty(CatalogStatusMessage);

    public string ArcaEnvironment
    {
        get => _arcaEnvironment;
        set
        {
            if (SetField(ref _arcaEnvironment, value))
            {
                ClearArcaConfigurationMessages();
                OnPropertyChanged(nameof(IsArcaProduction));
            }
        }
    }

    public bool IsArcaProduction =>
        string.Equals(ArcaEnvironmentName.Normalize(ArcaEnvironment), ArcaEnvironmentName.Produccion, StringComparison.Ordinal);

    public string ArcaRepresentedCuit
    {
        get => _arcaRepresentedCuit;
        set
        {
            if (SetField(ref _arcaRepresentedCuit, value))
            {
                ClearArcaConfigurationMessages();
            }
        }
    }

    public string ArcaPointOfSaleText
    {
        get => _arcaPointOfSaleText;
        set
        {
            if (SetField(ref _arcaPointOfSaleText, value))
            {
                ClearArcaConfigurationMessages();
            }
        }
    }

    public string ArcaPfxPath
    {
        get => _arcaPfxPath;
        set
        {
            if (SetField(ref _arcaPfxPath, value))
            {
                ClearArcaConfigurationMessages();
            }
        }
    }

    public string ArcaPfxPassword
    {
        get => _arcaPfxPassword;
        set
        {
            if (SetField(ref _arcaPfxPassword, value))
            {
                ClearArcaConfigurationMessages();
            }
        }
    }

    public bool ArcaAllowProduction
    {
        get => _arcaAllowProduction;
        set
        {
            if (SetField(ref _arcaAllowProduction, value))
            {
                ClearArcaConfigurationMessages();
            }
        }
    }

    public string? ArcaConfigurationValidationMessage
    {
        get => _arcaConfigurationValidationMessage;
        private set => SetField(ref _arcaConfigurationValidationMessage, value);
    }

    public string? ArcaConfigurationStatusMessage
    {
        get => _arcaConfigurationStatusMessage;
        private set => SetField(ref _arcaConfigurationStatusMessage, value);
    }

    public InvoiceHistoryItem? SelectedInvoiceHistoryItem
    {
        get => _selectedInvoiceHistoryItem;
        set => SetField(ref _selectedInvoiceHistoryItem, value);
    }

    public string? InvoiceHistoryValidationMessage
    {
        get => _invoiceHistoryValidationMessage;
        private set => SetField(ref _invoiceHistoryValidationMessage, value);
    }

    public string? InvoiceHistoryStatusMessage
    {
        get => _invoiceHistoryStatusMessage;
        private set => SetField(ref _invoiceHistoryStatusMessage, value);
    }

    public bool TryPrepareInvoice(out InvoiceRecord? invoice)
    {
        invoice = null;
        ValidationMessage = null;
        StatusMessage = null;
        AmountPreview = null;

        if (!InvoiceIssueDateRules.TryValidate(IssueDate, CurrentDate, out var issueDateErrorMessage))
        {
            ValidationMessage = issueDateErrorMessage;
            return false;
        }

        if (string.IsNullOrWhiteSpace(AmountText))
        {
            ValidationMessage = "Ingresá el importe de la factura.";
            return false;
        }

        if (!TryParseAmount(AmountText, out var amountInCents, out var errorMessage))
        {
            ValidationMessage = errorMessage;
            return false;
        }

        invoice = new InvoiceRecord(
            Id: 0,
            ReceiptNumber: null,
            IssueDate: IssueDate,
            ServiceFrom: ServiceFrom,
            ServiceTo: ServiceTo,
            PaymentDueDate: PaymentDueDate,
            AmountCents: amountInCents,
            Status: InvoiceStatus.Pending,
            Cae: null,
            CaeExpirationDate: null,
            PdfPath: null);
        AmountPreview = FormatAmount(invoice.AmountCents);
        return true;
    }

    public void LoadFrequentPrices(IEnumerable<ProductRecord> products, long? selectedProductId = null)
    {
        ArgumentNullException.ThrowIfNull(products);

        _selectedFrequentPrice = null;
        OnPropertyChanged(nameof(SelectedFrequentPrice));
        FrequentPrices.Clear();

        FrequentPriceItem? selectedItem = null;
        foreach (var product in products)
        {
            var item = FrequentPriceItem.FromProduct(product);
            FrequentPrices.Add(item);

            if (product.Id == selectedProductId)
            {
                selectedItem = item;
            }
        }

        if (selectedItem is not null)
        {
            SelectedFrequentPrice = selectedItem;
        }
    }

    public bool TryPrepareNewFrequentPrice(out ProductRecord? product)
    {
        return TryPrepareFrequentPrice(id: 0, out product);
    }

    public bool TryPrepareSelectedFrequentPrice(out ProductRecord? product)
    {
        product = null;
        if (SelectedFrequentPrice is null)
        {
            CatalogStatusMessage = null;
            CatalogValidationMessage = "Seleccioná un importe frecuente para editar.";
            return false;
        }

        return TryPrepareFrequentPrice(SelectedFrequentPrice.Id, out product);
    }

    public bool TryGetSelectedFrequentPrice(out FrequentPriceItem? selectedFrequentPrice)
    {
        selectedFrequentPrice = SelectedFrequentPrice;
        if (selectedFrequentPrice is not null)
        {
            return true;
        }

        CatalogStatusMessage = null;
        CatalogValidationMessage = "Seleccioná un importe frecuente para borrar.";
        return false;
    }

    public void MarkFrequentPriceAdded()
    {
        ShowCatalogStatus("Importe frecuente agregado.");
    }

    public void MarkFrequentPriceUpdated()
    {
        ShowCatalogStatus("Importe frecuente actualizado.");
    }

    public void MarkFrequentPriceDeleted()
    {
        _frequentPriceAmountText = string.Empty;
        OnPropertyChanged(nameof(FrequentPriceAmountText));
        ShowCatalogStatus("Importe frecuente eliminado.");
    }

    public void ShowCatalogError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        CatalogStatusMessage = null;
        CatalogValidationMessage = message;
    }

    public string BuildConfirmationMessage(InvoiceRecord invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return $"Se guardará una emisión local por {FormatAmount(invoice.AmountCents)}.\n\n" +
               $"Fecha de factura: {invoice.IssueDate:dd/MM/yyyy}\n" +
               $"Período: {invoice.ServiceFrom:dd/MM/yyyy} al {invoice.ServiceTo:dd/MM/yyyy}.\n" +
               $"Vencimiento de pago: {invoice.PaymentDueDate:dd/MM/yyyy}\n" +
               "Todavía no se enviará información a ARCA. ¿Querés continuar?";
    }

    public string BuildElectronicConfirmationMessage(InvoiceRecord invoice, ArcaOperationPreview preview)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(preview);

        var header = preview.IsProduction
            ? "ATENCIÓN: se emitirá una FACTURA ELECTRÓNICA REAL ante ARCA."
            : "Se emitirá un comprobante de prueba contra el ambiente de homologación de ARCA.";

        return $"{header}\n\n" +
               $"Ambiente: {preview.Environment}\n" +
               $"CUIT emisor: {preview.FiscalConfiguration.Cuit}\n" +
               $"Punto de venta: {preview.FiscalConfiguration.PointOfSale}\n" +
               $"Comprobante: {ReceiptType}\n" +
               $"Cliente: {CustomerType}\n" +
               $"Importe: {FormatAmount(invoice.AmountCents)}\n" +
               $"Fecha de factura: {invoice.IssueDate:dd/MM/yyyy}\n" +
               $"Período: {invoice.ServiceFrom:dd/MM/yyyy} al {invoice.ServiceTo:dd/MM/yyyy}\n" +
               $"Vencimiento de pago: {invoice.PaymentDueDate:dd/MM/yyyy}\n\n" +
               "Si ARCA autoriza la operación, se generará número de comprobante y CAE. ¿Querés continuar?";
    }

    public void MarkAsSaved(InvoiceRecord invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        ValidationMessage = null;
        StatusMessage = $"Emisión local #{invoice.Id} guardada por {FormatAmount(invoice.AmountCents)}. " +
                        BuildPdfStatus(invoice);
        AmountPreview = null;
        _amountText = string.Empty;
        OnPropertyChanged(nameof(AmountText));
    }

    public void MarkConnectionTested(ArcaConnectionTestResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        ValidationMessage = null;
        StatusMessage = $"Conexión ARCA OK en {result.Preview.Environment}. " +
                        $"CUIT {result.Preview.FiscalConfiguration.Cuit}, " +
                        $"PV {result.Preview.FiscalConfiguration.PointOfSale}, " +
                        $"última Factura C autorizada: {result.LastAuthorizedReceiptNumber}.";
    }

    public void MarkElectronicEmissionCompleted(ArcaEmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        ValidationMessage = null;
        StatusMessage = result.Status switch
        {
            ArcaEmissionStatus.Authorized => BuildAuthorizedStatus("Factura electrónica autorizada.", result.Invoice),
            ArcaEmissionStatus.Recovered => BuildAuthorizedStatus("Factura electrónica recuperada por reconciliación.", result.Invoice),
            ArcaEmissionStatus.AuthorizedWithPdfError => BuildAuthorizedStatus("Factura electrónica autorizada, pero no se pudo regenerar el PDF local.", result.Invoice),
            ArcaEmissionStatus.Rejected => BuildRejectedStatus(result),
            ArcaEmissionStatus.PendingReview => BuildPendingReviewStatus(result),
            _ => "La operación finalizó con estado desconocido.",
        };

        if (result.IsAuthorized)
        {
            AmountPreview = null;
            _amountText = string.Empty;
            OnPropertyChanged(nameof(AmountText));
        }
    }

    public void LoadInvoiceHistory(IEnumerable<InvoiceRecord> invoices)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        _selectedInvoiceHistoryItem = null;
        OnPropertyChanged(nameof(SelectedInvoiceHistoryItem));
        InvoiceHistory.Clear();

        foreach (var invoice in invoices.OrderByDescending(invoice => invoice.Id))
        {
            InvoiceHistory.Add(InvoiceHistoryItem.FromInvoice(invoice));
        }

        InvoiceHistoryValidationMessage = null;
        InvoiceHistoryStatusMessage = $"Comprobantes cargados: {InvoiceHistory.Count}.";
    }

    public bool TryGetSelectedInvoicePdfPath(out string pdfPath)
    {
        pdfPath = string.Empty;
        InvoiceHistoryValidationMessage = null;
        InvoiceHistoryStatusMessage = null;

        if (SelectedInvoiceHistoryItem is null)
        {
            InvoiceHistoryValidationMessage = "Seleccioná un comprobante para abrir el PDF.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedInvoiceHistoryItem.PdfPath))
        {
            InvoiceHistoryValidationMessage = "El comprobante seleccionado no tiene PDF generado.";
            return false;
        }

        pdfPath = SelectedInvoiceHistoryItem.PdfPath;
        return true;
    }

    public void MarkInvoicePdfOpened()
    {
        InvoiceHistoryValidationMessage = null;
        InvoiceHistoryStatusMessage = "PDF abierto.";
    }

    public void ShowInvoiceHistoryError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        InvoiceHistoryStatusMessage = null;
        InvoiceHistoryValidationMessage = message;
    }

    public void LoadArcaConfiguration(ArcaConfigurationForm form)
    {
        ArgumentNullException.ThrowIfNull(form);

        _arcaEnvironment = form.Environment;
        _arcaRepresentedCuit = form.RepresentedCuit;
        _arcaPointOfSaleText = form.PointOfSale.ToString(CultureInfo.InvariantCulture);
        _arcaPfxPath = form.PfxPath;
        _arcaPfxPassword = form.PfxPassword;
        _arcaAllowProduction = form.AllowProduction;
        OnPropertyChanged(nameof(ArcaEnvironment));
        OnPropertyChanged(nameof(IsArcaProduction));
        OnPropertyChanged(nameof(ArcaRepresentedCuit));
        OnPropertyChanged(nameof(ArcaPointOfSaleText));
        OnPropertyChanged(nameof(ArcaPfxPath));
        OnPropertyChanged(nameof(ArcaPfxPassword));
        OnPropertyChanged(nameof(ArcaAllowProduction));
    }

    public bool TryPrepareArcaConfiguration(out ArcaConfigurationForm? form)
    {
        form = null;
        ArcaConfigurationValidationMessage = null;
        ArcaConfigurationStatusMessage = null;

        var environment = ArcaEnvironmentName.Normalize(ArcaEnvironment);

        if (string.IsNullOrWhiteSpace(ArcaRepresentedCuit))
        {
            ArcaConfigurationValidationMessage = "Ingresá el CUIT emisor.";
            return false;
        }

        if (!int.TryParse(ArcaPointOfSaleText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pointOfSale))
        {
            ArcaConfigurationValidationMessage = "Ingresá un punto de venta numérico.";
            return false;
        }

        if (pointOfSale <= 0)
        {
            ArcaConfigurationValidationMessage = "El punto de venta debe ser mayor que cero.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ArcaPfxPath))
        {
            ArcaConfigurationValidationMessage = "Ingresá la ruta del certificado PFX.";
            return false;
        }

        if (environment == ArcaEnvironmentName.Produccion && !ArcaAllowProduction)
        {
            ArcaConfigurationValidationMessage = "Para producción marcá la confirmación explícita de uso productivo.";
            return false;
        }

        form = new ArcaConfigurationForm(
            environment,
            ArcaRepresentedCuit,
            pointOfSale,
            ArcaPfxPath,
            ArcaPfxPassword,
            ArcaAllowProduction);
        return true;
    }

    public void MarkArcaConfigurationSaved(ArcaConfigurationForm form)
    {
        ArgumentNullException.ThrowIfNull(form);

        ArcaConfigurationValidationMessage = null;
        ArcaConfigurationStatusMessage = $"Configuración ARCA guardada. {form.BuildSummary()}";
    }

    public void ShowArcaConfigurationError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        ArcaConfigurationStatusMessage = null;
        ArcaConfigurationValidationMessage = message;
    }

    public void ShowArcaError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        StatusMessage = null;
        ValidationMessage = message;
    }

    public void ShowPersistenceError()
    {
        StatusMessage = null;
        ValidationMessage = "No se pudo guardar la emisión local. Revisá el acceso a la base de datos e intentá nuevamente.";
    }

    public void ShowPdfError()
    {
        StatusMessage = null;
        ValidationMessage = "La emisión local se guardó, pero no se pudo generar el PDF. Revisá permisos y espacio disponible.";
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Today);

    private DateOnly CurrentDate => _todayProvider();

    private void RefreshServiceDates()
    {
        var serviceDates = ServiceDateRules.ForIssueDate(IssueDate, PaymentDueDate);
        ServiceFrom = serviceDates.ServiceFrom;
        ServiceTo = serviceDates.ServiceTo;
        OnPropertyChanged(nameof(PaymentDueDateText));
        OnPropertyChanged(nameof(IssueDateMinimumDate));
        OnPropertyChanged(nameof(IssueDateMaximumDate));
    }

    private static string FormatDate(DateOnly date) =>
        date.ToString("dd/MM/yyyy", ArgentineCulture);

    private static string FormatAmount(long amountInCents) =>
        (amountInCents / 100m).ToString("C2", ArgentineCulture);

    private static string BuildPdfStatus(InvoiceRecord invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.PdfPath))
        {
            return "Quedó pendiente de autorización en ARCA.";
        }

        return $"PDF local generado en {invoice.PdfPath}. Quedó pendiente de autorización en ARCA.";
    }

    private static string BuildAuthorizedStatus(string prefix, InvoiceRecord invoice)
    {
        var pdfStatus = string.IsNullOrWhiteSpace(invoice.PdfPath)
            ? "El PDF local quedó pendiente de revisar."
            : $"PDF: {invoice.PdfPath}.";

        return $"{prefix} Comprobante #{invoice.ReceiptNumber}, CAE {invoice.Cae}, " +
               $"vencimiento CAE {invoice.CaeExpirationDate:dd/MM/yyyy}. {pdfStatus}";
    }

    private static string BuildRejectedStatus(ArcaEmissionResult result)
    {
        var details = result.Response?.Errors.Count > 0
            ? string.Join(" ", result.Response.Errors.Select(error => $"{error.Code}: {error.Message}"))
            : "ARCA no informó un detalle adicional.";

        return $"ARCA rechazó la factura. No quedó autorizada. Detalle: {details}";
    }

    private static string BuildPendingReviewStatus(ArcaEmissionResult result)
    {
        var invoice = result.Invoice;
        var receiptText = invoice.ReceiptNumber is { } receiptNumber
            ? $" con número tentativo {receiptNumber}"
            : string.Empty;
        var detail = string.IsNullOrWhiteSpace(result.DetailMessage)
            ? string.Empty
            : $" Detalle: {result.DetailMessage}";

        return $"La operación quedó pendiente de revisión{receiptText}.{detail} No vuelvas a emitirla sin probar conexión y reconciliar.";
    }

    private bool TryPrepareFrequentPrice(long id, out ProductRecord? product)
    {
        product = null;
        CatalogValidationMessage = null;
        CatalogStatusMessage = null;

        if (string.IsNullOrWhiteSpace(FrequentPriceAmountText))
        {
            CatalogValidationMessage = "Ingresá el importe frecuente.";
            return false;
        }

        if (!TryParseAmount(FrequentPriceAmountText, out var amountInCents, out var errorMessage))
        {
            CatalogValidationMessage = errorMessage;
            return false;
        }

        product = new ProductRecord(
            id,
            ProductCode,
            ProductDescription,
            Unit,
            amountInCents);
        return true;
    }

    private void ShowCatalogStatus(string message)
    {
        CatalogValidationMessage = null;
        CatalogStatusMessage = message;
    }

    private static bool TryParseAmount(string amountText, out long amountInCents, out string errorMessage)
    {
        amountInCents = 0;
        errorMessage = string.Empty;

        foreach (var culture in GetAmountCultures(amountText))
        {
            if (!decimal.TryParse(amountText, AmountStyles, culture, out var amount))
            {
                continue;
            }

            if (amount <= 0)
            {
                errorMessage = "El importe debe ser mayor que cero.";
                return false;
            }

            var cents = amount * 100m;
            if (decimal.Truncate(cents) != cents)
            {
                errorMessage = "El importe puede tener como máximo dos decimales.";
                return false;
            }

            if (cents > long.MaxValue)
            {
                errorMessage = "El importe ingresado es demasiado grande.";
                return false;
            }

            amountInCents = decimal.ToInt64(cents);
            return true;
        }

        errorMessage = "Ingresá un importe válido, por ejemplo 125000,00.";
        return false;
    }

    private static IEnumerable<CultureInfo> GetAmountCultures(string amountText)
    {
        if (amountText.Contains(','))
        {
            yield return ArgentineCulture;
            yield break;
        }

        var dotIndex = amountText.LastIndexOf('.');
        if (dotIndex >= 0 && amountText.Length - dotIndex - 1 == 3)
        {
            yield return ArgentineCulture;
            yield return InvariantCulture;
            yield break;
        }

        yield return InvariantCulture;
        yield return ArgentineCulture;
    }

    private void ClearMessages()
    {
        ValidationMessage = null;
        StatusMessage = null;
        AmountPreview = null;
    }

    private void ClearCatalogMessages()
    {
        CatalogValidationMessage = null;
        CatalogStatusMessage = null;
    }

    private void ClearArcaConfigurationMessages()
    {
        ArcaConfigurationValidationMessage = null;
        ArcaConfigurationStatusMessage = null;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
