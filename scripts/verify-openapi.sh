#!/usr/bin/env bash
# Verifies that the committed OpenAPI snapshots in docs/contracts/ match the
# live documents emitted by each microservice. Fails (non-zero) on drift.
#
# Usage:
#   scripts/verify-openapi.sh            # all services
#   scripts/verify-openapi.sh auth       # a single service
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
source "$ROOT/scripts/openapi-lib.sh"

TARGET="${1:-all}"
TMP_DIR="$(mktemp -d)"
FAILURES=0

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
  echo "==> Verifying OpenAPI for '$name' ($project)"

  if [[ ! -f "$ROOT/$snapshot" ]]; then
    echo "    MISSING snapshot: $snapshot (run scripts/export-openapi.sh $name)" >&2
    FAILURES=$((FAILURES + 1))
    continue
  fi

  live="$TMP_DIR/$name.live.json"
  openapi_fetch_live "$ROOT" "$project" "$port" "$live"

  jq 'del(.servers)' "$live" >"$TMP_DIR/$name.live.norm.json"
  jq 'del(.servers)' "$ROOT/$snapshot" >"$TMP_DIR/$name.snap.norm.json"

  if diff -u "$TMP_DIR/$name.snap.norm.json" "$TMP_DIR/$name.live.norm.json"; then
    echo "    OK: snapshot matches '$name'."
  else
    echo "    DRIFT: $snapshot is out of date. Re-export with scripts/export-openapi.sh $name" >&2
    FAILURES=$((FAILURES + 1))
  fi
done < <(openapi_select "$TARGET")

if [[ "$FAILURES" -gt 0 ]]; then
  echo "OpenAPI verification failed for $FAILURES service(s)." >&2
  exit 1
fi

echo "All OpenAPI snapshots are up to date."
