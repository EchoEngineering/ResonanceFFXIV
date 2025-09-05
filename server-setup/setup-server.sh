#!/bin/bash

# Resonance PDS Server Setup Script
# Run this on the Ubuntu 22.04 droplet at 134.199.137.25

set -e

echo "🚀 Setting up Resonance PDS Server..."

# Update system
echo "📦 Updating system packages..."
apt update && apt upgrade -y

# Install Docker
echo "🐳 Installing Docker..."
if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    usermod -aG docker $USER
    rm get-docker.sh
fi

# Install Docker Compose
echo "📋 Installing Docker Compose..."
if ! command -v docker-compose &> /dev/null; then
    apt install docker-compose-plugin -y
fi

# Install useful tools
echo "🛠️ Installing additional tools..."
apt install -y curl wget htop nano ufw git

# Configure firewall
echo "🔥 Configuring firewall..."
ufw --force enable
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp

# Create PDS directory
echo "📁 Creating PDS directory..."
mkdir -p /opt/resonance-pds
cd /opt/resonance-pds

# Create docker-compose.yml (will be uploaded separately)
echo "⚙️ PDS directory created at /opt/resonance-pds"
echo "📋 Next steps:"
echo "  1. Upload docker-compose.yml, Caddyfile, .env, and init-db.sql to /opt/resonance-pds"
echo "  2. Run: docker compose up -d"
echo "  3. Check logs: docker compose logs -f"
echo ""
echo "✅ Server setup complete!"

# Show system info
echo "📊 System Information:"
echo "Hostname: $(hostname)"
echo "IP Address: $(curl -s ifconfig.me)"
echo "Docker Version: $(docker --version)"
echo "Docker Compose Version: $(docker compose version)"

echo "🔗 Your PDS will be available at: https://sync.terasync.app"