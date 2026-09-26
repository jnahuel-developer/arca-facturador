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

    [Fact]
    public void Add_RejectsADuplicateFrequentPrice()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new ProductRepository(temporaryDatabase.Database);
        repository.Add(CreateProduct(125_000));

        var exception = Assert.Throws<InvalidOperationException>(() => repository.Add(CreateProduct(125_000)));

        Assert.Contains("Ya existe", exception.Message);
    }

    [Fact]
    public void Update_UpdatesAnExistingProduct()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new ProductRepository(temporaryDatabase.Database);
        var storedProduct = repository.Add(CreateProduct(125_000));
        var updatedProduct = storedProduct with { UnitPriceCents = 150_000 };

        var result = repository.Update(updatedProduct);

        Assert.Equal(updatedProduct, result);
        Assert.Equal(updatedProduct, repository.GetById(storedProduct.Id));
    }

    [Fact]
    public void Update_RejectsADuplicateFrequentPrice()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new ProductRepository(temporaryDatabase.Database);
        repository.Add(CreateProduct(125_000));
        var secondProduct = repository.Add(CreateProduct(150_000));

        var exception = Assert.Throws<InvalidOperationException>(
            () => repository.Update(secondProduct with { UnitPriceCents = 125_000 }));

        Assert.Contains("Ya existe", exception.Message);
    }

    [Fact]
    public void Delete_RemovesAnExistingProduct()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new ProductRepository(temporaryDatabase.Database);
        var storedProduct = repository.Add(CreateProduct(125_000));

        repository.Delete(storedProduct.Id);

        Assert.Null(repository.GetById(storedProduct.Id));
        Assert.Empty(repository.GetAll());
    }

    private static ProductRecord CreateProduct(long unitPriceCents) => new(
        0,
        "0001",
        "Honorarios por servicio",
        "Otras unidades",
        unitPriceCents);
}
