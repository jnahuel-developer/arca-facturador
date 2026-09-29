# Manual operativo MVP

Versión estable del MVP: `1.0.0`.

Este sistema emite Factura C electrónica real para un único negocio, en una PC local, con un único CUIT y punto de venta Web Services.

## Uso diario

1. Abrir `ARCA Facturador` desde el Escritorio o el Menú Inicio.
2. Ir a `Configuración ARCA`.
3. Confirmar que el ambiente sea `Produccion`.
4. Presionar `Probar conexión ARCA`.
5. Ir a `Facturación`.
6. Cargar importe.
7. Elegir fecha de factura:
   - no futura;
   - hasta 10 días corridos hacia atrás;
   - nunca anterior a la última Factura C autorizada para ese punto de venta.
8. Presionar `Emitir factura electrónica`.
9. Revisar la confirmación.
10. Confirmar sólo si ambiente, CUIT, punto de venta, importe y fecha son correctos.
11. Ir a `Comprobantes`.
12. Abrir el PDF generado.

## Datos fijos del MVP

- Comprobante: Factura C.
- Cliente: consumidor final.
- Medio de pago: transferencia bancaria.
- Concepto: servicios.
- Producto local: `0001 - Honorarios por servicio`.
- Cantidad: `1`.
- Unidad local: `Otras unidades`.
- Vencimiento de pago: fecha actual de emisión/autorización.

## Dónde quedan los datos

```text
%LOCALAPPDATA%\ArcaFacturador
```

Ahí se guardan:

- base SQLite;
- PDFs;
- configuración local ARCA;
- cache WSAA.

La app instalada queda aparte:

```text
%LOCALAPPDATA%\Programs\ArcaFacturador
```

Actualizar la app no debe borrar los datos locales.

## Backup

Desde la carpeta del paquete de instalación:

```text
Backup.cmd
```

El backup se genera por defecto en:

```text
Escritorio\Backups ARCA Facturador
```

Conservar aparte el certificado PFX y su contraseña si están fuera de `%LOCALAPPDATA%\ArcaFacturador`.

## Actualización

1. Hacer backup.
2. Descomprimir el paquete nuevo.
3. Ejecutar `Instalar.cmd`.
4. Abrir la app.
5. Probar conexión ARCA.
6. Verificar que los comprobantes previos siguen listados.

## Qué no hace el MVP

- No administra stock.
- No administra clientes.
- No emite notas de crédito.
- No maneja múltiples CUIT.
- No maneja múltiples puntos de venta.
- No tiene usuarios ni roles.
- No reemplaza controles contables externos.
