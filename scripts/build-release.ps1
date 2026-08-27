<#
.SYNOPSIS
    Compila y empaqueta las versiones oficiales de lanzamiento (Releases) de EchoCut.

.DESCRIPTION
    Genera dos distribuciones de Windows x64:
    1. Standalone (Autonoma): Incluye el runtime de .NET 10 (Single-File, ReadyToRun). No requiere instalacion previa de .NET.
    2. Portable (Ligera): Requiere tener instalado el .NET 10 Desktop Runtime en el sistema.
    
    Adicionalmente comprime los binarios en archivos .zip y calcula sus firmas criptograficas SHA-256.

.PARAMETER Version
    Version SemVer a publicar (por ejemplo, "1.0.0"). Por defecto toma "1.0.0".

.PARAMETER OutputDir
    Carpeta destino donde se guardaran los instaladores y checksums. Por defecto es "./dist".

.PARAMETER Clean
    Si se especifica, limpia la carpeta de salida antes de compilar.

.EXAMPLE
    .\scripts\build-release.ps1 -Version "1.0.0" -Clean
#>

[CmdletBinding()]
param(
    [string]$Version = "1.0.0",
    [string]$OutputDir = "dist",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host " [EchoCut] Preparador de Releases (v$Version)" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

# 1. Validar presencia del SDK de .NET
try {
    $dotnetVersion = dotnet --version
    Write-Host "[OK] .NET SDK detectado: $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Error "No se encontro el comando 'dotnet'. Asegurate de tener instalado el .NET 10 SDK."
    exit 1
}

$rootPath = Resolve-Path "$PSScriptRoot/.."
$projectPath = "$rootPath/EchoCut/EchoCut.csproj"
$resolvedOutputDir = [System.IO.Path]::GetFullPath((Join-Path $rootPath $OutputDir))

if ($Clean -and (Test-Path $resolvedOutputDir)) {
    Write-Host "[INFO] Limpiando directorio de salida previo: $resolvedOutputDir" -ForegroundColor Yellow
    Remove-Item -Recurse -Force $resolvedOutputDir
}

$standaloneTemp = Join-Path $resolvedOutputDir "temp_standalone"
$portableTemp   = Join-Path $resolvedOutputDir "temp_portable"

New-Item -ItemType Directory -Force -Path $standaloneTemp | Out-Null
New-Item -ItemType Directory -Force -Path $portableTemp | Out-Null

# 2. Compilar version Standalone (Autonoma)
Write-Host "`n[1/4] Compilando distribucion Standalone (win-x64, Self-Contained)..." -ForegroundColor Cyan
dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=true `
    -p:Version=$Version `
    -o $standaloneTemp

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error al compilar la distribucion Standalone."
    exit $LASTEXITCODE
}

# 3. Compilar version Portable (Ligera)
Write-Host "`n[2/4] Compilando distribucion Portable (win-x64, Framework-Dependent)..." -ForegroundColor Cyan
dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:Version=$Version `
    -o $portableTemp

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error al compilar la distribucion Portable."
    exit $LASTEXITCODE
}

# 4. Empaquetar en archivos .zip
Write-Host "`n[3/4] Empaquetando en archivos comprimidos (.zip)..." -ForegroundColor Cyan
$standaloneZipName = "EchoCut-v$Version-win-x64-standalone.zip"
$portableZipName   = "EchoCut-v$Version-win-x64-portable.zip"

$standaloneZipPath = Join-Path $resolvedOutputDir $standaloneZipName
$portableZipPath   = Join-Path $resolvedOutputDir $portableZipName

Compress-Archive -Path "$standaloneTemp/*" -DestinationPath $standaloneZipPath -Force
Compress-Archive -Path "$portableTemp/*"   -DestinationPath $portableZipPath -Force

# Limpiar temporales
Remove-Item -Recurse -Force $standaloneTemp
Remove-Item -Recurse -Force $portableTemp

Write-Host "[OK] Creado: $standaloneZipName" -ForegroundColor Green
Write-Host "[OK] Creado: $portableZipName" -ForegroundColor Green

# 5. Generar Checksums SHA-256
Write-Host "`n[4/4] Calculando firmas criptograficas (SHA-256)..." -ForegroundColor Cyan

$hashes = @(
    Get-FileHash -Path $standaloneZipPath -Algorithm SHA256
    Get-FileHash -Path $portableZipPath   -Algorithm SHA256
)

$checksumFile = Join-Path $resolvedOutputDir "SHA256SUMS.txt"
$hashes | ForEach-Object { "$($_.Hash)  $($_.Path | Split-Path -Leaf)" } | Out-File -FilePath $checksumFile -Encoding utf8

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host " [EXITO] Release preparado en $resolvedOutputDir" -ForegroundColor Green
Write-Host "========================================================`n" -ForegroundColor Green

Write-Host "Tabla de verificacion SHA-256 (lista para pegar en GitHub Releases):`n" -ForegroundColor Cyan
Write-Host "| Archivo | Tamano | SHA-256 |"
Write-Host "| :--- | :---: | :--- |"
foreach ($item in $hashes) {
    $fileInfo = Get-Item $item.Path
    $sizeMB = ($fileInfo.Length / 1MB).ToString("N2") + " MB"
    Write-Host "| $($fileInfo.Name) | $sizeMB | $($item.Hash) |"
}
Write-Host ""
