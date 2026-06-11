# DocVault -- Build Docker images and export them for offline deployment
#
# Usage:
#   .\build-and-export.ps1
#   .\build-and-export.ps1 -OutputDir "C:\MyExport"

param(
    [string]$OutputDir = ".\docker-export"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   DocVault -- Docker Build and Export" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Verify docker is available
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker not found. Please install Docker Desktop and ensure it is running."
    exit 1
}

# -- Step 1: Build application images -----------------------------------------
Write-Host ""
Write-Host "[1/5] Building images (docvault-api, docvault-web)..." -ForegroundColor Yellow
docker compose build
if ($LASTEXITCODE -ne 0) { Write-Error "docker compose build failed."; exit 1 }

# -- Step 2: Pull database image -----------------------------------------------
Write-Host ""
Write-Host "[2/5] Pulling postgres:16-alpine from Docker Hub..." -ForegroundColor Yellow
docker pull postgres:16-alpine
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to pull postgres:16-alpine."; exit 1 }

# -- Step 3: Prepare output directory ------------------------------------------
Write-Host ""
Write-Host "[3/5] Preparing output directory: $OutputDir" -ForegroundColor Yellow
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path "$OutputDir\db" | Out-Null

# -- Step 4: Save all images into one tar file ---------------------------------
Write-Host ""
Write-Host "[4/5] Saving all images into docvault-images.tar..." -ForegroundColor Yellow
docker save docvault-api docvault-web postgres:16-alpine -o "$OutputDir\docvault-images.tar"
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to save images."; exit 1 }
Write-Host "  Done."

# -- Step 5: Copy deployment files ---------------------------------------------
Write-Host ""
Write-Host "[5/5] Copying deployment files..." -ForegroundColor Yellow
Copy-Item "docker-compose.deploy.yml" "$OutputDir\docker-compose.yml"
Copy-Item "db\01_schema.sql"          "$OutputDir\db\01_schema.sql"
Copy-Item "deploy\import-and-run.ps1" "$OutputDir\import-and-run.ps1"
Copy-Item "deploy\import-and-run.sh"  "$OutputDir\import-and-run.sh"

# -- Create ZIP ----------------------------------------------------------------
Write-Host ""
Write-Host "Creating docvault-deploy.zip..." -ForegroundColor Yellow
$zipPath = ".\docvault-deploy.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$OutputDir\*" -DestinationPath $zipPath

$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "   Export complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Archive : $zipPath  ($sizeMB MB)"
Write-Host ""
Write-Host "Next steps on the target machine:" -ForegroundColor Cyan
Write-Host "  1. Copy docvault-deploy.zip to the target machine"
Write-Host "  2. Extract the zip"
Write-Host "  3. Windows : .\import-and-run.ps1"
Write-Host "     Linux   : bash import-and-run.sh"
Write-Host ""
Write-Host "  If the target machine uses a network IP (not localhost):"
Write-Host "  Windows : .\import-and-run.ps1 -HostIP 192.168.1.100"
Write-Host "  Linux   : bash import-and-run.sh 192.168.1.100"
Write-Host ""
