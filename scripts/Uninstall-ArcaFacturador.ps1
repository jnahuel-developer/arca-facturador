[CmdletBinding()]
param(
    [string] $InstallPath = (Join-Path $env:LOCALAPPDATA "Programs\ArcaFacturador"),
    [switch] $RemoveLocalData,
    [switch] $ConfirmRemoveLocalData
)

$ErrorActionPreference = "Stop"

$startMenuDirectory = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\ARCA Facturador"
$desktopShortcut = Join-Path ([Environment]::GetFolderPath("DesktopDirectory")) "ARCA Facturador.lnk"
$localDataPath = Join-Path $env:LOCALAPPDATA "ArcaFacturador"

function Assert-SafePath {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath -eq [System.IO.Path]::GetPathRoot($fullPath)) {
        throw "La ruta '$Path' no es segura para borrar."
    }
}

Assert-SafePath -Path $InstallPath

if (Test-Path $desktopShortcut) {
    Remove-Item -LiteralPath $desktopShortcut -Force
}

if (Test-Path $startMenuDirectory) {
    Remove-Item -LiteralPath $startMenuDirectory -Recurse -Force
}

if (Test-Path $InstallPath) {
    Remove-Item -LiteralPath $InstallPath -Recurse -Force
}

if ($RemoveLocalData) {
    if (-not $ConfirmRemoveLocalData) {
        throw "Para borrar datos locales, repetí el comando con -RemoveLocalData -ConfirmRemoveLocalData. Esta acción elimina base SQLite, PDFs, configuración local y cache."
    }

    Assert-SafePath -Path $localDataPath
    if (Test-Path $localDataPath) {
        Remove-Item -LiteralPath $localDataPath -Recurse -Force
        Write-Host "Datos locales eliminados: $localDataPath"
    }
}

Write-Host "ARCA Facturador desinstalado. Los datos locales no se borran salvo que uses -RemoveLocalData -ConfirmRemoveLocalData."
