[CmdletBinding()]
param(
    [string] $InstallPath = (Join-Path $env:LOCALAPPDATA "Programs\ArcaFacturador"),
    [bool] $CreateDesktopShortcut = $true
)

$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $scriptDirectory "ArcaFacturador"
$sourceExecutable = Join-Path $sourcePath "ArcaFacturador.exe"
$startMenuDirectory = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\ARCA Facturador"
$startMenuShortcut = Join-Path $startMenuDirectory "ARCA Facturador.lnk"
$desktopShortcut = Join-Path ([Environment]::GetFolderPath("DesktopDirectory")) "ARCA Facturador.lnk"

function Assert-SafeInstallPath {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $localAppData = [System.IO.Path]::GetFullPath($env:LOCALAPPDATA)
    $programFiles = [System.IO.Path]::GetFullPath($env:ProgramFiles)

    if ($fullPath -eq [System.IO.Path]::GetPathRoot($fullPath)) {
        throw "La ruta de instalación no puede ser la raíz de una unidad."
    }

    if (-not ($fullPath.StartsWith($localAppData, [System.StringComparison]::OrdinalIgnoreCase) -or
            $fullPath.StartsWith($programFiles, [System.StringComparison]::OrdinalIgnoreCase))) {
        throw "Usá una ruta dentro de LocalAppData o Program Files para instalar la aplicación."
    }
}

function New-Shortcut {
    param(
        [Parameter(Mandatory = $true)] [string] $ShortcutPath,
        [Parameter(Mandatory = $true)] [string] $TargetPath,
        [Parameter(Mandatory = $true)] [string] $WorkingDirectory
    )

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.IconLocation = $TargetPath
    $shortcut.Save()
}

if (-not (Test-Path $sourceExecutable)) {
    throw "No se encontró '$sourceExecutable'. Ejecutá este script desde la carpeta descomprimida del paquete."
}

Assert-SafeInstallPath -Path $InstallPath

$resolvedInstallPath = [System.IO.Path]::GetFullPath($InstallPath)
$backupPath = $null
if (Test-Path $resolvedInstallPath) {
    $backupPath = Join-Path $env:TEMP ("ArcaFacturador-app-backup-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
    Move-Item -LiteralPath $resolvedInstallPath -Destination $backupPath
}

New-Item -ItemType Directory -Path $resolvedInstallPath | Out-Null
Copy-Item -Path (Join-Path $sourcePath "*") -Destination $resolvedInstallPath -Recurse -Force

$installedExecutable = Join-Path $resolvedInstallPath "ArcaFacturador.exe"
New-Item -ItemType Directory -Path $startMenuDirectory -Force | Out-Null
New-Shortcut -ShortcutPath $startMenuShortcut -TargetPath $installedExecutable -WorkingDirectory $resolvedInstallPath

if ($CreateDesktopShortcut) {
    New-Shortcut -ShortcutPath $desktopShortcut -TargetPath $installedExecutable -WorkingDirectory $resolvedInstallPath
}

Write-Host "ARCA Facturador instalado en: $resolvedInstallPath"
Write-Host "Los datos locales permanecen en: $(Join-Path $env:LOCALAPPDATA 'ArcaFacturador')"
if ($backupPath) {
    Write-Host "La versión anterior de la app quedó respaldada temporalmente en: $backupPath"
}
