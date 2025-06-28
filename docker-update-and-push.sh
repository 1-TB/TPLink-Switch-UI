#!/usr/bin/env bash
set -euo pipefail

# ───────────────────────────────────────────────────────────────────────────────
# update-and-push.sh
#
# Builds & pushes both backend and frontend images to Docker Hub (multi-arch),
# using a local cache so subsequent "dotnet restore" and "npm ci" are instant.
# ───────────────────────────────────────────────────────────────────────────────

DOCKER_USER="unknownplatform"
BACKEND_NAME="tplink-webui-backend"
FRONTEND_NAME="tplink-webui-frontend"
PLATFORMS="linux/amd64,linux/arm64"
BUILDER="tplink-builder"
CACHE_DIR=".buildx-cache"

# 1) Create/use a persistent builder
if ! docker buildx inspect "${BUILDER}" &> /dev/null; then
  echo "➤ Creating buildx builder “${BUILDER}”…"
  docker buildx create --name "${BUILDER}" --driver docker-container --use
else
  docker buildx use "${BUILDER}"
fi

# 2) Ensure cache directories exist
mkdir -p "${CACHE_DIR}/${BACKEND_NAME}" "${CACHE_DIR}/${FRONTEND_NAME}"

# 3) Log in to Docker Hub
echo "➤ docker login (ENTER credentials for ${DOCKER_USER})…"
docker login

# 4) Build & push backend with cache
echo "➤ Building & pushing ${BACKEND_NAME} for ${PLATFORMS}…"
docker buildx build \
  --platform "${PLATFORMS}" \
  --cache-from=type=local,src="${CACHE_DIR}/${BACKEND_NAME}" \
  --cache-to=type=local,dest="${CACHE_DIR}/${BACKEND_NAME}",mode=max \
  -f backend/Dockerfile \
  -t "${DOCKER_USER}/${BACKEND_NAME}:latest" \
  --push \
  backend/

# 5) Build & push frontend with cache
echo "➤ Building & pushing ${FRONTEND_NAME} for ${PLATFORMS}…"
docker buildx build \
  --platform "${PLATFORMS}" \
  --cache-from=type=local,src="${CACHE_DIR}/${FRONTEND_NAME}" \
  --cache-to=type=local,dest="${CACHE_DIR}/${FRONTEND_NAME}",mode=max \
  -f frontend/Dockerfile \
  -t "${DOCKER_USER}/${FRONTEND_NAME}:latest" \
  --push \
  frontend/

echo "✅ All done! Your multi-arch images are up-to-date and caches are warmed."
