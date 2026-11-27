# T04: single borrowing, return, validation and persistence.
# Normal run: resets equipment_borrowing_test, runs the test, saves evidence.
# After restarting the API: .\scripts\t04.ps1 -AfterRestart   (no reset, reads only)
param([switch]$AfterRestart)

$hostUrl = "http://localhost:5171"
$api = "$hostUrl/api/v1"
$db = "equipment_borrowing_test"
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$root = Split-Path $PSScriptRoot -Parent
$outDir = Join-Path $root "docs\evidence"
New-Item -ItemType Directory -Force $outDir | Out-Null
$log = Join-Path $outDir "T04.txt"
$bodyIn = Join-Path $env:TEMP "t04-in.json"
$bodyOut = Join-Path $env:TEMP "t04-out.txt"
$headers = Join-Path $env:TEMP "t04-headers.txt"
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
    $sql = Join-Path $env:TEMP "t04-snapshot.sql"
    [IO.File]::WriteAllText($sql, @'
SELECT "Id", "Identifier", "IsAvailable" FROM "Equipment" ORDER BY "Id";
SELECT b."Id", s."StudentNumber", e."Identifier", b."Status", b."DateBorrowed", b."ExpectedReturnDate", b."ReturnedAt"
FROM "Borrowings" b JOIN "Students" s ON s."Id" = b."StudentId" JOIN "Equipment" e ON e."Id" = b."EquipmentId" ORDER BY b."Id";
'@)
    Log ""
    Log "--- PostgreSQL ($db): $title, $((Get-Date).ToString('HH:mm:ss')) ---"
    & $psql -h localhost -U postgres -d $db -v ON_ERROR_STOP=1 -f $sql | ForEach-Object { Log $_ }
}

if (-not (Test-Path $psql)) { Write-Host "psql not found at $psql" -ForegroundColor Red; exit 1 }

$secure = Read-Host "PostgreSQL password" -AsSecureString
$env:PGPASSWORD = (New-Object System.Net.NetworkCredential("", $secure)).Password

try {
    & $psql -h localhost -U postgres -d $db -c "SELECT 1" | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host "Cannot connect. Check the password." -ForegroundColor Red; exit 1 }

    if (-not $AfterRestart) {
        "T04 run at $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))" | Set-Content $log -Encoding utf8
        & $psql -h localhost -U postgres -d $db -v ON_ERROR_STOP=1 -f (Join-Path $root "scripts\reset-test-data.sql") | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Host "Reset failed. Nothing was tested." -ForegroundColor Red; exit 1 }
    } else {
        Log ""
        Log "=== After API restart, $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz')) ==="
    }

    $students = Call GET "$api/students" 200
    if (-not (@($students) | Where-Object { $_.studentNumber -eq "ST-100" })) { Log "The API does not show the test dataset. Is it running against $db?" "Red"; exit 1 }
    $equipment = Call GET "$api/equipment" 200
    $s2 = (@($students) | Where-Object { $_.studentNumber -eq "ST-200" }).id
    $s4 = (@($students) | Where-Object { $_.studentNumber -eq "ST-400" }).id
    $e1 = (@($equipment) | Where-Object { $_.equipmentCode -eq "EQ-001" }).id
    $e2 = (@($equipment) | Where-Object { $_.equipmentCode -eq "EQ-002" }).id
    $e6 = (@($equipment) | Where-Object { $_.equipmentCode -eq "EQ-006" }).id

    function Issue($student, $item, $due) {
        return '{"studentId":' + $student + ',"equipmentId":' + $item + ',"expectedReturnDate":"' + $due + '"}'
    }

    if ($AfterRestart) {
        $history = Call GET "$api/students/$s4/borrowings" 200
        Check "S4 has one borrowing" (@($history).Count -eq 1)
        $loan = Call GET "$api/borrowings/$(@($history)[0].id)" 200
        Check "borrowing still Returned on 2026-11-10" ($loan.status -eq "Returned" -and $loan.returnedDate -eq "2026-11-10")
        $item = Call GET "$api/equipment/$e2" 200
        Check "E2 still available" ($item.isAvailable -eq $true)
        Snapshot "after restart"
    }
    else {
        Snapshot "before"

        # Step 1: issue and read back
        $new = Call POST "$api/borrowings" 201 (Issue $s4 $e2 "2026-11-12")
        Check "201 includes Location" ([bool]$script:location)
        if (-not ($new -and $script:location)) { Log "Cannot continue without the new borrowing." "Red"; exit 1 }
        $read = Call GET "$hostUrl$($script:location)" 200
        Check "Active, borrowed 2026-11-10, due 2026-11-12" ($read.status -eq "Active" -and $read.borrowedDate -eq "2026-11-10" -and $read.expectedReturnDate -eq "2026-11-12" -and $null -eq $read.returnedDate)
        $item = Call GET "$api/equipment/$e2" 200
        Check "E2 unavailable" ($item.isAvailable -eq $false)
        Snapshot "after issue"

        # Step 2: rejected attempts must write nothing
        $loansBefore = Call GET "$api/borrowings" 200
        $itemsBefore = Call GET "$api/equipment" 200
        Call POST "$api/borrowings" 409 (Issue $s2 $e6 "2026-11-12") | Out-Null
        Call POST "$api/borrowings" 409 (Issue $s4 $e1 "2026-11-12") | Out-Null
        Call POST "$api/borrowings" 404 (Issue 999999 $e6 "2026-11-12") | Out-Null
        Call POST "$api/borrowings" 404 (Issue $s4 999999 "2026-11-12") | Out-Null
        Call POST "$api/borrowings" 400 (Issue $s4 $e6 "2026-11-09") | Out-Null
        Call POST "$api/borrowings" 400 (Issue $s4 $e6 "not-a-date") | Out-Null
        $loansAfter = Call GET "$api/borrowings" 200
        $itemsAfter = Call GET "$api/equipment" 200
        Check "rejected attempts changed no borrowings" ((ConvertTo-Json @($loansBefore) -Compress) -eq (ConvertTo-Json @($loansAfter) -Compress))
        Check "rejected attempts changed no equipment" ((ConvertTo-Json @($itemsBefore) -Compress) -eq (ConvertTo-Json @($itemsAfter) -Compress))
        Snapshot "after rejected attempts"

        # Step 3: return, read, repeat
        $ret = Call POST "$api/borrowings/$($new.id)/return" 200 '{}'
        Check "returned on 2026-11-10" ($ret.status -eq "Returned" -and $ret.returnedDate -eq "2026-11-10")
        $loan = Call GET "$api/borrowings/$($new.id)" 200
        Check "GET shows the same borrowing as Returned" ($loan.id -eq $new.id -and $loan.status -eq "Returned")
        $item = Call GET "$api/equipment/$e2" 200
        Check "E2 available again" ($item.isAvailable -eq $true)
        Call POST "$api/borrowings/$($new.id)/return" 409 '{}' | Out-Null
        Call POST "$api/borrowings/999999/return" 404 '{}' | Out-Null
        Snapshot "after return"
    }

    Log ""
    if ($script:failures -eq 0) { Log "All T04 script checks passed." "Green" }
    else { Log "$($script:failures) check(s) failed." "Red" }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
