#!/usr/bin/env bash
# Post-deploy P0 smoke checks against a live Gateway (or Ingress host).
#
# Required:
#   MSF_BASE_URL   e.g. https://admin.example.com  or  http://localhost:8080
#
# Optional login checks:
#   MSF_EMAIL MSF_PASSWORD MSF_TENANT_ID
#
# Usage:
#   MSF_BASE_URL=https://admin.example.com ./smoke-prod.sh
set -euo pipefail

BASE_URL="${MSF_BASE_URL:-}"
if [[ -z "${BASE_URL}" ]]; then
  echo "Set MSF_BASE_URL (e.g. https://admin.example.com)" >&2
  exit 2
fi

BASE_URL="${BASE_URL%/}"
FAILED=0

pass() { echo "PASS: $1"; }
fail() { echo "FAIL: $1" >&2; FAILED=1; }

echo "Smoke against ${BASE_URL}"

# 1) Health aggregate (anonymous at gateway /ops)
HEALTH_CODE="$(curl -sS -o /tmp/msf-health.json -w '%{http_code}' \
  "${BASE_URL}/ops/api/v1/health/services" || echo "000")"
if [[ "${HEALTH_CODE}" != "200" ]]; then
  fail "GET /ops/api/v1/health/services → HTTP ${HEALTH_CODE}"
else
  pass "health aggregate HTTP 200"
  if command -v jq >/dev/null 2>&1; then
    UNREACHABLE="$(jq -r '[.services[]? | select(.reachable==false)] | length' /tmp/msf-health.json 2>/dev/null || echo "?")"
    if [[ "${UNREACHABLE}" == "0" ]]; then
      pass "all probed services reachable"
    else
      fail "${UNREACHABLE} service(s) not reachable — inspect /tmp/msf-health.json"
    fi
  else
    echo "INFO: install jq for per-service reachability parsing"
  fi
fi

# 2) TLS expectation when URL is https
if [[ "${BASE_URL}" == https://* ]]; then
  TLS_CODE="$(curl -sS -o /dev/null -w '%{http_code}' --fail "${BASE_URL}/" || echo "000")"
  if [[ "${TLS_CODE}" =~ ^[23] ]]; then
    pass "HTTPS root reachable (HTTP ${TLS_CODE})"
  else
    fail "HTTPS root not OK (HTTP ${TLS_CODE})"
  fi
fi

# 3) Optional auth smoke
EMAIL="${MSF_EMAIL:-}"
PASSWORD="${MSF_PASSWORD:-}"
TENANT="${MSF_TENANT_ID:-}"

if [[ -n "${EMAIL}" && -n "${PASSWORD}" && -n "${TENANT}" ]]; then
  LOGIN_CODE="$(curl -sS -o /tmp/msf-login.json -w '%{http_code}' \
    -H 'Content-Type: application/json' \
    -d "{\"email\":\"${EMAIL}\",\"password\":\"${PASSWORD}\",\"tenantId\":\"${TENANT}\"}" \
    "${BASE_URL}/identity/api/v1/auth/login" || echo "000")"
  if [[ "${LOGIN_CODE}" != "200" ]]; then
    fail "POST /identity/api/v1/auth/login → HTTP ${LOGIN_CODE}"
  else
    pass "login HTTP 200"
    if command -v jq >/dev/null 2>&1; then
      TOKEN="$(jq -r '.data.accessToken // .data.AccessToken // empty' /tmp/msf-login.json)"
      if [[ -z "${TOKEN}" ]]; then
        fail "login response missing accessToken"
      else
        pass "accessToken present"
        # Unauthorized probe without token on a protected route
        FORBIDDEN_CODE="$(curl -sS -o /dev/null -w '%{http_code}' \
          "${BASE_URL}/identity/api/v1/roles" || echo "000")"
        if [[ "${FORBIDDEN_CODE}" == "401" ]]; then
          pass "roles without JWT → 401"
        else
          fail "roles without JWT expected 401, got ${FORBIDDEN_CODE}"
        fi
        OK_CODE="$(curl -sS -o /dev/null -w '%{http_code}' \
          -H "Authorization: Bearer ${TOKEN}" \
          -H "X-Tenant-Id: ${TENANT}" \
          "${BASE_URL}/identity/api/v1/roles" || echo "000")"
        if [[ "${OK_CODE}" == "200" ]]; then
          pass "roles with JWT → 200"
        else
          fail "roles with JWT expected 200, got ${OK_CODE}"
        fi
      fi
    fi
  fi
else
  echo "INFO: skip login smoke (set MSF_EMAIL MSF_PASSWORD MSF_TENANT_ID)"
fi

if [[ "${FAILED}" -ne 0 ]]; then
  echo "Smoke FAILED" >&2
  exit 1
fi

echo "Smoke OK"
