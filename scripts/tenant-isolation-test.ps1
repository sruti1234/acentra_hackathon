# Tenant isolation test — run while the API is running on http://localhost:5000
#   powershell -ExecutionPolicy Bypass -File scripts\tenant-isolation-test.ps1
$Base = "http://localhost:5000"
$script:Failed = 0

function Invoke-Api($Method, $Path, $Token, $TenantId, $Body) {
    $headers = @{}
    if ($Token)    { $headers["Authorization"] = "Bearer $Token" }
    if ($TenantId) { $headers["X-Tenant-ID"] = "$TenantId" }
    $params = @{ Uri = "$Base$Path"; Method = $Method; Headers = $headers; UseBasicParsing = $true }
    if ($Body) { $params.Body = ($Body | ConvertTo-Json); $params.ContentType = "application/json" }
    try {
        $r = Invoke-WebRequest @params
        $data = $null
        if ($r.Content) { $data = $r.Content | ConvertFrom-Json }
        return @{ Status = [int]$r.StatusCode; Data = $data }
    } catch {
        if ($_.Exception.Response) { return @{ Status = [int]$_.Exception.Response.StatusCode; Data = $null } }
        throw
    }
}

function Check($Name, $Condition) {
    if ($Condition) { Write-Host "PASS  $Name" -ForegroundColor Green }
    else { Write-Host "FAIL  $Name" -ForegroundColor Red; $script:Failed++ }
}

$demo = (Invoke-Api POST "/api/auth/login" $null $null @{ email = "demo@example.com"; password = "Demo@123" }).Data
$abc  = (Invoke-Api POST "/api/auth/login" $null $null @{ email = "abc@example.com";  password = "Abc@123" }).Data
Check "Demo user logs in and sees 3 organizations" ($demo.tenants.Count -eq 3)
Check "ABC user logs in and sees 1 organization"   ($abc.tenants.Count -eq 1)

$t1 = Invoke-Api GET "/api/products" $demo.token 1
$t2 = Invoke-Api GET "/api/products" $demo.token 2
Write-Host "   Tenant 1: $(($t1.Data | ForEach-Object name) -join ', ')"
Write-Host "   Tenant 2: $(($t2.Data | ForEach-Object name) -join ', ')"
Check "X-Tenant-ID: 1 returns only tenant 1 products" ($t1.Status -eq 200 -and @($t1.Data | Where-Object tenantId -ne 1).Count -eq 0)
Check "X-Tenant-ID: 2 returns only tenant 2 products" ($t2.Status -eq 200 -and @($t2.Data | Where-Object tenantId -ne 2).Count -eq 0)

Check "ABC user with X-Tenant-ID: 2 gets 403" ((Invoke-Api GET "/api/products" $abc.token 2).Status -eq 403)

$t2ProductId = $t2.Data[0].id
Check "Tenant 1 cannot GET tenant 2 product $t2ProductId (404)"    ((Invoke-Api GET    "/api/products/$t2ProductId" $demo.token 1).Status -eq 404)
Check "Tenant 1 cannot UPDATE tenant 2 product $t2ProductId (404)" ((Invoke-Api PUT    "/api/products/$t2ProductId" $demo.token 1 @{ name = "hacked"; quantity = 1; price = 1 }).Status -eq 404)
Check "Tenant 1 cannot DELETE tenant 2 product $t2ProductId (404)" ((Invoke-Api DELETE "/api/products/$t2ProductId" $demo.token 1).Status -eq 404)
Check "Tenant 2 product is unchanged" ((Invoke-Api GET "/api/products/$t2ProductId" $demo.token 2).Data.name -ne "hacked")

$created = Invoke-Api POST "/api/products" $demo.token 1 @{ name = "Isolation Test"; quantity = 1; price = 1; tenantId = 2 }
Check "tenantId in request body is ignored (product lands in tenant 1)" ($created.Status -eq 201 -and $created.Data.tenantId -eq 1)
if ($created.Data.id) { [void](Invoke-Api DELETE "/api/products/$($created.Data.id)" $demo.token 1) }

Check "No token -> 401"          ((Invoke-Api GET "/api/products" $null 1).Status -eq 401)
Check "Missing X-Tenant-ID -> 400" ((Invoke-Api GET "/api/products" $demo.token $null).Status -eq 400)
Check "Unknown tenant -> 404"      ((Invoke-Api GET "/api/products" $demo.token 999).Status -eq 404)

if ($script:Failed -eq 0) { Write-Host "`nALL TENANT ISOLATION TESTS PASSED" -ForegroundColor Green }
else { Write-Host "`n$script:Failed TEST(S) FAILED" -ForegroundColor Red; exit 1 }
