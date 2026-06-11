#!/usr/bin/env bash
# DocVault — Import Docker images and start the application (Linux / macOS)
#
# Usage (run from the extracted docvault-deploy folder):
#   bash import-and-run.sh                    # localhost
#   bash import-and-run.sh 192.168.1.100      # LAN access via IP

set -e

HOST_IP="${1:-localhost}"

echo ""
echo "================================================"
echo "   DocVault — Deployment"
echo "================================================"

# Verify Docker
if ! command -v docker &>/dev/null; then
    echo "ERROR: Docker is not installed."
    echo "  Install: https://docs.docker.com/engine/install/"
    exit 1
fi

# ── Step 1: Load images ───────────────────────────────────────────────────────
echo ""
echo "[1/3] Loading Docker images from docvault-images.tar..."
docker load -i docvault-images.tar

# ── Step 2: Configure host IP for CORS ───────────────────────────────────────
echo ""
echo "[2/3] Configuring for host: $HOST_IP"

if [ "$HOST_IP" != "localhost" ]; then
    # BSD sed (macOS) needs an empty-string argument after -i; GNU sed (Linux) does not
    if sed --version 2>/dev/null | grep -q GNU; then
        sed -i "s|Cors__Origins__0=http://localhost:3000|Cors__Origins__0=http://${HOST_IP}:3000|g" docker-compose.yml
    else
        sed -i '' "s|Cors__Origins__0=http://localhost:3000|Cors__Origins__0=http://${HOST_IP}:3000|g" docker-compose.yml
    fi
    echo "  CORS updated to http://${HOST_IP}:3000"
else
    echo "  Using localhost (default)"
fi

# ── Step 3: Start the application ────────────────────────────────────────────
echo ""
echo "[3/3] Starting DocVault..."
docker compose up -d

echo ""
echo "================================================"
echo "   DocVault is running!"
echo "================================================"
echo ""
echo "  Frontend  : http://${HOST_IP}:3000"
echo "  API       : http://${HOST_IP}:5080/swagger"
echo ""
echo "  Default admin account:"
echo "    Email   : admin@docvault.local"
echo "    Password: Admin@12345"
echo ""
echo "  Commands:"
echo "    Stop app            : docker compose down"
echo "    Stop + delete data  : docker compose down -v"
echo "    View logs           : docker compose logs -f"
echo ""
