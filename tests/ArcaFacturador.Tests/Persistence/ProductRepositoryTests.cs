using ArcaFacturador.Persistence.Models;
using ArcaFacturador.Persistence.Repositories;

namespace ArcaFacturador.Tests.Persistence;

public class ProductRepositoryTests
{
    [Fact]
    public void Add_PersistsAndReturnsAProduct()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new ProductRepository(temporaryDatabase.Database);
        var product = CreateProduct(125_000);

        var storedProduct = repository.Add(product);

        Assert.True(storedProduct.Id > 0);
        Assert.Equal(storedProduct, repository.GetById(storedProduct.Id));
    }

    [Fact]
    public void GetAll_ReturnsFrequentPricesInInsertionOrder()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new ProductRepository(temporaryDatabase.Database);
        var first = repository.Add(CreateProduct(100_000));
        var second = repository.Add(CreateProduct(150_000));

        var products = repository.GetAll();

        Assert.Equal([first, second], products);
    }

    [Fact]
    public void Add_RejectsANonPositivePrice()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new ProductRepository(temporaryDatabase.Database);

        Assert.Throws<ArgumentOutOfRangeException>(() => repository.Add(CreateProduct(0)));
    }

    private static ProductRecord CreateProduct(long unitPriceCents) => new(
        0,
        "0001",
        "Honorarios por servicio",
        "Otras unidades",
        unitPriceCents);
}
