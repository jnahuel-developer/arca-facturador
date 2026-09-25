# Configuración ARCA

## Estado

`mod001` no implementa autenticación ni emisión contra ARCA. Este documento es un punto de partida operativo para las ramas posteriores y no contiene credenciales reales.

## Configuración futura

La integración requerirá, como mínimo:

- CUIT emisor.
- Punto de venta habilitado para Web Services.
- Certificado digital y clave privada protegida localmente.
- Selección explícita de ambiente: homologación o producción.
- Direcciones de los servicios WSAA y WSFEv1.
- Manejo del Ticket de Acceso y su vencimiento.

La conexión comenzará en homologación en `mod007` y `mod008`. El pasaje a producción se preparará en `mod010`.

## Seguridad

Los certificados, claves privadas, tickets, CUIT y configuración local sensible no deben confirmarse en Git. El `.gitignore` inicial excluye extensiones habituales de certificados y claves, además de `appsettings.Local.json`.

## Datos fijos del comprobante

Las constantes funcionales y las reglas de fechas se formalizarán y probarán en `mod002`. La integración con ARCA deberá consumir esas mismas reglas para evitar diferencias entre la pantalla, el PDF y la solicitud fiscal.
