using ArcaFacturador.Arca;

namespace ArcaFacturador.Tests.Arca;

public class ArcaErrorClassifierTests
{
    [Theory]
    [InlineData("El CEE ya posee un TA valido para el acceso al WSN solicitado", ArcaServiceErrorKind.ExistingValidTicket)]
    [InlineData("ORA-01034: ORACLE not available", ArcaServiceErrorKind.RemoteUnavailable)]
    [InlineData("The request timed out.", ArcaServiceErrorKind.Recoverable)]
    [InlineData("Error desconocido", ArcaServiceErrorKind.Unknown)]
    public void Classify_ReturnsExpectedKind(string message, ArcaServiceErrorKind expectedKind)
    {
        Assert.Equal(expectedKind, ArcaErrorClassifier.Classify(message));
    }
}
