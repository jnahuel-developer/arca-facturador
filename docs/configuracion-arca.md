# Configuración ARCA

## Estado

`mod002` incorpora el modelo local de configuración y las reglas fijas del comprobante. `mod007` prepara la autenticación WSAA en homologación. `mod008` prepara la emisión WSFEv1 de Factura C en homologación. `mod009` agrega reconciliación y cache de TA. `mod010` separa homologación de producción y agrega resguardos locales para evitar emisiones accidentales. Este documento no contiene credenciales reales.

## Configuración local implementada

- Un único CUIT emisor, normalizado a 11 dígitos y validado mediante su dígito verificador.
- Un único punto de venta, con valor permitido entre `1` y `99999`.

`mod003` incorpora el almacenamiento clave-valor necesario para persistir estos datos. La carga y edición desde la interfaz se implementará en una rama posterior.

## Configuración futura

La integración requerirá, como mínimo:

- CUIT emisor.
- Punto de venta habilitado para Web Services.
- Endpoint WSFEv1 de homologación o producción, según ambiente.
- Certificado digital y clave privada protegida localmente.
- Selección explícita de ambiente: homologación o producción.
- Direcciones de los servicios WSAA y WSFEv1.
- Manejo del Ticket de Acceso y su vencimiento.

La autenticación comienza en homologación en `mod007`, la emisión WSFEv1 se prepara en `mod008`, y el pasaje seguro a producción queda preparado en `mod010`.

## Seguridad

Los certificados, claves privadas, tickets, CUIT y configuración local sensible no deben confirmarse en Git. El `.gitignore` inicial excluye extensiones habituales de certificados y claves, además de `appsettings.Local.json`.

Para producción ver también [Producción y seguridad local](produccion-seguridad.md).

## Datos fijos del comprobante

- Tipo: `Factura C`.
- Receptor: `Consumidor final`.
- Medio de pago: `Transferencia bancaria`.
- Concepto: `Servicios`.
- Código local: `0001`.
- Descripción: `Honorarios por servicio`.
- Cantidad: `1`.
- Unidad local: `Otras unidades`.

Para una fecha de factura determinada, `FchServDesde` es el primer día del mismo mes y `FchServHasta` es el último día de ese mes. La fecha de factura (`CbteFch`) puede elegirse desde la interfaz sólo entre la fecha actual y hasta 10 días corridos hacia atrás. `FchVtoPago` queda fijado en la fecha actual de emisión/autorización.

Estas reglas son independientes de la interfaz y deberán ser reutilizadas por la persistencia, el PDF y la integración con ARCA para evitar diferencias entre representaciones.
