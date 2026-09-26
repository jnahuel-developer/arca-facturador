using System.Configuration;
using System.Windows;
using ArcaFacturador.Persistence;

namespace ArcaFacturador;

/// <summary>
/// Interaction logic for App.xaml.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        new SqliteDatabase(LocalDataPaths.DatabaseFilePath).Initialize();

        base.OnStartup(e);
    }
}
