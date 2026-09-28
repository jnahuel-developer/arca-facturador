# Instalación local y empaquetado

`mod012` agrega una distribución local simple para Windows. No usa nube ni instalador complejo: genera una carpeta publicable con la aplicación, scripts operativos y un `.zip` para copiar a la PC objetivo.

## Separación entre aplicación y datos

La instalación separa binarios de datos:

```text
Aplicación instalada:
%LOCALAPPDATA%\Programs\ArcaFacturador

Datos locales:
%LOCALAPPDATA%\ArcaFacturador
```

En la carpeta de datos quedan:

- base SQLite;
- PDFs generados;
- `appsettings.Local.json`;
- cache del Ticket de Acceso WSAA.

Los certificados PFX no se copian ni se respaldan automáticamente si están guardados fuera de esa carpeta. Deben conservarse aparte, por ejemplo en `C:\ARCA\produccion`.

## Generar paquete

Desde la raíz del repositorio:

```powershell
.\scripts\Publish-ArcaFacturador.ps1
```

Resultado esperado:

```text
artifacts\ArcaFacturador-win-x64.zip
artifacts\package\
```

La carpeta `artifacts\package` contiene:

- `ArcaFacturador\` con la app publicada;
- `Instalar.cmd`;
- `Backup.cmd`;
- `Desinstalar.cmd`;
- `Install-ArcaFacturador.ps1`;
- `Uninstall-ArcaFacturador.ps1`;
- `Backup-ArcaFacturadorData.ps1`;
- `LEEME-INSTALACION.md`.

## Instalación limpia

1. Descomprimir `ArcaFacturador-win-x64.zip`.
2. Ejecutar `Instalar.cmd`.

Ese archivo usa PowerShell con política de ejecución temporal `Bypass` sólo para esa instalación. No cambia la política del sistema.

Alternativamente, desde PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-ArcaFacturador.ps1
```

Por defecto instala en:

```text
%LOCALAPPDATA%\Programs\ArcaFacturador
```

También crea accesos directos en:

```text
Menú Inicio\ARCA Facturador
Escritorio
```

## Actualización

Para actualizar:

1. Generar o recibir un paquete nuevo.
2. Descomprimirlo.
3. Ejecutar nuevamente `Instalar.cmd`.

Alternativamente, desde PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-ArcaFacturador.ps1
```

La actualización reemplaza los binarios de la app, pero no borra la carpeta de datos locales. La versión anterior de la app queda movida temporalmente a `%TEMP%`.

## Backup de datos

Antes de actualizar o desinstalar conviene ejecutar:

```text
Backup.cmd
```

Alternativamente, desde PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Backup-ArcaFacturadorData.ps1
```

Por defecto genera un `.zip` en:

```text
Escritorio\Backups ARCA Facturador
```

El backup incluye base SQLite, PDFs, configuración local y cache. No reemplaza el backup manual del certificado PFX si está fuera de `%LOCALAPPDATA%\ArcaFacturador`.

## Desinstalación

Para quitar la aplicación sin borrar datos:

```text
Desinstalar.cmd
```

Alternativamente, desde PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-ArcaFacturador.ps1
```

Esto elimina la carpeta instalada y accesos directos. No elimina base de datos, PDFs ni configuración fiscal.

Para borrar también datos locales hay que pedirlo explícitamente:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-ArcaFacturador.ps1 -RemoveLocalData -ConfirmRemoveLocalData
```

Ese comando elimina:

```text
%LOCALAPPDATA%\ArcaFacturador
```

Usarlo sólo después de confirmar que existe un backup válido.

## Checklist de validación de mod012

1. Ejecutar `dotnet restore`.
2. Ejecutar `dotnet build`.
3. Ejecutar `dotnet test`.
4. Ejecutar `.\scripts\Publish-ArcaFacturador.ps1`.
5. Descomprimir el `.zip` generado.
6. Ejecutar `Instalar.cmd`.
7. Abrir la app desde el acceso directo.
8. Confirmar que la configuración ARCA y comprobantes previos siguen disponibles.
9. Ejecutar `Backup.cmd`.
10. Verificar que el `.zip` de backup contiene base, PDFs y configuración local.
