using System.IO;
using System.Net.Http;

namespace ArcaFacturador.Arca;

public static class ArcaUserMessageBuilder
{
    public static string FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            FileNotFoundException fileNotFoundException => $"Falta un archivo de configuración o certificado: {fileNotFoundException.FileName}",
            UnauthorizedAccessException => "No hay permisos suficientes para leer la configuración, el certificado o escribir los datos locales.",
            InvalidOperationException invalidOperationException => invalidOperationException.Message,
            ArcaServiceException arcaException => FromArcaException(arcaException),
            HttpRequestException => "No se pudo conectar con ARCA. Revisá la conexión a internet e intentá nuevamente.",
            IOException => "No se pudo leer o escribir un archivo local necesario para la operación.",
            _ => "No se pudo completar la operación. Revisá la configuración local e intentá nuevamente.",
        };
    }

    private static string FromArcaException(ArcaServiceException exception)
    {
        return exception.Kind switch
        {
            ArcaServiceErrorKind.RemoteUnavailable => "ARCA respondió con una falla interna del servicio. No reintentes a ciegas: probá conexión o reconciliá antes de emitir nuevamente.",
            ArcaServiceErrorKind.Recoverable => "La operación quedó con resultado incierto por un problema de comunicación. La app intentará reconciliar antes de reintentar.",
            ArcaServiceErrorKind.ExistingValidTicket => "ARCA informó que ya existe un ticket de acceso válido. Cerrá y volvé a probar; si persiste, borrá el cache local del TA.",
            _ => "ARCA rechazó o no pudo procesar la operación. Revisá el detalle técnico y la configuración local.",
        };
    }
}
