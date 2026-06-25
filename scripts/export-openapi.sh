#!/usr/bin/env bash
# Exports the OpenAPI document of one or all microservices to docs/contracts/
# (the runtime "servers" block is stripped so snapshots are host-independent).
#
# Usage:
#   scripts/export-openapi.sh            # all services
#   scripts/export-openapi.sh auth       # a single service
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
source "$ROOT/scripts/openapi-lib.sh"

TARGET="${1:-all}"
TMP_DIR="$(mktemp -d)"

cleanup() {
  if [[ -n "${OPENAPI_SERVER_PID:-}" ]] && kill -0 "$OPENAPI_SERVER_PID" 2>/dev/null; then
    kill "$OPENAPI_SERVER_PID" 2>/dev/null || true
    wait "$OPENAPI_SERVER_PID" 2>/dev/null || true
  fi
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

while IFS='|' read -r name project port snapshot; do
  [[ -z "$name" ]] && continue
  echo "==> Exporting OpenAPI for '$name' ($project)"
  live="$TMP_DIR/$name.live.json"
  openapi_fetch_live "$ROOT" "$project" "$port" "$live"

  output="$ROOT/$snapshot"
  mkdir -p "$(dirname "$output")"
  jq 'del(.servers)' "$live" >"$output"
  echo "    Wrote $snapshot"
done < <(openapi_select "$TARGET")

echo "Done."
