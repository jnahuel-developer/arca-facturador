[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [string] $Runtime = "win-x64",
    [bool] $SelfContained = $true
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\ArcaFacturador\ArcaFacturador.csproj"
$artifactsPath = Join-Path $repoRoot "artifacts"
$publishPath = Join-Path $artifactsPath "publish\ArcaFacturador"
$packagePath = Join-Path $artifactsPath "package"
$packageAppPath = Join-Path $packagePath "ArcaFacturador"
$zipPath = Join-Path $artifactsPath "ArcaFacturador-$Runtime.zip"

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Parent,
        [Parameter(Mandatory = $true)] [string] $Child
    )

    $parentFullPath = [System.IO.Path]::GetFullPath($Parent)
    $childFullPath = [System.IO.Path]::GetFullPath($Child)
    if (-not $childFullPath.StartsWith($parentFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "La ruta '$Child' no está dentro de '$Parent'."
    }
}

Assert-ChildPath -Parent $repoRoot -Child $artifactsPath
Assert-ChildPath -Parent $artifactsPath -Child $publishPath
Assert-ChildPath -Parent $artifactsPath -Child $packagePath

if (Test-Path $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}

if (Test-Path $packagePath) {
    Remove-Item -LiteralPath $packagePath -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $publishPath | Out-Null
New-Item -ItemType Directory -Path $packageAppPath | Out-Null

$selfContainedValue = if ($SelfContained) { "true" } else { "false" }

dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained $selfContainedValue `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    --output $publishPath

Get-ChildItem -Path $publishPath -File | Where-Object { $_.Extension -ne ".pdb" } | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $packageAppPath -Force
}
Copy-Item -Path (Join-Path $PSScriptRoot "Install-ArcaFacturador.ps1") -Destination $packagePath -Force
Copy-Item -Path (Join-Path $PSScriptRoot "Uninstall-ArcaFacturador.ps1") -Destination $packagePath -Force
Copy-Item -Path (Join-Path $PSScriptRoot "Backup-ArcaFacturadorData.ps1") -Destination $packagePath -Force
Copy-Item -Path (Join-Path $repoRoot "docs\instalacion-local.md") -Destination (Join-Path $packagePath "LEEME-INSTALACION.md") -Force

Compress-Archive -Path (Join-Path $packagePath "*") -DestinationPath $zipPath -Force

Write-Host "Paquete generado: $zipPath"
Write-Host "Carpeta publicable: $packagePath"
