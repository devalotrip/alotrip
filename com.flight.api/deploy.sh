#!/usr/bin/env bash
set -euo pipefail

REMOTE_USER="alotrip"
REMOTE_HOST="alotrip-pc"
REMOTE_DIR="~/com.flight.api"

echo "==> Syncing project to ${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_DIR} ..."
ssh "${REMOTE_USER}@${REMOTE_HOST}" "mkdir -p ${REMOTE_DIR}"
rsync -avz --delete \
  --exclude='.git' \
  --exclude='src/*/bin' \
  --exclude='src/*/obj' \
  . "${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_DIR}/"

echo "==> Running docker compose on remote ..."
ssh "${REMOTE_USER}@${REMOTE_HOST}" \
  "cd ${REMOTE_DIR} && docker compose -f docker-compose.yml up -d --build"

echo "==> Done."
