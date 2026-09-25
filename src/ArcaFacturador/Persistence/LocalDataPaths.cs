using System.IO;

namespace ArcaFacturador.Persistence;

public static class LocalDataPaths
{
    public static string DatabaseFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArcaFacturador",
        "arca-facturador.db");
}
