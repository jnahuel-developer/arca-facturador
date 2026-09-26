# Facturación local simulada

`mod004` incorpora la primera pantalla operativa del facturador. El flujo permite cargar un importe, revisar los datos fijos del comprobante, validar las fechas calculadas y guardar una emisión local. Desde `mod011`, la misma pantalla también puede probar conexión con ARCA y emitir la factura electrónica real cuando la configuración local está completa.

## Qué hace

- Muestra los datos fijos definidos para el MVP:
  - Factura C.
  - Consumidor final.
  - Transferencia bancaria.
  - Concepto Servicios.
  - Producto local `0001 - Honorarios por servicio`.
  - Cantidad `1`.
  - Unidad `Otras unidades`.
- Calcula las fechas según la regla acordada:
  - `FchServDesde`: primer día del mes de emisión.
  - `FchServHasta`: último día del mes de emisión.
  - `FchVtoPago`: fecha de emisión.
- Valida que el importe sea numérico, mayor que cero y con hasta dos decimales.
- Pide confirmación antes de guardar.
- Guarda un registro en SQLite con estado `Pending`.
- Genera un PDF local con los datos disponibles y campos pendientes de autorización.

## Emisión electrónica real

La acción `Emitir factura electrónica` invoca WSAA y WSFEv1, solicita CAE, guarda el resultado fiscal y regenera el PDF local con CAE si ARCA autoriza.

La acción `Guardar borrador` se mantiene para guardar una factura local sin enviar información a ARCA.
