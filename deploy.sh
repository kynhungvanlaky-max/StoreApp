#!/bin/bash
set -e

echo "🚀 NexusKeys Game Store Deploy Script"
echo "======================================"

# Variables
PROJECT_DIR="/opt/gamestore"
BACKUP_DIR="/opt/gamestore-backups/$(date +%Y%m%d_%H%M%S)"
COMPOSE_FILE="docker-compose.yml"
GIT_REPO="https://github.com/kynhungvanlaky-max/StoreApp.git"

# Step 1: Backup current deployment
echo "📦 Creating backup..."
mkdir -p "$BACKUP_DIR"
if [ -f "$PROJECT_DIR/docker-compose.yml" ]; then
    cp "$PROJECT_DIR/docker-compose.yml" "$BACKUP_DIR/"
fi

# Step 2: Pull latest code
echo "📥 Pulling latest code..."
cd "$PROJECT_DIR" || {
    echo "First-time deploy: cloning repository..."
    git clone "$GIT_REPO" "$PROJECT_DIR"
    cd "$PROJECT_DIR"
}

git pull origin main

# Step 3: Create .env if not exists
if [ ! -f ".env" ]; then
    echo "🔧 Creating .env file..."
    cat > .env << EOF
DOCKER_USERNAME=nexus
EOF
    echo "⚠️  Please edit .env file with your actual values if needed!"
fi

# Step 4: Build and start containers
echo "🏗️  Building and starting containers..."
docker compose -f "$COMPOSE_FILE" build --no-cache
docker compose -f "$COMPOSE_FILE" up -d

# Step 5: Wait for health checks
echo "⏳ Waiting for services to be healthy..."
sleep 15

# Step 6: Verify deployment
echo "✅ Verifying deployment..."
if curl -sf http://localhost:5021/ > /dev/null 2>&1; then
    echo "✅ API is running!"
else
    echo "❌ API health check failed!"
    docker compose logs nexuskeys-api --tail=20
    exit 1
fi

if curl -sf http://localhost:5205/ > /dev/null 2>&1; then
    echo "✅ Frontend is running!"
else
    echo "❌ Frontend health check failed!"
    exit 1
fi

# Step 7: SSL certificate renewal reminder
echo "🔒 Don't forget to set up SSL with:"
echo "   sudo certbot --nginx -d nexusstore.com -d www.nexusstore.com"

# Step 8: Cleanup old images
echo "🧹 Cleaning up old Docker images..."
docker image prune -f

echo ""
echo "🎉 Deployment completed successfully!"
echo "   API:      http://localhost:5021"
echo "   Frontend: http://localhost:5205"
echo "   Admin:    http://localhost:3001"
echo "   Nginx:    http://localhost:80"
