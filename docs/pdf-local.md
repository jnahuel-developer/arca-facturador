# PDF local del comprobante

`mod006` agrega la generación de un PDF local para cada emisión guardada desde la pantalla principal.

## Ubicación

Los archivos se guardan en la carpeta local de la aplicación:

```text
%LOCALAPPDATA%\ArcaFacturador\facturas\
```

El nombre es determinístico a partir del identificador local de la factura:

```text
factura-local-00000001.pdf
factura-local-00000002.pdf
```

## Contenido

El PDF incluye:

- Tipo de comprobante `Factura C`.
- Número local de comprobante.
- Fecha de emisión.
- Período de servicio.
- Vencimiento de pago.
- Receptor `Consumidor final`.
- Medio de pago `Transferencia bancaria`.
- Concepto `Servicios`.
- Código local `0001`.
- Descripción `Honorarios por servicio`.
- Cantidad `1`.
- Unidad `Otras unidades`.
- Importe total.
- Campos reservados para CAE y vencimiento de CAE.

## Límites de esta etapa

- El PDF no autoriza fiscalmente la operación.
- El número fiscal queda pendiente hasta integrar WSFEv1.
- El CAE y su vencimiento quedan pendientes hasta la autorización de ARCA.
- Los datos sensibles de configuración fiscal no se versionan en Git.

La rama `mod008` deberá completar o regenerar el PDF con la información devuelta por ARCA.
