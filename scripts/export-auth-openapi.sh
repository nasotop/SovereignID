#!/usr/bin/env bash
# Exports Auth.Api OpenAPI to docs/contracts/auth.openapi.json (servers stripped).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PORT="${AUTH_OPENAPI_PORT:-5199}"
BASE_URL="http://127.0.0.1:${PORT}"
OUTPUT="${ROOT}/docs/contracts/auth.openapi.json"
TMP_DIR="$(mktemp -d)"

cleanup() {
  if [[ -n "${SERVER_PID:-}" ]] && kill -0 "$SERVER_PID" 2>/dev/null; then
    kill "$SERVER_PID" 2>/dev/null || true
    wait "$SERVER_PID" 2>/dev/null || true
  fi
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

cd "$ROOT"
dotnet run --project src/auth/Auth.Api --urls "$BASE_URL" >"$TMP_DIR/auth.log" 2>&1 &
SERVER_PID=$!

for _ in $(seq 1 60); do
  if curl -sf "${BASE_URL}/openapi/v1.json" -o "$TMP_DIR/live.json" 2>/dev/null; then
    break
  fi
  sleep 1
done

if [[ ! -f "$TMP_DIR/live.json" ]]; then
  echo "Auth.Api did not expose OpenAPI in time. Log:" >&2
  cat "$TMP_DIR/auth.log" >&2
  exit 1
fi

mkdir -p "$(dirname "$OUTPUT")"
jq 'del(.servers)' "$TMP_DIR/live.json" >"$OUTPUT"
echo "Wrote $OUTPUT"
