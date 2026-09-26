# WSAA en homologación

`mod007` prepara la autenticación contra WSAA en ambiente de homologación. Esta etapa no emite facturas y no invoca WSFEv1.

## Qué queda implementado

- Armado del `loginTicketRequest` para el servicio `wsfe`.
- Firma CMS con certificado local y clave privada.
- Cliente SOAP para `loginCms`.
- Interpretación del `loginTicketResponse`.
- Caché local del ticket mientras siga vigente.
- Carga de configuración local desde un archivo no versionado.
- Ejemplo de configuración en `appsettings.example.json`.

## Datos necesarios para la prueba real

- CUIT representado.
- Certificado de homologación emitido por ARCA.
- Clave privada asociada al certificado.
- Autorización del certificado para el servicio `wsfe`.
- Endpoint de homologación:

```text
https://wsaahomo.afip.gov.ar/ws/services/LoginCms
```

## Archivo local

Copiar:

```text
appsettings.example.json
```

como:

```text
appsettings.Local.json
```

Luego completar los datos reales. Ese archivo está ignorado por Git.

Ejemplo usando PFX:

```json
{
  "Arca": {
    "Environment": "Homologacion",
    "RepresentedCuit": "20111111112",
    "PointOfSale": 1,
    "Service": "wsfe",
    "LoginUrl": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
    "WsfeUrl": "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
    "TicketLifetimeHours": 12,
    "Certificate": {
      "PfxPath": "C:\\ARCA\\homologacion\\certificado-homologacion.pfx",
      "PfxPassword": "password-local-del-pfx",
      "CertificatePemPath": null,
      "PrivateKeyPemPath": null,
      "PrivateKeyPassword": null
    }
  }
}
```

También se puede configurar con certificado y clave privada en formato PEM:

```json
{
  "Arca": {
    "Environment": "Homologacion",
    "RepresentedCuit": "20111111112",
    "PointOfSale": 1,
    "Service": "wsfe",
    "LoginUrl": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
    "WsfeUrl": "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
    "TicketLifetimeHours": 12,
    "Certificate": {
      "PfxPath": null,
      "PfxPassword": null,
      "CertificatePemPath": "C:\\ARCA\\homologacion\\certificado.crt",
      "PrivateKeyPemPath": "C:\\ARCA\\homologacion\\clave.key",
      "PrivateKeyPassword": null
    }
  }
}
```

## Qué no se versiona

- `appsettings.Local.json`.
- Cache local de tickets WSAA.
- Certificados.
- Claves privadas.
- Archivos PFX/P12.
- Solicitudes CSR.
- Tickets de acceso.

## Prueba real pendiente

La prueba contra ARCA debe hacerse después de integrar `mod007`, cuando estén cargados el certificado y la autorización del servicio en homologación. La aplicación ya contiene las piezas técnicas para pedir el ticket; falta completar los datos locales reales y ejecutar la prueba controlada.

Desde `mod009`, el ticket se guarda localmente en:

```text
%LOCALAPPDATA%\ArcaFacturador\wsaa-ticket-cache.json
```
