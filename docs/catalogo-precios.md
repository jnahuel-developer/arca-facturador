# Catálogo simple de importes frecuentes

`mod005` agrega un catálogo mínimo para reutilizar importes habituales al cargar una factura local.

## Qué hace

- Permite agregar importes frecuentes.
- Permite seleccionar un importe para completar el importe de la factura.
- Permite editar el importe seleccionado.
- Permite borrar el importe seleccionado.
- Guarda los valores en SQLite usando el producto fijo del MVP:
  - Código `0001`.
  - Descripción `Honorarios por servicio`.
  - Unidad `Otras unidades`.
- Evita duplicados exactos de importe.

## Qué no hace

- No administra productos reales.
- No administra stock.
- No permite cambiar descripción, código ni unidad desde la interfaz.
- No maneja listas comerciales, clientes ni precios por cliente.
- No emite ante ARCA.

El catálogo existe sólo para acelerar la carga manual del importe en el flujo local de facturación.
