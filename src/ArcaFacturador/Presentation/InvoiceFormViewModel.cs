using System.ComponentModel;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ArcaFacturador.Domain;
using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Presentation;

public sealed class InvoiceFormViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo ArgentineCulture = CultureInfo.GetCultureInfo("es-AR");
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    private static readonly NumberStyles AmountStyles = NumberStyles.Number;
    private string _amountText = string.Empty;
    private string? _validationMessage;
    private string? _statusMessage;
    private string? _amountPreview;
    private string _frequentPriceAmountText = string.Empty;
    private string? _catalogValidationMessage;
    private string? _catalogStatusMessage;
    private FrequentPriceItem? _selectedFrequentPrice;

    public InvoiceFormViewModel(Func<DateOnly>? todayProvider = null)
    {
        IssueDate = (todayProvider ?? Today)();
        var serviceDates = ServiceDateRules.ForIssueDate(IssueDate);
        ServiceFrom = serviceDates.ServiceFrom;
        ServiceTo = serviceDates.ServiceTo;
        PaymentDueDate = serviceDates.PaymentDueDate;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FrequentPriceItem> FrequentPrices { get; } = [];

    public string ReceiptType => InvoiceDefaults.ReceiptType;

    public string CustomerType => InvoiceDefaults.CustomerType;

    public string PaymentMethod => InvoiceDefaults.PaymentMethod;

    public string Concept => InvoiceDefaults.Concept;

    public string ProductCode => InvoiceDefaults.ProductCode;

    public string ProductDescription => InvoiceDefaults.ProductDescription;

    public decimal Quantity => InvoiceDefaults.Quantity;

    public string Unit => InvoiceDefaults.Unit;

    public DateOnly IssueDate { get; }

    public string IssueDateText => FormatDate(IssueDate);

    public DateOnly ServiceFrom { get; }

    public string ServiceFromText => FormatDate(ServiceFrom);

    public DateOnly ServiceTo { get; }

    public string ServiceToText => FormatDate(ServiceTo);

    public DateOnly PaymentDueDate { get; }

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

    public bool TryPrepareInvoice(out InvoiceRecord? invoice)
    {
        invoice = null;
        ValidationMessage = null;
        StatusMessage = null;
        AmountPreview = null;

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
               $"Período: {invoice.ServiceFrom:dd/MM/yyyy} al {invoice.ServiceTo:dd/MM/yyyy}.\n" +
               "Todavía no se enviará información a ARCA. ¿Querés continuar?";
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
