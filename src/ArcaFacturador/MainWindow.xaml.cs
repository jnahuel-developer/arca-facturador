using System.Windows;
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

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new InvoiceFormViewModel();
        var database = new SqliteDatabase(LocalDataPaths.DatabaseFilePath);
        database.Initialize();
        _invoiceRepository = new InvoiceRepository(database);
        _productRepository = new ProductRepository(database);
        DataContext = _viewModel;
        ReloadFrequentPrices();
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
            _viewModel.MarkAsSaved(storedInvoice);
        }
        catch (InvalidOperationException)
        {
            _viewModel.ShowPersistenceError();
        }
        catch (SqliteException)
        {
            _viewModel.ShowPersistenceError();
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
}
