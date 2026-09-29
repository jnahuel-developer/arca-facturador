# Emisión real desde la GUI

## Estado de la mod011

`mod011` conecta la pantalla principal con el flujo real de ARCA. A partir de esta rama, la app puede:

- editar y guardar la configuración ARCA desde la GUI;
- alternar entre `Homologacion` y `Produccion`;
- probar conexión con ARCA sin emitir comprobantes;
- guardar una factura pendiente local;
- solicitar CAE mediante WSAA + WSFEv1;
- persistir autorización, rechazo o estado pendiente de revisión;
- regenerar el PDF local con CAE cuando ARCA autoriza;
- mostrar mensajes accionables sin exponer token, sign ni secretos.

## Acciones disponibles en la pantalla

La app abre maximizada y separa la operación en dos pestañas:

- `Facturación`: carga de importes, borradores, prueba de conexión y emisión.
- `Comprobantes`: listado local de comprobantes generados desde la aplicación y apertura del PDF.
- `Configuración ARCA`: ambiente, CUIT, punto de venta y certificado.

### Configuración ARCA

En la pestaña `Configuración ARCA` se cargan:

- ambiente: `Homologacion` o `Produccion`;
- CUIT emisor;
- punto de venta;
- ruta del certificado PFX;
- contraseña del PFX, oculta en pantalla;
- confirmación explícita de uso productivo.

Al guardar, la app crea o actualiza `appsettings.Local.json`, que no se versiona. Si se elige producción, la app escribe automáticamente las URLs productivas oficiales y exige la confirmación explícita.

## Uso paso a paso

1. Abrir la pestaña `Configuración ARCA`.
2. Elegir ambiente:
   - `Homologacion` para pruebas.
   - `Produccion` para facturas reales.
3. Completar CUIT, punto de venta, ruta del PFX y contraseña.
4. Si el ambiente es `Produccion`, marcar la confirmación productiva.
5. Presionar `Guardar configuración`.
6. Ir a la pestaña `Facturación`.
7. Presionar `Probar conexión ARCA`.
8. Si la conexión responde OK, cargar importe y elegir la fecha de factura.
9. Presionar `Emitir factura electrónica`.
10. Revisar el comprobante en la pestaña `Comprobantes` y abrir el PDF generado.

La fecha de factura puede ser la fecha actual o un día anterior dentro de los últimos 10 días corridos. La aplicación no permite emitir a futuro ni usar una fecha anterior a la última Factura C autorizada en ARCA para ese punto de venta. El vencimiento de pago se mantiene automáticamente en la fecha actual de emisión/autorización.

### Guardar borrador

Guarda una factura local pendiente y genera PDF sin CAE. No envía información a ARCA.

### Probar conexión ARCA

Ejecuta una prueba no emisora:

1. carga `appsettings.Local.json`;
2. valida ambiente, CUIT, punto de venta y certificado;
3. obtiene TA mediante WSAA;
4. consulta `FECompUltimoAutorizado` en WSFEv1;
5. consulta el detalle del último comprobante autorizado para ajustar la fecha mínima permitida.

Esta acción no emite comprobantes.

### Emitir factura electrónica

Ejecuta el flujo fiscal real:

1. valida importe, fecha de factura y datos fijos;
2. carga configuración local;
3. muestra una confirmación previa con ambiente, CUIT, punto de venta, comprobante e importe;
4. guarda la factura local como pendiente;
5. obtiene TA;
6. consulta numeración;
7. solicita CAE;
8. persiste resultado;
9. genera PDF con CAE si ARCA autoriza.

En producción, la confirmación indica explícitamente que se emitirá una factura electrónica real.

Si la pantalla muestra `Homologacion`, no se emitirá una factura real aunque el flujo llegue a ARCA. Homologación sólo sirve para pruebas.

## Resultado de la operación

La app distingue estos casos:

- `Authorized`: ARCA autorizó y el PDF local quedó generado.
- `Recovered`: la autorización se recuperó mediante reconciliación.
- `AuthorizedWithPdfError`: ARCA autorizó, pero hubo un problema regenerando el PDF local.
- `Rejected`: ARCA rechazó la solicitud; no hay CAE.
- `PendingReview`: hubo un error recuperable o resultado incierto; no debe reintentarse a ciegas.

## PDF emitido

El PDF local se genera con estructura similar al comprobante web de ARCA:

- páginas `ORIGINAL`, `DUPLICADO` y `TRIPLICADO` para comprobantes autorizados;
- tipo `C` y código `011`;
- CUIT emisor, punto de venta, número fiscal y fecha;
- período facturado y vencimiento de pago;
- receptor consumidor final;
- detalle fijo `0001 - Honorarios por servicio`;
- totales;
- CAE y vencimiento de CAE.

El PDF se guarda en la carpeta local de facturas de la app y puede abrirse desde la pestaña `Comprobantes`.

## Checklist productivo mínimo

Antes de la primera emisión real:

1. Confirmar con el contador CUIT, punto de venta y condición fiscal.
2. Confirmar que el punto de venta productivo esté habilitado para Web Services.
3. Usar certificado productivo, no el de homologación.
4. Completar `appsettings.Local.json` con `Environment = Produccion`.
5. Confirmar que producción esté habilitada mediante:

```json
{
  "AllowProduction": true,
  "ProductionConfirmation": "CONFIRMO_USO_PRODUCCION"
}
```

6. Ejecutar `Probar conexión ARCA`.
7. Emitir una primera factura real controlada.
8. Verificar el comprobante en ARCA.
9. Confirmar que el PDF local contiene número, CAE y vencimiento de CAE.

## Seguridad operativa

- No se versionan certificados ni configuración real.
- No se muestran `token` ni `sign` en pantalla.
- Los errores recuperables piden revisar/reconciliar antes de reintentar.
- La emisión real requiere confirmación visual previa.
