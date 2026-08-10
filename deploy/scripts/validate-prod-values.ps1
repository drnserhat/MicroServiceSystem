# Fail closed if a production values file still contains placeholders.
# Usage: .\validate-prod-values.ps1 path\to\values-production.local.yaml
param(
    [Parameter(Mandatory = $true)]
    [string]$ValuesFile
)

if (-not (Test-Path -LiteralPath $ValuesFile)) {
    Write-Error "File not found: $ValuesFile"
    exit 2
}

$content = Get-Content -LiteralPath $ValuesFile -Raw
$failed = $false

function Fail([string]$Message) {
    Write-Host "FAIL: $Message" -ForegroundColor Red
    $script:failed = $true
}

function Warn([string]$Message) {
    Write-Host "WARN: $Message" -ForegroundColor Yellow
}

$patterns = @(
    'REQUIRED_',
    'REPLACE_ME',
    'replace-with',
    'change-me',
    'REQUIRED_PG',
    'REQUIRED_GHCR',
    'REQUIRED_JWT',
    'REQUIRED_INTERNAL',
    'REQUIRED_RABBIT',
    'REQUIRED_REDIS',
    'REQUIRED_MONGO',
    'REQUIRED_IMAGE'
)

foreach ($pattern in $patterns) {
    if ($content -match [regex]::Escape($pattern)) {
        Fail "placeholder pattern still present: $pattern"
    }
}

$keyLine = Select-String -Path $ValuesFile -Pattern '^\s*jwtSigningKey:' | Select-Object -First 1
if ($null -eq $keyLine) {
    Fail "secrets.jwtSigningKey not found"
}
else {
    $keyVal = ($keyLine.Line -replace '^[^:]+:\s*', '' -replace '^["'']', '' -replace '["'']$', '').Trim()
    if ($keyVal.Length -lt 32) {
        Fail "secrets.jwtSigningKey must be at least 32 characters (got $($keyVal.Length))"
    }
}

if ($content -match '(?im)^\s*tag:\s*["'']?latest["'']?\s*$') {
    Fail 'image.tag must not be "latest" for production'
}

if ($content -notmatch '(?im)^\s*aspnetcoreEnvironment:\s*Production') {
    Warn "aspnetcoreEnvironment should be Production"
}

if ($content -match '(?im)^\s*requireHttpsMetadata:\s*["'']?false["'']?') {
    Warn "jwt.requireHttpsMetadata is false — prefer true behind TLS Ingress"
}

if ($content -match '(?im)^\s*type:\s*LoadBalancer') {
    Warn "gateway Service LoadBalancer with Ingress is unusual — prefer ClusterIP"
}

if ($failed) {
    Write-Error "Validation failed for $ValuesFile"
    exit 1
}

Write-Host "OK: $ValuesFile has no known production placeholders." -ForegroundColor Green
exit 0
