# OWASP-oriented baseline applied by BuildingBlocks.ServiceDefaults SecurityHeadersMiddleware.
# Services inherit these defaults; override per host only when a UI surface needs a wider CSP.

Content-Security-Policy: default-src 'self'
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
Permissions-Policy: geolocation=(), microphone=(), camera=()
Strict-Transport-Security: max-age=31536000; includeSubDomains

Authentication defaults:
- JWT bearer required (fallback authorization policy)
- Anonymous endpoints must opt in with [AllowAnonymous]
- Idempotency header X-Idempotency-Key available for mutations
- Tenant resolution claim-first, optional X-Tenant-Id when TrustTenantHeader is enabled
