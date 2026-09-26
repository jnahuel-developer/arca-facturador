# WSFEv1 en homologación para Factura C

`mod008` prepara la emisión de Factura C de servicios contra WSFEv1 en ambiente de homologación.

## Qué queda implementado

- Cliente SOAP para WSFEv1.
- Consulta del último comprobante autorizado mediante `FECompUltimoAutorizado`.
- Armado de solicitud `FECAESolicitar`.
- Numeración automática a partir del último comprobante autorizado.
- Factura C de servicios con consumidor final:
  - `CbteTipo`: `11`.
  - `Concepto`: `2`.
  - `DocTipo`: `99`.
  - `DocNro`: `0`.
  - `CondicionIVAReceptorId`: `5`.
  - `MonId`: `PES`.
  - `MonCotiz`: `1`.
- Fechas de servicio ya definidas:
  - `FchServDesde`: primer día del mes.
  - `FchServHasta`: último día del mes.
  - `FchVtoPago`: fecha de emisión.
- Interpretación de aprobación, rechazo, observaciones y errores.
- Persistencia de:
  - número fiscal autorizado;
  - estado autorizado o rechazado;
  - CAE;
  - vencimiento del CAE.
- Regeneración del PDF local cuando la factura queda autorizada.

## Qué falta para la prueba real

Antes de probar contra ARCA hacen falta los datos reales en `appsettings.Local.json`:

- CUIT representado.
- Punto de venta habilitado para Web Services.
- Certificado y clave de homologación.
- Autorización del certificado para el servicio `wsfe`.

El ejemplo versionado está en `appsettings.example.json`. El archivo real `appsettings.Local.json` no se versiona.

## Endpoint de homologación

```text
https://wswhomo.afip.gov.ar/wsfev1/service.asmx
```

## Límites de esta etapa

- No se dispara automáticamente desde la pantalla principal.
- No implementa reconciliación ante timeouts o respuestas inciertas; eso queda para `mod009`.
- No prepara producción; eso queda para `mod010`.
- No versiona certificados, claves ni tickets.

La prueba manual real deberá hacerse en forma controlada una vez cargada la configuración local y confirmada la autorización del servicio en ARCA.
