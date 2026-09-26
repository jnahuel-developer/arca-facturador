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

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new InvoiceFormViewModel();
        var database = new SqliteDatabase(LocalDataPaths.DatabaseFilePath);
        database.Initialize();
        _invoiceRepository = new InvoiceRepository(database);
        DataContext = _viewModel;
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
}
