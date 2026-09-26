# Facturación local simulada

`mod004` incorpora la primera pantalla operativa del facturador. El flujo permite cargar un importe, revisar los datos fijos del comprobante, validar las fechas calculadas y guardar una emisión local.

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

## Qué no hace todavía

- No invoca WSAA.
- No invoca WSFEv1.
- No solicita CAE.
- No genera PDF.
- No asigna número fiscal de comprobante.
- No usa catálogo de precios frecuentes.

Los registros guardados en esta etapa son una simulación local para preparar el flujo de trabajo. La emisión fiscal real se implementará en las ramas posteriores.
