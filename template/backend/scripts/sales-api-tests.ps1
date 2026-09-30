<#
.SYNOPSIS
    End-to-end tests for the Sales API. Runs every endpoint, business rule and error case
    against a running API and prints PASS/FAIL for each check.

.USAGE
    powershell -ExecutionPolicy Bypass -File .\sales-api-tests.ps1
    powershell -ExecutionPolicy Bypass -File .\sales-api-tests.ps1 -BaseUrl http://localhost:8080
    powershell -ExecutionPolicy Bypass -File .\sales-api-tests.ps1 -Cleanup

    Each run uses unique sale numbers (T<time>-N), so it can be executed multiple times.
#>
param(
    [string]$BaseUrl = "http://localhost:5119",
    [switch]$Cleanup
)

$ErrorActionPreference = "Stop"
$script:Passed = 0
$script:Failed = 0
$script:Failures = @()

$Unknown = "00000000-0000-0000-0000-000000000001"
$Run = Get-Date -Format "HHmmss"
$Prefix = "T$Run"

# ---------------------------------------------------------------- helpers

function Invoke-Api {
    param([string]$Method, [string]$Path, $Body = $null)

    $params = @{
        Uri             = "$BaseUrl$Path"
        Method          = $Method
        UseBasicParsing = $true
        ContentType     = "application/json"
    }
    if ($null -ne $Body) {
        $params.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 10))
    }

    $status = 0
    $content = $null
    try {
        $response = Invoke-WebRequest @params
        $status = [int]$response.StatusCode
        $content = $response.Content
    }
    catch {
        $errorResponse = $_.Exception.Response
        if ($null -eq $errorResponse) {
            throw "API unreachable at $BaseUrl. Is it running? $($_.Exception.Message)"
        }
        $status = [int]$errorResponse.StatusCode
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
            $content = $_.ErrorDetails.Message
        }
        else {
            try {
                $reader = New-Object System.IO.StreamReader($errorResponse.GetResponseStream())
                $content = $reader.ReadToEnd()
            }
            catch { }
        }
    }

    $json = $null
    if ($content) {
        try { $json = $content | ConvertFrom-Json } catch { }
    }

    return [pscustomobject]@{ Status = $status; Json = $json; Raw = $content }
}

function Check {
    param([string]$Name, [bool]$Condition, [string]$Detail = "")
    if ($Condition) {
        $script:Passed++
        Write-Host "  PASS  $Name" -ForegroundColor Green
    }
    else {
        $script:Failed++
        $script:Failures += "$Name $Detail"
        Write-Host "  FAIL  $Name $Detail" -ForegroundColor Red
    }
}

function Near($a, $b) {
    if ($null -eq $a) { return $false }
    return [math]::Abs([double]$a - [double]$b) -lt 0.001
}

function Section([string]$Title) {
    Write-Host ""
    Write-Host "== $Title" -ForegroundColor Cyan
}

function New-Sale {
    param([string]$Number, [string]$Date, [string]$Customer, [string]$Branch, [array]$Items)
    return @{
        saleNumber   = $Number
        saleDate     = $Date
        customerId   = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        customerName = $Customer
        branchId     = "9b2f1c3e-1a2b-4c5d-8e9f-0a1b2c3d4e5f"
        branchName   = $Branch
        items        = $Items
    }
}

function Item([string]$ProductId, [string]$Name, [int]$Quantity, [double]$Price) {
    return @{ productId = $ProductId; productName = $Name; quantity = $Quantity; unitPrice = $Price }
}

$Brahma   = "11111111-1111-1111-1111-111111111111"
$Skol     = "22222222-2222-2222-2222-222222222222"
$Stella   = "33333333-3333-3333-3333-333333333333"
$Original = "44444444-4444-4444-4444-444444444444"
$Bud      = "55555555-5555-5555-5555-555555555555"

Write-Host "Sales API tests - $BaseUrl - run $Prefix" -ForegroundColor Yellow

# ---------------------------------------------------------------- health

Section "Health"
$r = Invoke-Api GET "/health"
Check "GET /health returns 200" ($r.Status -eq 200) "(got $($r.Status))"

# ---------------------------------------------------------------- create

Section "POST /api/sales - discount tiers"

# S1: 3 items (0%), 5 items (10%), 12 items (20%) -> 13.50 + 18.00 + 66.24 = 97.74
$s1Body = New-Sale "$Prefix-1" "2026-09-30T10:00:00Z" "John $Prefix" "Belo Horizonte - Savassi" @(
    (Item $Brahma "Brahma 350ml" 3 4.50),
    (Item $Skol   "Skol 350ml"   5 4.00),
    (Item $Stella "Stella 330ml" 12 6.90)
)
$r = Invoke-Api POST "/api/sales" $s1Body
Check "S1 created (201)" ($r.Status -eq 201) "(got $($r.Status) $($r.Raw))"
$s1 = $r.Json.data
$s1Id = $s1.id
$s1Brahma = $s1.items | Where-Object { $_.productId -eq $Brahma }
$s1Skol   = $s1.items | Where-Object { $_.productId -eq $Skol }
$s1Stella = $s1.items | Where-Object { $_.productId -eq $Stella }
Check "S1 response has message 'Sale created successfully'" ($r.Json.message -eq "Sale created successfully")
Check "S1 3 items -> 0% discount, total 13.50" ((Near $s1Brahma.discountPercentage 0) -and (Near $s1Brahma.discount 0) -and (Near $s1Brahma.totalAmount 13.50))
Check "S1 5 items -> 10% discount (2.00), total 18.00" ((Near $s1Skol.discountPercentage 0.10) -and (Near $s1Skol.discount 2.00) -and (Near $s1Skol.totalAmount 18.00))
Check "S1 12 items -> 20% discount (16.56), total 66.24" ((Near $s1Stella.discountPercentage 0.20) -and (Near $s1Stella.discount 16.56) -and (Near $s1Stella.totalAmount 66.24))
Check "S1 sale total 97.74" (Near $s1.totalAmount 97.74) "(got $($s1.totalAmount))"
Check "S1 not cancelled" ($s1.isCancelled -eq $false)

# S2: exactly 4 items -> 10%
$s2Body = New-Sale "$Prefix-2" "2026-09-29T15:00:00Z" "Maria $Prefix" "Contagem - Centro" @(
    (Item $Skol "Skol 350ml" 4 4.00)
)
$r = Invoke-Api POST "/api/sales" $s2Body
Check "S2 created (201)" ($r.Status -eq 201) "(got $($r.Status) $($r.Raw))"
$s2Id = $r.Json.data.id
Check "S2 4 items -> 10% discount (1.60), total 14.40" ((Near $r.Json.data.items[0].discountPercentage 0.10) -and (Near $r.Json.data.items[0].discount 1.60) -and (Near $r.Json.data.totalAmount 14.40))

# S3: exactly 20 items -> 20% (max allowed)
$s3Body = New-Sale "$Prefix-3" "2026-09-28T09:00:00Z" "Pedro $Prefix" "Belo Horizonte - Savassi" @(
    (Item $Original "Original 600ml" 20 2.50)
)
$r = Invoke-Api POST "/api/sales" $s3Body
Check "S3 created (201)" ($r.Status -eq 201) "(got $($r.Status) $($r.Raw))"
$s3Id = $r.Json.data.id
Check "S3 20 items -> 20% discount (10.00), total 40.00" ((Near $r.Json.data.items[0].discountPercentage 0.20) -and (Near $r.Json.data.totalAmount 40.00))

# S4: 9 items (10%) and 10 items (20%) boundaries -> 81.00 + 80.00 = 161.00
$s4Body = New-Sale "$Prefix-4" "2026-09-27T09:00:00Z" "Ana $Prefix" "Betim - Centro" @(
    (Item $Bud    "Budweiser 350ml" 9 10.00),
    (Item $Stella "Stella 330ml"    10 10.00)
)
$r = Invoke-Api POST "/api/sales" $s4Body
Check "S4 created (201)" ($r.Status -eq 201) "(got $($r.Status) $($r.Raw))"
$s4Id = $r.Json.data.id
$s4Bud    = $r.Json.data.items | Where-Object { $_.productId -eq $Bud }
$s4Stella = $r.Json.data.items | Where-Object { $_.productId -eq $Stella }
Check "S4 9 items -> 10%, total 81.00" ((Near $s4Bud.discountPercentage 0.10) -and (Near $s4Bud.totalAmount 81.00))
Check "S4 10 items -> 20%, total 80.00" ((Near $s4Stella.discountPercentage 0.20) -and (Near $s4Stella.totalAmount 80.00))
Check "S4 sale total 161.00" (Near $r.Json.data.totalAmount 161.00)

Section "POST /api/sales - validation and conflicts"

$r = Invoke-Api POST "/api/sales" $s2Body
Check "Duplicate sale number -> 409" ($r.Status -eq 409) "(got $($r.Status))"
Check "Error response has success=false" ($r.Json.success -eq $false)

$bad = New-Sale "$Prefix-X1" "2026-09-30T10:00:00Z" "X" "Y" @((Item $Skol "Skol" 21 4.00))
$r = Invoke-Api POST "/api/sales" $bad
Check "21 identical items -> 400" ($r.Status -eq 400) "(got $($r.Status))"
Check "21 items message mentions the 20 limit" ($r.Raw -match "more than 20")

$bad = New-Sale "$Prefix-X2" "2026-09-30T10:00:00Z" "X" "Y" @((Item $Skol "Skol" 0 4.00))
$r = Invoke-Api POST "/api/sales" $bad
Check "Quantity 0 -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$bad = New-Sale "$Prefix-X3" "2026-09-30T10:00:00Z" "X" "Y" @((Item $Skol "Skol" 2 -1))
$r = Invoke-Api POST "/api/sales" $bad
Check "Negative unit price -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$bad = New-Sale "$Prefix-X4" "2026-09-30T10:00:00Z" "X" "Y" @()
$r = Invoke-Api POST "/api/sales" $bad
Check "No items -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$bad = New-Sale "$Prefix-X5" "2026-09-30T10:00:00Z" "" "Y" @((Item $Skol "Skol" 2 4.00))
$r = Invoke-Api POST "/api/sales" $bad
Check "Empty customer name -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$bad = New-Sale "$Prefix-X6" "2026-09-30T10:00:00Z" "X" "" @((Item $Skol "Skol" 2 4.00))
$r = Invoke-Api POST "/api/sales" $bad
Check "Empty branch name -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$bad = New-Sale "" "2026-09-30T10:00:00Z" "X" "Y" @((Item $Skol "Skol" 2 4.00))
$r = Invoke-Api POST "/api/sales" $bad
Check "Empty sale number -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$bad = New-Sale "$Prefix-X7" "2026-09-30T10:00:00Z" "X" "Y" @((Item $Skol "Skol" 2 4.00))
$bad.customerId = "00000000-0000-0000-0000-000000000000"
$r = Invoke-Api POST "/api/sales" $bad
Check "Empty customer id -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$bad = New-Sale "$Prefix-X8" "2026-09-30T10:00:00Z" "X" "Y" @((Item "00000000-0000-0000-0000-000000000000" "Skol" 2 4.00))
$r = Invoke-Api POST "/api/sales" $bad
Check "Empty product id -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$bad = New-Sale "$Prefix-X9" "2026-09-30T10:00:00Z" "X" "Y" @((Item $Skol "Skol" 15 4.00), (Item $Skol "Skol" 15 4.00))
$r = Invoke-Api POST "/api/sales" $bad
Check "Same product in two lines -> 400" ($r.Status -eq 400) "(got $($r.Status))"
Check "Duplicate product message mentions single line" ($r.Raw -match "single line")

# ---------------------------------------------------------------- get

Section "GET /api/sales/{id}"

$r = Invoke-Api GET "/api/sales/$s1Id"
Check "Existing sale -> 200" ($r.Status -eq 200) "(got $($r.Status))"
Check "Returns the requested sale" ($r.Json.data.id -eq $s1Id)
Check "Response is not double-wrapped" ($null -eq $r.Json.data.data)
Check "Message 'Sale retrieved successfully'" ($r.Json.message -eq "Sale retrieved successfully")
Check "Returns 3 items" (@($r.Json.data.items).Count -eq 3)

$r = Invoke-Api GET "/api/sales/$Unknown"
Check "Unknown sale -> 404" ($r.Status -eq 404) "(got $($r.Status))"

# ---------------------------------------------------------------- list

Section "GET /api/sales - pagination, ordering, filters"

$scope = "saleNumber=$Prefix-*"

$r = Invoke-Api GET "/api/sales?$scope"
Check "Wildcard saleNumber filter returns the 4 sales of this run" ($r.Json.totalCount -eq 4) "(got $($r.Json.totalCount))"
Check "Paginated envelope (currentPage/totalPages/totalCount)" (($r.Json.currentPage -eq 1) -and ($r.Json.totalPages -eq 1))
Check "Default order is saleDate desc (S1 first)" ($r.Json.data[0].id -eq $s1Id)

$r = Invoke-Api GET "/api/sales?$scope&_order=totalAmount desc"
Check "_order=totalAmount desc -> S4 (161) first, S2 (14.40) last" (($r.Json.data[0].id -eq $s4Id) -and ($r.Json.data[3].id -eq $s2Id))

$r = Invoke-Api GET "/api/sales?$scope&_order=totalAmount asc"
Check "_order=totalAmount asc -> S2 first" ($r.Json.data[0].id -eq $s2Id)

$r = Invoke-Api GET "/api/sales?$scope&_order=saleNumber"
Check "_order=saleNumber (default asc) -> S1 first" ($r.Json.data[0].id -eq $s1Id)

$r = Invoke-Api GET "/api/sales?$scope&customerName=Maria*"
Check "customerName=Maria* -> only S2" (($r.Json.totalCount -eq 1) -and ($r.Json.data[0].id -eq $s2Id))

$r = Invoke-Api GET "/api/sales?$scope&customerName=maria $Prefix"
Check "customerName exact match is case-insensitive" ($r.Json.totalCount -eq 1)

$r = Invoke-Api GET "/api/sales?$scope&branchName=*Savassi"
Check "branchName=*Savassi -> S1 and S3" ($r.Json.totalCount -eq 2) "(got $($r.Json.totalCount))"

$r = Invoke-Api GET "/api/sales?$scope&branchName=*Centro*"
Check "branchName=*Centro* -> S2 and S4" ($r.Json.totalCount -eq 2) "(got $($r.Json.totalCount))"

$r = Invoke-Api GET "/api/sales?$scope&_minTotalAmount=20&_maxTotalAmount=100"
Check "_minTotalAmount=20&_maxTotalAmount=100 -> S1 and S3" ($r.Json.totalCount -eq 2) "(got $($r.Json.totalCount))"

$r = Invoke-Api GET "/api/sales?$scope&_minSaleDate=2026-09-29T00:00:00Z"
Check "_minSaleDate=2026-09-29 -> S1 and S2" ($r.Json.totalCount -eq 2) "(got $($r.Json.totalCount))"

$r = Invoke-Api GET "/api/sales?$scope&_maxSaleDate=2026-09-28T23:59:59Z"
Check "_maxSaleDate=2026-09-28 -> S3 and S4" ($r.Json.totalCount -eq 2) "(got $($r.Json.totalCount))"

$r = Invoke-Api GET "/api/sales?$scope&isCancelled=false"
Check "isCancelled=false -> 4 sales" ($r.Json.totalCount -eq 4)

$r = Invoke-Api GET "/api/sales?$scope&_page=2&_size=3"
Check "_page=2&_size=3 -> 1 item, totalPages 2" ((@($r.Json.data).Count -eq 1) -and ($r.Json.totalPages -eq 2) -and ($r.Json.currentPage -eq 2))

$r = Invoke-Api GET "/api/sales?_size=0"
Check "_size=0 -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$r = Invoke-Api GET "/api/sales?_size=101"
Check "_size=101 -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$r = Invoke-Api GET "/api/sales?_page=0"
Check "_page=0 -> 400" ($r.Status -eq 400) "(got $($r.Status))"

# ---------------------------------------------------------------- update

Section "PUT /api/sales/{id}"

# Keep Brahma (3 -> 10: 20%), remove Skol and Stella, add Original (4: 10%) -> 36.00 + 18.00 = 54.00
$updateBody = @{
    saleDate     = "2026-09-30T10:00:00Z"
    customerId   = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    customerName = "John $Prefix Updated"
    branchId     = "9b2f1c3e-1a2b-4c5d-8e9f-0a1b2c3d4e5f"
    branchName   = "Belo Horizonte - Savassi"
    items        = @(
        (Item $Brahma   "Brahma 350ml"   10 4.50),
        (Item $Original "Original 600ml" 4  5.00)
    )
}
$r = Invoke-Api PUT "/api/sales/$s1Id" $updateBody
Check "Update S1 -> 200" ($r.Status -eq 200) "(got $($r.Status) $($r.Raw))"
$u = $r.Json.data
$uBrahma   = $u.items | Where-Object { $_.productId -eq $Brahma }
$uOriginal = $u.items | Where-Object { $_.productId -eq $Original }
Check "Customer name updated" ($u.customerName -eq "John $Prefix Updated")
Check "Sale number unchanged" ($u.saleNumber -eq "$Prefix-1")
Check "Removed products are gone (2 items left)" (@($u.items).Count -eq 2)
Check "Existing product keeps its item id" ($uBrahma.id -eq $s1Brahma.id)
Check "Updated item recalculated: 10 items -> 20%, total 36.00" ((Near $uBrahma.discountPercentage 0.20) -and (Near $uBrahma.totalAmount 36.00))
Check "New item added: 4 items -> 10%, total 18.00" ((Near $uOriginal.discountPercentage 0.10) -and (Near $uOriginal.totalAmount 18.00))
Check "Sale total 54.00" (Near $u.totalAmount 54.00) "(got $($u.totalAmount))"
Check "updatedAt is set" ($null -ne $u.updatedAt)
Check "Response is not double-wrapped" ($null -eq $r.Json.data.data)
$originalItemId = $uOriginal.id

$r = Invoke-Api PUT "/api/sales/$Unknown" $updateBody
Check "Update unknown sale -> 404" ($r.Status -eq 404) "(got $($r.Status))"

$badUpdate = $updateBody.Clone()
$badUpdate.items = @((Item $Brahma "Brahma 350ml" 25 4.50))
$r = Invoke-Api PUT "/api/sales/$s1Id" $badUpdate
Check "Update with 25 identical items -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$badUpdate = $updateBody.Clone()
$badUpdate.items = @((Item $Brahma "Brahma" 5 4.50), (Item $Brahma "Brahma" 5 4.50))
$r = Invoke-Api PUT "/api/sales/$s1Id" $badUpdate
Check "Update with same product in two lines -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$badUpdate = $updateBody.Clone()
$badUpdate.items = @()
$r = Invoke-Api PUT "/api/sales/$s1Id" $badUpdate
Check "Update with no items -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$r = Invoke-Api GET "/api/sales/$s1Id"
Check "Failed updates did not change the sale (total still 54.00)" (Near $r.Json.data.totalAmount 54.00)

# ---------------------------------------------------------------- cancel item

Section "PATCH /api/sales/{id}/items/{itemId}/cancel"

$r = Invoke-Api PATCH "/api/sales/$s1Id/items/$originalItemId/cancel"
Check "Cancel item -> 200" ($r.Status -eq 200) "(got $($r.Status) $($r.Raw))"
$ci = $r.Json.data
Check "Item marked as cancelled" (($ci.items | Where-Object { $_.id -eq $originalItemId }).isCancelled -eq $true)
Check "Sale total recalculated without cancelled item (36.00)" (Near $ci.totalAmount 36.00) "(got $($ci.totalAmount))"
Check "Sale itself stays active" ($ci.isCancelled -eq $false)

$r = Invoke-Api PATCH "/api/sales/$s1Id/items/$originalItemId/cancel"
Check "Cancel same item again -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$r = Invoke-Api PATCH "/api/sales/$s1Id/items/$Unknown/cancel"
Check "Cancel unknown item -> 404" ($r.Status -eq 404) "(got $($r.Status))"

$r = Invoke-Api PATCH "/api/sales/$Unknown/items/$originalItemId/cancel"
Check "Cancel item of unknown sale -> 404" ($r.Status -eq 404) "(got $($r.Status))"

# ---------------------------------------------------------------- cancel sale

Section "PATCH /api/sales/{id}/cancel"

$r = Invoke-Api PATCH "/api/sales/$s3Id/cancel"
Check "Cancel sale -> 200" ($r.Status -eq 200) "(got $($r.Status) $($r.Raw))"
Check "Sale marked as cancelled" ($r.Json.data.isCancelled -eq $true)

$r = Invoke-Api PATCH "/api/sales/$s3Id/cancel"
Check "Cancel same sale again -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$r = Invoke-Api PUT "/api/sales/$s3Id" $updateBody
Check "Update cancelled sale -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$s3ItemId = (Invoke-Api GET "/api/sales/$s3Id").Json.data.items[0].id
$r = Invoke-Api PATCH "/api/sales/$s3Id/items/$s3ItemId/cancel"
Check "Cancel item of cancelled sale -> 400" ($r.Status -eq 400) "(got $($r.Status))"

$r = Invoke-Api PATCH "/api/sales/$Unknown/cancel"
Check "Cancel unknown sale -> 404" ($r.Status -eq 404) "(got $($r.Status))"

$r = Invoke-Api GET "/api/sales?$scope&isCancelled=true"
Check "isCancelled=true filter -> only S3" (($r.Json.totalCount -eq 1) -and ($r.Json.data[0].id -eq $s3Id))

# ---------------------------------------------------------------- delete

Section "DELETE /api/sales/{id}"

$r = Invoke-Api DELETE "/api/sales/$s2Id"
Check "Delete sale -> 200" ($r.Status -eq 200) "(got $($r.Status))"
Check "Message 'Sale deleted successfully'" ($r.Json.message -eq "Sale deleted successfully")

$r = Invoke-Api GET "/api/sales/$s2Id"
Check "Deleted sale -> GET 404" ($r.Status -eq 404) "(got $($r.Status))"

$r = Invoke-Api DELETE "/api/sales/$s2Id"
Check "Delete again -> 404" ($r.Status -eq 404) "(got $($r.Status))"

$r = Invoke-Api GET "/api/sales?$scope"
Check "List now has 3 sales of this run" ($r.Json.totalCount -eq 3)

# ---------------------------------------------------------------- cleanup

if ($Cleanup) {
    Section "Cleanup"
    foreach ($id in @($s1Id, $s3Id, $s4Id)) {
        $null = Invoke-Api DELETE "/api/sales/$id"
    }
    Write-Host "  Test sales removed."
}

# ---------------------------------------------------------------- summary

Write-Host ""
Write-Host "==============================================" -ForegroundColor Yellow
$total = $script:Passed + $script:Failed
if ($script:Failed -eq 0) {
    Write-Host "  ALL $total CHECKS PASSED" -ForegroundColor Green
}
else {
    Write-Host "  $($script:Passed)/$total passed, $($script:Failed) failed:" -ForegroundColor Red
    $script:Failures | ForEach-Object { Write-Host "   - $_" -ForegroundColor Red }
}
Write-Host "==============================================" -ForegroundColor Yellow
Write-Host "Check the API console for: SaleCreatedEvent x4, SaleModifiedEvent x1, ItemCancelledEvent x1, SaleCancelledEvent x1"

exit $script:Failed
