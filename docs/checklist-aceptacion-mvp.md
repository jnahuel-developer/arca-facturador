# Checklist de aceptación MVP

## Estado funcional validado

- [x] Emisión real de Factura C desde la GUI.
- [x] Validación posterior de comprobantes reales.
- [x] Generación y apertura correcta de PDFs emitidos desde la aplicación.
- [x] Consulta local de comprobantes emitidos.
- [x] Instalación local validada.

## Validación técnica obligatoria

```powershell
dotnet restore
dotnet build
dotnet test
.\scripts\Publish-ArcaFacturador.ps1
```

Resultado esperado:

- compilación sin errores;
- tests completos exitosos;
- paquete `artifacts\ArcaFacturador-win-x64.zip` generado;
- ZIP con `Instalar.cmd`, `Backup.cmd`, `Desinstalar.cmd` y `ArcaFacturador.exe`.

## Validación manual antes de pasar a main

- [ ] Instalar desde ZIP usando `Instalar.cmd`.
- [ ] Abrir desde acceso directo.
- [ ] Probar conexión ARCA en producción.
- [ ] Confirmar que se ve la configuración local.
- [ ] Confirmar que se listan comprobantes previos.
- [ ] Abrir PDF de un comprobante emitido.
- [ ] Ejecutar `Backup.cmd`.
- [ ] Confirmar que el backup contiene base, PDFs y configuración local.

## Cierre sugerido

Después de integrar `mod013` en `develop`:

1. ejecutar CI;
2. integrar `develop` en `main`;
3. crear tag `v1.0.0`.
