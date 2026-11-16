<#
.SYNOPSIS
    Arranca todo el entorno local: PostgreSQL, API .NET y frontend React.

.DESCRIPTION
    Ejecuta scripts/start-db.ps1 y abre dos ventanas de PowerShell con la API y
    el frontend en marcha. Cada ventana se puede cerrar con Ctrl+C.

.PARAMETER SkipDatabase
    No arranca PostgreSQL (útil si ya está en marcha).

.PARAMETER SkipBrowser
    No abre el navegador al terminar.

.EXAMPLE
    .\scripts\dev.ps1

.EXAMPLE
    .\scripts\dev.ps1 -SkipDatabase
#>
param(
    [switch]$SkipDatabase,
    [switch]$SkipBrowser
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $root ".dotnet\dotnet.exe"
$frontend = Join-Path $root "frontend"

if (-not (Test-Path $dotnet)) {
    throw "No se encontró el SDK local en $dotnet. Ejecuta dotnet-install.ps1 o instala el SDK de .NET 8."
}

if (-not $SkipDatabase) {
    & (Join-Path $PSScriptRoot "start-db.ps1")
}

Write-Host "Abriendo la API en una ventana nueva..." -ForegroundColor Cyan
Start-Process powershell.exe -WorkingDirectory $root -ArgumentList @(
    "-NoExit",
    "-Command",
    "& '$dotnet' run --project backend\src\LocalizadorCafes.Api"
)

Write-Host "Abriendo el frontend en una ventana nueva..." -ForegroundColor Cyan
Start-Process powershell.exe -WorkingDirectory $frontend -ArgumentList @(
    "-NoExit",
    "-Command",
    "npm run dev"
)

Write-Host ""
Write-Host "Web:    http://localhost:5173" -ForegroundColor Green
Write-Host "API:    http://localhost:5067" -ForegroundColor Green
Write-Host "Swagger: http://localhost:5067/swagger" -ForegroundColor Green
Write-Host ""
Write-Host "La API tarda unos segundos en compilar y arrancar." -ForegroundColor DarkGray

if (-not $SkipBrowser) {
    Start-Sleep -Seconds 10
    Start-Process "http://localhost:5173/"
}
