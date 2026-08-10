#!/usr/bin/env bash
# Fail closed if a production values file still contains placeholders.
# Usage: ./validate-prod-values.sh path/to/values-production.local.yaml
set -euo pipefail

FILE="${1:-}"
if [[ -z "${FILE}" || ! -f "${FILE}" ]]; then
  echo "Usage: $0 <values-production.local.yaml>" >&2
  exit 2
fi

CONTENT="$(cat "${FILE}")"
FAILED=0

fail() {
  echo "FAIL: $1" >&2
  FAILED=1
}

warn() {
  echo "WARN: $1" >&2
}

# Placeholder tokens that must never ship to a real cluster.
PATTERNS=(
  'REQUIRED_'
  'REPLACE_ME'
  'replace-with'
  'change-me'
  'REQUIRED_PG'
  'REQUIRED_GHCR'
  'REQUIRED_JWT'
  'REQUIRED_INTERNAL'
  'REQUIRED_RABBIT'
  'REQUIRED_REDIS'
  'REQUIRED_MONGO'
  'REQUIRED_IMAGE'
)

for pattern in "${PATTERNS[@]}"; do
  if grep -Fqi -- "${pattern}" "${FILE}"; then
    fail "placeholder pattern still present: ${pattern}"
  fi
done

# jwtSigningKey length (best-effort YAML scrape)
KEY_LINE="$(grep -E '^\s*jwtSigningKey:' "${FILE}" | head -n1 || true)"
if [[ -n "${KEY_LINE}" ]]; then
  KEY_VAL="$(echo "${KEY_LINE}" | sed -E 's/^[^:]+:[[:space:]]*//; s/^["'\'']//; s/["'\'']$//')"
  if [[ ${#KEY_VAL} -lt 32 ]]; then
    fail "secrets.jwtSigningKey must be at least 32 characters (got ${#KEY_VAL})"
  fi
else
  fail "secrets.jwtSigningKey not found"
fi

# Discourage floating tags
if grep -Eiq '^\s*tag:[[:space:]]*["'\'']?latest["'\'']?[[:space:]]*$' "${FILE}"; then
  fail 'image.tag must not be "latest" for production'
fi

# Production posture hints
if ! grep -Eiq '^\s*aspnetcoreEnvironment:[[:space:]]*Production' "${FILE}"; then
  warn "aspnetcoreEnvironment should be Production (DevelopmentAdminSeeder only runs in Development)"
fi

if grep -Eiq '^\s*requireHttpsMetadata:[[:space:]]*["'\'']?false["'\'']?' "${FILE}"; then
  warn "jwt.requireHttpsMetadata is false — prefer true behind TLS Ingress"
fi

if grep -Eiq '^\s*type:[[:space:]]*LoadBalancer' "${FILE}"; then
  warn "gateway Service LoadBalancer with Ingress is unusual — prefer ClusterIP"
fi

if [[ "${FAILED}" -ne 0 ]]; then
  echo "Validation failed for ${FILE}" >&2
  exit 1
fi

echo "OK: ${FILE} has no known production placeholders."
