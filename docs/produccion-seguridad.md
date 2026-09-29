# Producción y seguridad local

## Estado de la mod010

La aplicación queda preparada para distinguir inequívocamente homologación de producción, validar la configuración local antes de operar y evitar emisiones productivas accidentales.

Esta rama no incorpora credenciales reales ni ejecuta emisiones productivas.

## Ambientes

La configuración local acepta dos ambientes:

- `Homologacion`
- `Produccion`

Para evitar cruces peligrosos, las URLs configuradas deben corresponder al ambiente seleccionado.

| Ambiente | WSAA | WSFEv1 |
| --- | --- | --- |
| Homologación | `https://wsaahomo.afip.gov.ar/ws/services/LoginCms` | `https://wswhomo.afip.gov.ar/wsfev1/service.asmx` |
| Producción | `https://wsaa.afip.gov.ar/ws/services/LoginCms` | `https://servicios1.afip.gov.ar/wsfev1/service.asmx` |

Las URLs de producción se basan en la documentación pública de ARCA/AFIP para WSAA y WSFEv1.

## Confirmación obligatoria para producción

Si `Environment` es `Produccion`, la app exige ambas condiciones:

```json
{
  "AllowProduction": true,
  "ProductionConfirmation": "CONFIRMO_USO_PRODUCCION"
}
```

Esto es deliberado. La producción no debe quedar habilitada por error al copiar un archivo de configuración.

## Certificado

Antes de llamar a ARCA, la app valida localmente que el certificado:

- exista en la ruta configurada;
- contenga clave privada;
- esté vigente;
- no esté vencido.

Homologación y producción usan certificados/autorizaciones distintos. No conviene reutilizar rutas ni nombres entre ambientes.

## Archivos sensibles

No deben versionarse:

- `appsettings.Local.json`;
- archivos `.pfx`, `.p12`, `.key`, `.pem`, `.crt`, `.csr`;
- tickets WSAA cacheados;
- contraseñas de certificados;
- datos reales del contribuyente si no son necesarios para documentación.

El `.gitignore` ya contempla estos casos.

## Configuración local sugerida

Para producción se puede usar directamente la sección `Configuración ARCA` de la pantalla principal.

Como alternativa manual, partir de:

```text
appsettings.production.example.json
```

copiarlo localmente como:

```text
appsettings.Local.json
```

y completar sólo en la PC operativa los datos reales:

- CUIT emisor;
- punto de venta productivo de tipo Web Services;
- ruta del PFX productivo;
- contraseña local del PFX.

## Prueba segura antes de emitir

Antes de emitir una factura real, realizar una prueba controlada que sólo consulte:

- obtención de TA por WSAA;
- `FECompUltimoAutorizado` por WSFEv1.

Esa consulta no emite comprobantes. La primera emisión productiva debe hacerse luego con un importe controlado y verificarse en ARCA.

Desde `mod011`, esta prueba se ejecuta desde la pantalla principal con el botón `Probar conexión ARCA`.

La emisión productiva real se ejecuta con `Emitir factura electrónica` y siempre muestra una confirmación previa con ambiente, CUIT, punto de venta e importe.
