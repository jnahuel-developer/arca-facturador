using System.IO;
using System.Net.Http;
using System.Windows;
using ArcaFacturador.Arca;
using ArcaFacturador.Documents;
using ArcaFacturador.Persistence;
using ArcaFacturador.Persistence.Repositories;
using ArcaFacturador.Presentation;
using Microsoft.Data.Sqlite;

namespace ArcaFacturador;

public partial class MainWindow : Window
{
    private readonly InvoiceFormViewModel _viewModel;
    private readonly InvoiceRepository _invoiceRepository;
    private readonly ProductRepository _productRepository;
    private readonly InvoicePdfGenerator _invoicePdfGenerator;
    private readonly ArcaInvoiceEmissionService _arcaEmissionService;
    private readonly ArcaConfigurationStore _arcaConfigurationStore;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new InvoiceFormViewModel();
        var database = new SqliteDatabase(LocalDataPaths.DatabaseFilePath);
        database.Initialize();
        _invoiceRepository = new InvoiceRepository(database);
        _productRepository = new ProductRepository(database);
        _invoicePdfGenerator = new InvoicePdfGenerator();
        _arcaConfigurationStore = new ArcaConfigurationStore();
        _arcaEmissionService = new ArcaInvoiceEmissionService(
            _invoiceRepository,
            _invoicePdfGenerator,
            new LocalArcaRuntimeFactory());
        DataContext = _viewModel;
        ReloadFrequentPrices();
        LoadArcaConfiguration();
    }

    private void ValidateButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TryPrepareInvoice(out _);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryPrepareInvoice(out var invoice) || invoice is null)
        {
            return;
        }

        var result = MessageBox.Show(
            _viewModel.BuildConfirmationMessage(invoice),
            "Confirmar emisión local",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var storedInvoice = _invoiceRepository.Add(invoice);
            var pdfPath = _invoicePdfGenerator.Generate(
                storedInvoice,
                LocalDataPaths.GetInvoicePdfFilePath(storedInvoice.Id));
            var invoiceWithPdf = _invoiceRepository.UpdatePdfPath(storedInvoice.Id, pdfPath);
            _viewModel.MarkAsSaved(invoiceWithPdf);
        }
        catch (InvalidOperationException)
        {
            _viewModel.ShowPersistenceError();
        }
        catch (SqliteException)
        {
            _viewModel.ShowPersistenceError();
        }
        catch (IOException)
        {
            _viewModel.ShowPdfError();
        }
        catch (UnauthorizedAccessException)
        {
            _viewModel.ShowPdfError();
        }
    }

    private async void TestArcaConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.ShowArcaError("Probando conexión con ARCA. Esta operación no emite facturas.");
            var result = await _arcaEmissionService.TestConnectionAsync();
            _viewModel.MarkConnectionTested(result);
        }
        catch (Exception exception) when (IsUserFacingArcaException(exception))
        {
            _viewModel.ShowArcaError(ArcaUserMessageBuilder.FromException(exception));
        }
    }

    private void SaveArcaConfigurationButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryPrepareArcaConfiguration(out var form) || form is null)
        {
            return;
        }

        try
        {
            _arcaConfigurationStore.Save(form);
            _viewModel.MarkArcaConfigurationSaved(form);
        }
        catch (Exception exception) when (IsUserFacingArcaException(exception))
        {
            _viewModel.ShowArcaConfigurationError(ArcaUserMessageBuilder.FromException(exception));
        }
    }

    private async void EmitElectronicInvoiceButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryPrepareInvoice(out var invoice) || invoice is null)
        {
            return;
        }

        ArcaOperationPreview preview;
        try
        {
            preview = _arcaEmissionService.GetPreview();
        }
        catch (Exception exception) when (IsUserFacingArcaException(exception))
        {
            _viewModel.ShowArcaError(ArcaUserMessageBuilder.FromException(exception));
            return;
        }

        var result = MessageBox.Show(
            _viewModel.BuildElectronicConfirmationMessage(invoice, preview),
            preview.IsProduction ? "Confirmar factura electrónica real" : "Confirmar emisión ARCA",
            MessageBoxButton.YesNo,
            preview.IsProduction ? MessageBoxImage.Warning : MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var emissionResult = await _arcaEmissionService.EmitAsync(invoice);
            _viewModel.MarkElectronicEmissionCompleted(emissionResult);
        }
        catch (Exception exception) when (IsUserFacingArcaException(exception))
        {
            _viewModel.ShowArcaError(ArcaUserMessageBuilder.FromException(exception));
        }
    }

    private void AddFrequentPriceButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryPrepareNewFrequentPrice(out var product) || product is null)
        {
            return;
        }

        try
        {
            var storedProduct = _productRepository.Add(product);
            ReloadFrequentPrices(storedProduct.Id);
            _viewModel.MarkFrequentPriceAdded();
        }
        catch (InvalidOperationException exception)
        {
            _viewModel.ShowCatalogError(exception.Message);
        }
        catch (SqliteException)
        {
            _viewModel.ShowCatalogError("No se pudo guardar el importe frecuente.");
        }
    }

    private void UpdateFrequentPriceButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryPrepareSelectedFrequentPrice(out var product) || product is null)
        {
            return;
        }

        try
        {
            var updatedProduct = _productRepository.Update(product);
            ReloadFrequentPrices(updatedProduct.Id);
            _viewModel.MarkFrequentPriceUpdated();
        }
        catch (InvalidOperationException exception)
        {
            _viewModel.ShowCatalogError(exception.Message);
        }
        catch (SqliteException)
        {
            _viewModel.ShowCatalogError("No se pudo actualizar el importe frecuente.");
        }
    }

    private void DeleteFrequentPriceButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryGetSelectedFrequentPrice(out var selectedFrequentPrice) || selectedFrequentPrice is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Se eliminará el importe frecuente {selectedFrequentPrice.DisplayText}.",
            "Eliminar importe frecuente",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _productRepository.Delete(selectedFrequentPrice.Id);
            ReloadFrequentPrices();
            _viewModel.MarkFrequentPriceDeleted();
        }
        catch (InvalidOperationException exception)
        {
            _viewModel.ShowCatalogError(exception.Message);
        }
        catch (SqliteException)
        {
            _viewModel.ShowCatalogError("No se pudo borrar el importe frecuente.");
        }
    }

    private void ReloadFrequentPrices(long? selectedProductId = null)
    {
        _viewModel.LoadFrequentPrices(_productRepository.GetAll(), selectedProductId);
    }

    private void LoadArcaConfiguration()
    {
        try
        {
            _viewModel.LoadArcaConfiguration(_arcaConfigurationStore.LoadForm());
        }
        catch (Exception exception) when (IsUserFacingArcaException(exception))
        {
            _viewModel.ShowArcaConfigurationError(ArcaUserMessageBuilder.FromException(exception));
        }
    }

    private static bool IsUserFacingArcaException(Exception exception) =>
        exception is FileNotFoundException
            or InvalidOperationException
            or UnauthorizedAccessException
            or IOException
            or HttpRequestException
            or ArcaServiceException;
}
