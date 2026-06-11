# DocVault -- Import Docker images and start the application (Windows)
#
# Usage (run from the extracted docvault-deploy folder):
#   .\import-and-run.ps1                        # localhost
#   .\import-and-run.ps1 -HostIP 192.168.1.100  # LAN access via IP

param(
    [string]$HostIP = "localhost"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   DocVault -- Deployment" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Verify Docker
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker is not installed or not running. Install Docker Desktop first."
    exit 1
}

# -- Step 1: Load images -------------------------------------------------------
Write-Host ""
Write-Host "[1/3] Loading Docker images from docvault-images.tar..." -ForegroundColor Yellow
docker load -i docvault-images.tar
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to load images."; exit 1 }

# -- Step 2: Configure host IP for CORS ----------------------------------------
Write-Host ""
Write-Host "[2/3] Configuring for host: $HostIP" -ForegroundColor Yellow

if ($HostIP -ne "localhost") {
    (Get-Content docker-compose.yml) `
        -replace 'Cors__Origins__0=http://localhost:3000', "Cors__Origins__0=http://${HostIP}:3000" |
        Set-Content docker-compose.yml
    Write-Host "  CORS updated to http://${HostIP}:3000"
} else {
    Write-Host "  Using localhost (default)"
}

# -- Step 3: Start the application ---------------------------------------------
Write-Host ""
Write-Host "[3/3] Starting DocVault..." -ForegroundColor Yellow
docker compose up -d
if ($LASTEXITCODE -ne 0) { Write-Error "docker compose up failed."; exit 1 }

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "   DocVault is running!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Frontend  : http://${HostIP}:3000"
Write-Host "  API       : http://${HostIP}:5080/swagger"
Write-Host ""
Write-Host "  Default admin account:"
Write-Host "    Email   : admin@docvault.local"
Write-Host "    Password: Admin@12345"
Write-Host ""
Write-Host "  Commands:"
Write-Host "    Stop app           : docker compose down"
Write-Host "    Stop + delete data : docker compose down -v"
Write-Host "    View logs          : docker compose logs -f"
Write-Host ""
