using ArcaFacturador.Persistence.Repositories;

namespace ArcaFacturador.Tests.Persistence;

public class SettingsRepositoryTests
{
    [Fact]
    public void Set_InsertsAndUpdatesAValue()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new SettingsRepository(temporaryDatabase.Database);

        repository.Set("issuer_cuit", "20123456786");
        repository.Set("issuer_cuit", "27345678901");

        Assert.Equal("27345678901", repository.Get("issuer_cuit"));
    }

    [Fact]
    public void Get_ReturnsNullForAMissingSetting()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new SettingsRepository(temporaryDatabase.Database);

        Assert.Null(repository.Get("missing"));
    }

    [Fact]
    public void Set_RejectsAnEmptyKey()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new SettingsRepository(temporaryDatabase.Database);

        Assert.Throws<ArgumentException>(() => repository.Set(" ", "value"));
    }
}
