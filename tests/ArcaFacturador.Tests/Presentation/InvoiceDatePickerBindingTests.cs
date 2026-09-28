using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using ArcaFacturador.Presentation;

namespace ArcaFacturador.Tests.Presentation;

public class InvoiceDatePickerBindingTests
{
    [Fact]
    public void DatePickerBindings_LoadInWpfRuntime()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var viewModel = new InvoiceFormViewModel(() => new DateOnly(2026, 9, 25));
                var datePicker = new DatePicker();
                datePicker.SetBinding(
                    DatePicker.DisplayDateStartProperty,
                    new Binding(nameof(InvoiceFormViewModel.IssueDateMinimumDate)) { Mode = BindingMode.OneWay });
                datePicker.SetBinding(
                    DatePicker.DisplayDateEndProperty,
                    new Binding(nameof(InvoiceFormViewModel.IssueDateMaximumDate)) { Mode = BindingMode.OneWay });
                datePicker.SetBinding(
                    DatePicker.SelectedDateProperty,
                    new Binding(nameof(InvoiceFormViewModel.IssueDatePickerDate))
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    });

                var window = new Window
                {
                    DataContext = viewModel,
                    Content = datePicker,
                    Width = 200,
                    Height = 100,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10_000,
                    Top = -10_000,
                };

                window.Show();
                window.UpdateLayout();
                window.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }
}
