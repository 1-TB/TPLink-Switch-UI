#!/usr/bin/env bash
set -euo pipefail

# ───────────────────────────────────────────────────────────────────────────────
# update-and-push.sh
#
# Builds & pushes both backend and frontend images to Docker Hub (multi-arch).
# Usage: ./docker-update-and-push.sh
# ───────────────────────────────────────────────────────────────────────────────

# Change these if you ever rename things
DOCKER_USER="unknownplatform"
BACKEND_NAME="tplink-webui-backend"
FRONTEND_NAME="tplink-webui-frontend"
PLATFORMS="linux/amd64,linux/arm64"

# 1) Ensure buildx builder exists & is selected
if ! docker buildx inspect mybuilder &> /dev/null; then
  echo "➤ Creating buildx builder “mybuilder”…"
  docker buildx create --name mybuilder --driver docker-container --use
else
  docker buildx use mybuilder
fi

# 2) Login to Docker Hub (will prompt once)
echo "➤ Logging into Docker Hub as \$DOCKER_USER…"
docker login

# 3) Build & push backend
echo "➤ Building & pushing backend (${PLATFORMS})…"
docker buildx build \
  --platform ${PLATFORMS} \
  -t ${DOCKER_USER}/${BACKEND_NAME}:latest \
  --push \
  -f backend/Dockerfile \
  ./backend

# 4) Build & push frontend
echo "➤ Building & pushing frontend (${PLATFORMS})…"
docker buildx build \
  --platform ${PLATFORMS} \
  -t ${DOCKER_USER}/${FRONTEND_NAME}:latest \
  --push \
  -f frontend/Dockerfile \
  ./frontend

echo "✅ All done! Images are now up-to-date under ${DOCKER_USER}/<{backend,frontend}>:latest"

