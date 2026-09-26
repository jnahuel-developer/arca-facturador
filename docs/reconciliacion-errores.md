# Reconciliación y errores críticos

`mod009` agrega defensas para operar con ARCA sin duplicar comprobantes ante cortes, timeouts o respuestas inciertas.

## Ticket WSAA

El Ticket de Acceso se cachea localmente mientras esté vigente.

Ubicación:

```text
%LOCALAPPDATA%\ArcaFacturador\wsaa-ticket-cache.json
```

Esto evita pedir un nuevo TA cuando ARCA todavía informa que ya existe uno válido.

## Emisión WSFEv1

Antes de pedir CAE, la aplicación:

- consulta el último comprobante autorizado;
- calcula el próximo número;
- guarda ese número localmente en la factura pendiente;
- consulta si ese número ya existe en ARCA antes de reintentar;
- si ARCA ya lo autorizó, recupera CAE y vencimiento;
- si el pedido falla con error recuperable, vuelve a consultar ese mismo número antes de propagar el error.

## Errores clasificados como recuperables

- Timeouts.
- Problemas transitorios de conexión.
- Indisponibilidad del backend de homologación, por ejemplo `ORA-01034`.

## Qué no resuelve todavía

- No reemplaza una auditoría fiscal completa.
- No automatiza reintentos infinitos.
- No pasa a producción.
- No oculta ni rota secretos; eso corresponde a `mod010`.

La regla general queda así: ante duda, primero consultar ARCA por el número ya intentado y recién después decidir si corresponde reintentar.
