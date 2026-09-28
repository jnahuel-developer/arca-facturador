namespace ArcaFacturador.Tests.Packaging;

public class PackagingScriptsTests
{
    [Theory]
    [InlineData("Publish-ArcaFacturador.ps1")]
    [InlineData("Install-ArcaFacturador.ps1")]
    [InlineData("Uninstall-ArcaFacturador.ps1")]
    [InlineData("Backup-ArcaFacturadorData.ps1")]
    [InlineData("Instalar.cmd")]
    [InlineData("Desinstalar.cmd")]
    [InlineData("Backup.cmd")]
    public void PackagingScript_Exists(string fileName)
    {
        var scriptPath = Path.Combine(GetRepositoryRoot(), "scripts", fileName);

        Assert.True(File.Exists(scriptPath), $"No existe el script requerido: {scriptPath}");
    }

    [Fact]
    public void UninstallScript_RequiresExplicitConfirmationBeforeRemovingLocalData()
    {
        var scriptPath = Path.Combine(GetRepositoryRoot(), "scripts", "Uninstall-ArcaFacturador.ps1");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("RemoveLocalData", script, StringComparison.Ordinal);
        Assert.Contains("ConfirmRemoveLocalData", script, StringComparison.Ordinal);
        Assert.Contains("Los datos locales no se borran", script, StringComparison.Ordinal);
    }

    [Fact]
    public void InstallScript_UsesLocalAppDataProgramsByDefault()
    {
        var scriptPath = Path.Combine(GetRepositoryRoot(), "scripts", "Install-ArcaFacturador.ps1");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("Programs\\ArcaFacturador", script, StringComparison.Ordinal);
        Assert.Contains("Los datos locales permanecen", script, StringComparison.Ordinal);
    }

    [Fact]
    public void CommandWrappers_UseProcessScopedExecutionPolicyBypass()
    {
        var scriptsPath = Path.Combine(GetRepositoryRoot(), "scripts");

        foreach (var fileName in new[] { "Instalar.cmd", "Desinstalar.cmd", "Backup.cmd" })
        {
            var script = File.ReadAllText(Path.Combine(scriptsPath, fileName));

            Assert.Contains("-ExecutionPolicy Bypass", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("powershell.exe", script, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ArcaFacturador.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("No se pudo resolver la raíz del repositorio.");
    }
}
