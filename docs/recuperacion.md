# Recuperación y contingencias

## ARCA rechaza la factura

Si ARCA rechaza, la factura no queda autorizada. Revisar el mensaje informado por la aplicación.

Caso frecuente:

- fecha de factura anterior al último comprobante autorizado;
- punto de venta incorrecto;
- certificado no autorizado;
- ambiente incorrecto.

No repetir a ciegas: primero probar conexión y revisar configuración.

## Operación pendiente de revisión

Si la aplicación indica resultado incierto o pendiente:

1. no volver a emitir inmediatamente;
2. probar conexión ARCA;
3. revisar el último comprobante autorizado;
4. verificar si el comprobante fue autorizado;
5. recién después decidir si corresponde reintentar.

El sistema intenta reconciliar para evitar duplicados, pero ante cortes de red o fallas de ARCA conviene confirmar antes de repetir.

## Restaurar backup

1. Cerrar la app.
2. Ubicar el backup `.zip`.
3. Hacer copia de seguridad de la carpeta actual:

```text
%LOCALAPPDATA%\ArcaFacturador
```

4. Reemplazar el contenido por el contenido del backup.
5. Abrir la app.
6. Verificar comprobantes y PDFs.

## Reinstalar la aplicación

La app puede reinstalarse ejecutando:

```text
Instalar.cmd
```

Esto reemplaza binarios, pero no borra datos locales.

## Desinstalar sin perder datos

Ejecutar:

```text
Desinstalar.cmd
```

Esto elimina aplicación y accesos directos. No elimina:

- base SQLite;
- PDFs;
- configuración local;
- cache WSAA.

Para borrar datos locales se requiere un comando explícito documentado en `docs/instalacion-local.md`.
