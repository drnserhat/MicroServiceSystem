# Post-deploy P0 smoke checks against a live Gateway (or Ingress host).
#
#   $env:MSF_BASE_URL = "https://admin.example.com"
#   # optional:
#   $env:MSF_EMAIL = "..."; $env:MSF_PASSWORD = "..."; $env:MSF_TENANT_ID = "..."
#   .\smoke-prod.ps1

$BaseUrl = $env:MSF_BASE_URL
if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    Write-Error "Set MSF_BASE_URL (e.g. https://admin.example.com)"
    exit 2
}

$BaseUrl = $BaseUrl.TrimEnd("/")
$failed = $false

function Pass([string]$Message) { Write-Host "PASS: $Message" -ForegroundColor Green }
function Fail([string]$Message) {
    Write-Host "FAIL: $Message" -ForegroundColor Red
    $script:failed = $true
}

Write-Host "Smoke against $BaseUrl"

try {
    $health = Invoke-WebRequest -Uri "$BaseUrl/ops/api/v1/health/services" -UseBasicParsing
    if ($health.StatusCode -ne 200) {
        Fail "GET /ops/api/v1/health/services → HTTP $($health.StatusCode)"
    }
    else {
        Pass "health aggregate HTTP 200"
        try {
            $payload = $health.Content | ConvertFrom-Json
            $bad = @($payload.services | Where-Object { $_.reachable -eq $false })
            if ($bad.Count -eq 0) { Pass "all probed services reachable" }
            else { Fail "$($bad.Count) service(s) not reachable" }
        }
        catch {
            Write-Host "INFO: could not parse health JSON"
        }
    }
}
catch {
    Fail "health request failed: $($_.Exception.Message)"
}

if ($BaseUrl.StartsWith("https://", [StringComparison]::OrdinalIgnoreCase)) {
    try {
        $root = Invoke-WebRequest -Uri "$BaseUrl/" -UseBasicParsing
        if ($root.StatusCode -ge 200 -and $root.StatusCode -lt 400) {
            Pass "HTTPS root reachable (HTTP $($root.StatusCode))"
        }
        else {
            Fail "HTTPS root not OK (HTTP $($root.StatusCode))"
        }
    }
    catch {
        Fail "HTTPS root failed: $($_.Exception.Message)"
    }
}

$email = $env:MSF_EMAIL
$password = $env:MSF_PASSWORD
$tenant = $env:MSF_TENANT_ID

if ($email -and $password -and $tenant) {
    $body = @{ email = $email; password = $password; tenantId = $tenant } | ConvertTo-Json
    try {
        $login = Invoke-WebRequest -Uri "$BaseUrl/identity/api/v1/auth/login" `
            -Method POST -ContentType "application/json" -Body $body -UseBasicParsing
        if ($login.StatusCode -ne 200) {
            Fail "login → HTTP $($login.StatusCode)"
        }
        else {
            Pass "login HTTP 200"
            $loginJson = $login.Content | ConvertFrom-Json
            $token = $loginJson.data.accessToken
            if (-not $token) { $token = $loginJson.data.AccessToken }
            if (-not $token) {
                Fail "login response missing accessToken"
            }
            else {
                Pass "accessToken present"
                try {
                    Invoke-WebRequest -Uri "$BaseUrl/identity/api/v1/roles" -UseBasicParsing | Out-Null
                    Fail "roles without JWT expected 401"
                }
                catch {
                    $code = $_.Exception.Response.StatusCode.value__
                    if ($code -eq 401) { Pass "roles without JWT → 401" }
                    else { Fail "roles without JWT expected 401, got $code" }
                }

                $headers = @{
                    Authorization = "Bearer $token"
                    "X-Tenant-Id" = $tenant
                }
                $roles = Invoke-WebRequest -Uri "$BaseUrl/identity/api/v1/roles" -Headers $headers -UseBasicParsing
                if ($roles.StatusCode -eq 200) { Pass "roles with JWT → 200" }
                else { Fail "roles with JWT expected 200, got $($roles.StatusCode)" }
            }
        }
    }
    catch {
        Fail "login failed: $($_.Exception.Message)"
    }
}
else {
    Write-Host "INFO: skip login smoke (set MSF_EMAIL MSF_PASSWORD MSF_TENANT_ID)"
}

if ($failed) {
    Write-Error "Smoke FAILED"
    exit 1
}

Write-Host "Smoke OK" -ForegroundColor Green
exit 0
