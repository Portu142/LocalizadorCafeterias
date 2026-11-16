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
    .\scripts\start-db.ps1

.EXAMPLE
    .\scripts\start-db.ps1 -Distro Ubuntu -Cluster 16

.NOTES
    Funciona con Windows PowerShell 5.1 (powershell.exe) y con PowerShell 7
    (pwsh.exe). Si la política de ejecución bloquea el script:
    powershell -ExecutionPolicy Bypass -File .\scripts\start-db.ps1
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
    # `--exec` ejecuta el binario sin pasar por un shell: con `bash -lc` las
    # comillas se perdían al lanzar el proceso y el keepalive moría al instante.
    Start-Process wsl.exe `
        -ArgumentList @("-d", $Distro, "-u", "root", "--exec", "sleep", "infinity") `
        -WindowStyle Hidden
    Start-Sleep -Seconds 3

    $keepAlive = Get-CimInstance Win32_Process -Filter "Name = 'wsl.exe'" |
        Where-Object { $_.CommandLine -like "*sleep infinity*" }

    if ($keepAlive) {
        Write-Host "Distro mantenida activa en segundo plano." -ForegroundColor DarkGray
    }
    else {
        Write-Warning "No se pudo mantener la distro en segundo plano. Deja abierta una terminal con: wsl -d $Distro"
    }
}

# El clúster puede tardar unos segundos en aceptar conexiones.
$reachable = $false
for ($i = 0; $i -lt 15; $i++) {
    if (Test-NetConnection -ComputerName 127.0.0.1 -Port 5432 -InformationLevel Quiet -WarningAction SilentlyContinue) {
        $reachable = $true
        break
    }
    Start-Sleep -Seconds 1
}

if ($reachable) {
    Write-Host "PostgreSQL disponible en localhost:5432." -ForegroundColor Green
}
else {
    Write-Warning "El puerto 5432 no responde. Revisa el clúster con: wsl -d $Distro -u root -- pg_lsclusters"
    exit 1
}
