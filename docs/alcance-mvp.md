# Alcance del MVP

## Propósito

El producto es una herramienta local para automatizar la facturación de un negocio concreto. No es un sistema de gestión general ni se diseña para escalar a múltiples empresas, equipos o usuarios.

## Incluido

- Ejecución local en una única PC Windows.
- Un usuario, sin autenticación ni roles.
- Un CUIT emisor y un punto de venta.
- Emisión exclusiva de Factura C a consumidor final.
- Medio de pago fijo: transferencia bancaria.
- Concepto fiscal: servicios.
- Código local de producto `0001`.
- Descripción fija `Honorarios por servicio`.
- Cantidad fija `1`.
- Unidad local `Otras unidades`.
- Importe editable y catálogo sencillo de precios frecuentes.
- Autorización mediante WSAA y WSFEv1.
- Generación y conservación local del PDF con CAE.
- Persistencia mínima de configuración, productos frecuentes y facturas emitidas.

Para cada comprobante de servicios:

- `CbteFch`: fecha de factura elegida por el usuario, sólo entre la fecha actual y hasta 10 días corridos hacia atrás, y nunca anterior a la fecha del último comprobante autorizado en ARCA para el mismo punto de venta y tipo.
- `FchServDesde`: primer día del mes de la fecha de factura.
- `FchServHasta`: último día del mes de la fecha de factura.
- `FchVtoPago`: fecha actual de emisión/autorización, no la fecha histórica elegida para la factura.

## Excluido

- Control de stock.
- Gestión de clientes.
- Multiusuario, roles y auditoría avanzada.
- Múltiples CUIT o puntos de venta.
- Notas de crédito.
- Otros tipos de factura o medios de pago.
- Reportes avanzados.
- Operación en la nube o desde otras computadoras.
- Arquitectura orientada al escalamiento.

Los datos de ítem se utilizan en la aplicación y el PDF local. La autorización por WSFEv1 se implementará según los totales y campos fiscales exigidos por el servicio.
