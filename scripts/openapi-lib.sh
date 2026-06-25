#!/usr/bin/env bash
# Shared helpers and service registry for OpenAPI snapshot export/verify.
#
# Each entry: <name>|<api project path>|<export port>|<snapshot path>
# The port is only used transiently while the service runs to emit its document.

OPENAPI_SERVICES=(
  "auth|src/auth/Auth.Api|5199|docs/contracts/auth.openapi.json"
  "identity|src/identity/Identity.Api|5198|docs/contracts/identity.openapi.json"
  "issuer|src/issuer/Issuer.Api|5197|docs/contracts/issuer.openapi.json"
  "verifier|src/verifier/Verifier.Api|5196|docs/contracts/verifier.openapi.json"
)

OPENAPI_SERVER_PID=""

# Prints registry entries matching "all" or a single service name.
openapi_select() {
  local target="$1"
  local found=0
  local entry name
  for entry in "${OPENAPI_SERVICES[@]}"; do
    name="${entry%%|*}"
    if [[ "$target" == "all" || "$target" == "$name" ]]; then
      echo "$entry"
      found=1
    fi
  done
  if [[ "$found" -eq 0 ]]; then
    {
      echo "Unknown service '$target'."
      echo "Known services: $(printf '%s ' "${OPENAPI_SERVICES[@]%%|*}")all"
    } >&2
    return 1
  fi
}

# Runs a service, fetches its live OpenAPI document, then stops it.
# Args: <repo root> <api project> <port> <output json path>
openapi_fetch_live() {
  local root="$1" project="$2" port="$3" out="$4"
  local base_url="http://127.0.0.1:${port}"
  local log
  log="$(mktemp)"

  cd "$root"
  dotnet run --project "$project" --urls "$base_url" >"$log" 2>&1 &
  OPENAPI_SERVER_PID=$!

  local ok=0
  for _ in $(seq 1 90); do
    if curl -sf "${base_url}/openapi/v1.json" -o "$out" 2>/dev/null; then
      ok=1
      break
    fi
    if ! kill -0 "$OPENAPI_SERVER_PID" 2>/dev/null; then
      break
    fi
    sleep 1
  done

  kill "$OPENAPI_SERVER_PID" 2>/dev/null || true
  wait "$OPENAPI_SERVER_PID" 2>/dev/null || true
  OPENAPI_SERVER_PID=""

  if [[ "$ok" -ne 1 ]]; then
    echo "ERROR: '$project' did not expose OpenAPI on ${base_url}/openapi/v1.json in time. Log:" >&2
    cat "$log" >&2
    rm -f "$log"
    return 1
  fi

  rm -f "$log"
}
