# Configuración ARCA

## Estado

`mod002` incorpora el modelo local de configuración y las reglas fijas del comprobante, pero no implementa autenticación ni emisión contra ARCA. Este documento no contiene credenciales reales.

## Configuración local implementada

- Un único CUIT emisor, normalizado a 11 dígitos y validado mediante su dígito verificador.
- Un único punto de venta, con valor permitido entre `1` y `99999`.

`mod003` incorpora el almacenamiento clave-valor necesario para persistir estos datos. La carga y edición desde la interfaz se implementará en una rama posterior.

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

- Tipo: `Factura C`.
- Receptor: `Consumidor final`.
- Medio de pago: `Transferencia bancaria`.
- Concepto: `Servicios`.
- Código local: `0001`.
- Descripción: `Honorarios por servicio`.
- Cantidad: `1`.
- Unidad local: `Otras unidades`.

Para una fecha de emisión determinada, `FchServDesde` es el primer día del mismo mes, `FchServHasta` es el último día de ese mes y `FchVtoPago` coincide con la fecha de emisión.

Estas reglas son independientes de la interfaz y deberán ser reutilizadas por la persistencia, el PDF y la integración con ARCA para evitar diferencias entre representaciones.
