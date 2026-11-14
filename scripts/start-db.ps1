<#
.SYNOPSIS
    Arranca PostgreSQL + PostGIS en WSL y mantiene la distro viva.

.DESCRIPTION
    WSL apaga la distribución cuando se cierra la última sesión, y con ella
    PostgreSQL. Este script arranca el clúster y deja un proceso en segundo
    plano que mantiene la distro activa mientras el equipo esté encendido.

.PARAMETER Distro
    Nombre de la distribución WSL. Por defecto, Ubuntu-24.04.

.PARAMETER Cluster
    Versión del clúster de PostgreSQL. Por defecto, 16.

.EXAMPLE
    pwsh -File scripts/start-db.ps1

.EXAMPLE
    pwsh -File scripts/start-db.ps1 -Distro Ubuntu -Cluster 16
#>
param(
    [string]$Distro = "Ubuntu-24.04",
    [string]$Cluster = "16"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command wsl.exe -ErrorAction SilentlyContinue)) {
    throw "WSL no está instalado. Instálalo con 'wsl --install' o usa Docker/PostgreSQL nativo."
}

Write-Host "Arrancando PostgreSQL $Cluster en WSL ($Distro)..." -ForegroundColor Cyan

wsl -d $Distro -u root -- bash -lc "pg_ctlcluster $Cluster main start 2>/dev/null; pg_lsclusters" | Out-Host

# WSL termina la distro al cerrar la última sesión: un proceso `sleep` la
# mantiene encendida y con ella el servidor de base de datos.
$keepAlive = Get-CimInstance Win32_Process -Filter "Name = 'wsl.exe'" |
    Where-Object { $_.CommandLine -like "*sleep infinity*" }

if (-not $keepAlive) {
    Start-Process wsl.exe `
        -ArgumentList "-d", $Distro, "-u", "root", "--", "bash", "-lc", "exec sleep infinity" `
        -WindowStyle Hidden
    Write-Host "Distro mantenida activa en segundo plano." -ForegroundColor DarkGray
}

$reachable = Test-NetConnection -ComputerName 127.0.0.1 -Port 5432 -InformationLevel Quiet

if ($reachable) {
    Write-Host "PostgreSQL disponible en localhost:5432." -ForegroundColor Green
}
else {
    Write-Warning "El puerto 5432 no responde. Revisa el clúster con: wsl -d $Distro -u root -- pg_lsclusters"
    exit 1
}
