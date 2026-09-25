# Persistencia local

## Alcance

La aplicación utiliza una única base SQLite local. No contiene usuarios, roles, stock, auditoría ni estructuras para reportes avanzados.

La base productiva se crea automáticamente al iniciar la aplicación en:

```text
%LOCALAPPDATA%\ArcaFacturador\arca-facturador.db
```

La ruta puede sustituirse al construir `SqliteDatabase`, lo que permite usar archivos temporales aislados durante los tests.

## Tablas

### `settings`

Almacena pares clave-valor para la configuración local. Las claves son únicas y se actualizan mediante una operación de inserción o reemplazo controlado por el repositorio.

### `products`

Almacena las entradas del futuro catálogo de importes frecuentes: código, descripción, unidad e importe unitario. El importe se guarda en centavos enteros para evitar errores de precisión.

### `invoices`

Conserva los datos mínimos necesarios para recuperar un comprobante: número, fechas, importe, estado, CAE, vencimiento del CAE y ruta del PDF. El número de comprobante es único cuando está informado. Los registros pendientes pueden no tener número todavía.

Los estados permitidos son `Pending`, `Authorized` y `Rejected`.

## Inicialización

La creación del directorio, el archivo y las tablas es automática e idempotente. El esquema inicial usa `PRAGMA user_version = 1` como referencia para futuras migraciones.

## Seguridad y respaldo

El archivo `.db` está excluido de Git. La estrategia de respaldo e instalación definitiva se completará en `mod011`.
