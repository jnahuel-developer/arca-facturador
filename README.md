# ARCA Facturador

Aplicación Windows local para automatizar la emisión de Facturas C de un único negocio mediante los servicios de ARCA. El MVP está pensado para una sola PC, un único usuario, CUIT y punto de venta.

La base técnica incluye la solución, una aplicación WPF mínima, tests automatizados, integración continua y documentación. `mod002` agrega las reglas de dominio del comprobante: configuración fiscal validada, datos fijos y fechas del período de servicio. Todavía no incluye persistencia, conexión con ARCA, generación de PDF ni una pantalla funcional de facturación.

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
```

## Documentación

- [Alcance del MVP](docs/alcance-mvp.md)
- [Plan de ramas](docs/plan-mods.md)
- [Configuración ARCA](docs/configuracion-arca.md)
