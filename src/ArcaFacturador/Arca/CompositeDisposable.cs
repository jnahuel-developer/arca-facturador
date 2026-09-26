namespace ArcaFacturador.Arca;

internal sealed class CompositeDisposable(params IDisposable[] disposables) : IDisposable
{
    public void Dispose()
    {
        foreach (var disposable in disposables.Reverse())
        {
            disposable.Dispose();
        }
    }
}
