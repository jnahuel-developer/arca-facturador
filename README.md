# ARCA Facturador

Aplicación Windows local para automatizar la emisión de Facturas C de un único negocio mediante los servicios de ARCA. El MVP está pensado para una sola PC, un único usuario, CUIT y punto de venta.

La base técnica incluye la solución, una aplicación WPF, tests automatizados, integración continua y documentación. Las reglas de dominio definen la configuración fiscal, los datos fijos y las fechas del período de servicio. `mod003` incorpora persistencia SQLite mínima para configuración, importes frecuentes y comprobantes. `mod004` suma una pantalla local para cargar importes, validar datos y guardar emisiones simuladas. `mod005` agrega un catálogo simple de importes frecuentes. `mod006` genera un PDF local del comprobante con campos pendientes de autorización. `mod007` prepara la autenticación WSAA en homologación. `mod008` prepara la emisión WSFEv1 de Factura C en homologación. `mod009` agrega cache persistente del TA y reconciliación ante errores críticos. `mod010` prepara producción con validaciones de ambiente, certificado y resguardos locales. `mod011` conecta la configuración ARCA y la emisión electrónica real desde la GUI. `mod012` agrega publicación, instalación local, actualización, backup y desinstalación segura.

## Stack

- C# y WPF.
- .NET 9 como decisión transitoria, porque es el SDK disponible en el equipo al iniciar `mod001`.
- xUnit para tests automatizados.
- GitHub Actions sobre Windows.
- SQLite a partir de `mod003`.

La versión objetivo acordada es .NET 10 LTS. La migración se realizará cuando el SDK esté disponible en el entorno local y en CI; hasta entonces, `global.json` y el workflow fijan .NET 9 para garantizar compilaciones reproducibles.

## Estructura

```text
src/ArcaFacturador/          Aplicación WPF
tests/ArcaFacturador.Tests/ Tests automatizados
docs/                       Alcance y planificación
.github/workflows/          Integración continua
```

## Ramas

- `main`: estado productivo; sólo recibe integraciones aprobadas desde `develop`.
- `develop`: desarrollo integrado, probado y estable.
- `modxxx`: cambio acotado, creado desde `develop`, validado mediante tests e integrado luego mediante PR/MR. La numeración es secuencial desde `mod001` y la rama se elimina después de integrarse.

La planificación completa está en [docs/plan-mods.md](docs/plan-mods.md).

## Requisitos

- Windows 10 o posterior.
- SDK .NET 9 compatible con `global.json`.

## Comandos básicos

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/ArcaFacturador
.\scripts\Publish-ArcaFacturador.ps1
```

## Documentación

- [Alcance del MVP](docs/alcance-mvp.md)
- [Plan de ramas](docs/plan-mods.md)
- [Configuración ARCA](docs/configuracion-arca.md)
- [Persistencia local](docs/persistencia.md)
- [Facturación local simulada](docs/facturacion-local.md)
- [Catálogo de importes frecuentes](docs/catalogo-precios.md)
- [PDF local del comprobante](docs/pdf-local.md)
- [WSAA en homologación](docs/wsaa-homologacion.md)
- [WSFEv1 en homologación](docs/wsfev1-homologacion.md)
- [Reconciliación y errores críticos](docs/reconciliacion-errores.md)
- [Producción y seguridad local](docs/produccion-seguridad.md)
- [Emisión real desde la GUI](docs/emision-real-gui.md)
- [Instalación local y empaquetado](docs/instalacion-local.md)
