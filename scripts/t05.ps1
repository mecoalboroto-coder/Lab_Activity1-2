# T05: multi-item checkout, three-item limit, invalid requests and atomic rollback.
# Normal run (steps 1 to 3): .\scripts\t05.ps1
# Fault run (step 4):        .\scripts\t05.ps1 -Fault
#   Start the API with EQUIPMENT_BORROWING_FAIL_BEFORE_COMMIT = "1" for the fault run only.
param([switch]$Fault)

$hostUrl = "http://localhost:5171"
$api = "$hostUrl/api/v1"
$db = "equipment_borrowing_test"
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$root = Split-Path $PSScriptRoot -Parent
$outDir = Join-Path $root "docs\evidence"
New-Item -ItemType Directory -Force $outDir | Out-Null
$log = Join-Path $outDir "T05.txt"
$bodyIn = Join-Path $env:TEMP "t05-in.json"
$bodyOut = Join-Path $env:TEMP "t05-out.txt"
$headers = Join-Path $env:TEMP "t05-headers.txt"
$script:failures = 0

function Log($text, $color = "Gray") {
    Write-Host $text -ForegroundColor $color
    Add-Content $log $text -Encoding utf8
}

function Check($label, $condition) {
    if ($condition) { Log "[PASS] $label" "Green" }
    else { Log "[FAIL] $label" "Red"; $script:failures++ }
}

function Call($method, $url, $expected, $json) {
    Remove-Item $bodyOut, $headers -ErrorAction SilentlyContinue
    $curlArgs = @("-sS", "-X", $method, "-o", $bodyOut, "-D", $headers, "-w", "%{http_code}")
    if ($json) {
        [IO.File]::WriteAllText($bodyIn, $json)
        $curlArgs += @("-H", "Content-Type: application/json", "--data-binary", "@$bodyIn")
    }
    $status = & curl.exe @curlArgs $url
    $body = ""
    if (Test-Path $bodyOut) { $body = [IO.File]::ReadAllText($bodyOut) }
    $script:location = ""
    if (Test-Path $headers) {
        $line = Get-Content $headers | Where-Object { $_ -like "Location:*" } | Select-Object -First 1
        if ($line) { $script:location = $line.Substring(9).Trim() }
    }
    Log ""
    Log "$method $url"
    if ($json) { Log "Request: $json" }
    Log "Status: $status"
    if ($script:location) { Log "Location: $($script:location)" }
    Log "Body: $body"
    Check "expected status $expected" ("$status" -eq "$expected")
    if ("$status" -like "2*" -and $body) { $parsed = $body | ConvertFrom-Json; return $parsed }
}

function Snapshot($title) {
    $sql = Join-Path $env:TEMP "t05-snapshot.sql"
    [IO.File]::WriteAllText($sql, @'
SELECT "Id", "Identifier", "IsAvailable" FROM "Equipment" ORDER BY "Id";
SELECT "Id", "StudentId", "EquipmentId", "Status", "DateBorrowed", "ExpectedReturnDate", "ReturnedAt" FROM "Borrowings" ORDER BY "Id";
'@)
    Log ""
    Log "--- PostgreSQL ($db): $title, $((Get-Date).ToString('HH:mm:ss')) ---"
    $rows = & $psql -h localhost -U postgres -d $db -v ON_ERROR_STOP=1 -f $sql
    if ($LASTEXITCODE -ne 0) { Log "Database read failed." "Red"; exit 1 }
    $rows | ForEach-Object { Log $_ }
    return ($rows -join "`n")
}

function Reset-Data($title) {
    Log ""
    Log "========== $title =========="
    & $psql -h localhost -U postgres -d $db -v ON_ERROR_STOP=1 -f (Join-Path $root "scripts\reset-test-data.sql") | Out-Null
    if ($LASTEXITCODE -ne 0) { Log "Reset failed." "Red"; exit 1 }
}

# IDs restart at 1 on every reset: students S1-S4 = 1-4, equipment E1-E7 = 1-7. Confirmed below from the API.
function Checkout($student, $items, $due) {
    return '{"studentId":' + $student + ',"equipmentIds":[' + ($items -join ",") + '],"expectedReturnDate":"' + $due + '"}'
}

function ActiveCount($student) {
    $history = Call GET "$api/students/$student/borrowings" 200
    return @(@($history) | Where-Object { $_.status -eq "Active" }).Count
}

function Rejected($label, $expected, $json) {
    Log ""
    Log "-- Subcase: $label"
    Call POST "$api/checkout-requests" $expected $json | Out-Null
    $now = Snapshot "after $label"
    Check "no rows or availability changed ($label)" ($now -eq $script:baseline)
}

if (-not (Test-Path $psql)) { Write-Host "psql not found at $psql" -ForegroundColor Red; exit 1 }

$secure = Read-Host "PostgreSQL password" -AsSecureString
$env:PGPASSWORD = (New-Object System.Net.NetworkCredential("", $secure)).Password

try {
    & $psql -h localhost -U postgres -d $db -c "SELECT 1" | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host "Cannot connect. Check the password." -ForegroundColor Red; exit 1 }

    if ($Fault) { Reset-Data "Step 4: injected failure, $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))" }
    else {
        "T05 run at $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))" | Set-Content $log -Encoding utf8
        Reset-Data "Step 1: S4 checks out E2 and E6"
    }

    $students = Call GET "$api/students" 200
    $equipment = Call GET "$api/equipment" 200
    $studentMap = (@($students) | ForEach-Object { "$($_.id)=$($_.studentNumber)" }) -join ","
    $itemMap = (@($equipment) | ForEach-Object { "$($_.id)=$($_.equipmentCode)" }) -join ","
    if ($studentMap -ne "1=ST-100,2=ST-200,3=ST-300,4=ST-400" -or $itemMap -ne "1=EQ-001,2=EQ-002,3=EQ-003,4=EQ-004,5=EQ-005,6=EQ-006,7=EQ-007") {
        Log "The API does not show the test dataset. Is it running against $db?" "Red"; exit 1
    }

    if ($Fault) {
        Log "Failure point: EfBorrowingTransactions.CheckoutAsync, after SaveChangesAsync, before CommitAsync."
        $script:baseline = Snapshot "before injected failure"
        Rejected "injected failure" 500 (Checkout 4 @(2, 6) "2026-11-12")
    }
    else {
        Snapshot "before step 1" | Out-Null
        $r = Call POST "$api/checkout-requests" 201 (Checkout 4 @(2, 6) "2026-11-12")
        Check "two borrowings returned" (@($r.borrowings).Count -eq 2)
        Check "S4 has two active items" ((ActiveCount 4) -eq 2)
        Snapshot "after step 1" | Out-Null

        Reset-Data "Step 2a: S3 (two active) borrows E2"
        Snapshot "before 2a" | Out-Null
        Call POST "$api/borrowings" 201 '{"studentId":3,"equipmentId":2,"expectedReturnDate":"2026-11-12"}' | Out-Null
        Check "S3 has exactly three active items" ((ActiveCount 3) -eq 3)
        Snapshot "after 2a" | Out-Null

        Reset-Data "Step 2b: S1 (one active) checks out E2 and E6"
        Snapshot "before 2b" | Out-Null
        $r = Call POST "$api/checkout-requests" 201 (Checkout 1 @(2, 6) "2026-11-12")
        Check "two borrowings returned" (@($r.borrowings).Count -eq 2)
        Check "S1 has exactly three active items" ((ActiveCount 1) -eq 3)
        Snapshot "after 2b" | Out-Null

        Reset-Data "Step 2c: S3 (two active) checks out E2 and E6, over the limit"
        $script:baseline = Snapshot "before 2c"
        Rejected "over the limit" 409 (Checkout 3 @(2, 6) "2026-11-12")

        Reset-Data "Step 3: invalid requests"
        $script:baseline = Snapshot "before step 3"
        Rejected "duplicate items" 400 (Checkout 4 @(2, 2) "2026-11-12")
        Rejected "one item" 400 (Checkout 4 @(2) "2026-11-12")
        Rejected "unavailable E1 with E2" 409 (Checkout 4 @(1, 2) "2026-11-12")
        Rejected "restricted S2" 409 (Checkout 2 @(2, 6) "2026-11-12")
        Rejected "missing equipment" 404 (Checkout 4 @(2, 999999) "2026-11-12")
        Rejected "missing student" 404 (Checkout 999999 @(2, 6) "2026-11-12")
        Rejected "date before checkout date" 400 (Checkout 4 @(2, 6) "2026-11-09")
    }

    Log ""
    if ($script:failures -eq 0) { Log "All T05 script checks passed." "Green" }
    else { Log "$($script:failures) check(s) failed." "Red" }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
