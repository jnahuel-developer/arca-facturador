[CmdletBinding()]
param(
    [string] $DestinationDirectory = (Join-Path ([Environment]::GetFolderPath("DesktopDirectory")) "Backups ARCA Facturador")
)

$ErrorActionPreference = "Stop"

$localDataPath = Join-Path $env:LOCALAPPDATA "ArcaFacturador"
if (-not (Test-Path $localDataPath)) {
    throw "No se encontró la carpeta de datos locales: $localDataPath"
}

New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupPath = Join-Path $DestinationDirectory "ArcaFacturador-datos-$timestamp.zip"

Compress-Archive -Path (Join-Path $localDataPath "*") -DestinationPath $backupPath -Force

Write-Host "Backup generado: $backupPath"
Write-Host "Incluye base SQLite, PDFs, configuración local y cache. No incluye certificados PFX si están guardados fuera de la carpeta de datos."
