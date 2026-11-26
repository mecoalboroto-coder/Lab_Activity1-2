# T02: student create, edit, eligibility, uniqueness and history.
# Normal run: resets equipment_borrowing_test, runs the test, saves evidence.
# After restarting the API: .\scripts\t02.ps1 -AfterRestart   (no reset, reads only)
param([switch]$AfterRestart)

$hostUrl = "http://localhost:5171"
$api = "$hostUrl/api/v1"
$db = "equipment_borrowing_test"
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$root = Split-Path $PSScriptRoot -Parent
$outDir = Join-Path $root "docs\evidence"
New-Item -ItemType Directory -Force $outDir | Out-Null
$log = Join-Path $outDir "T02.txt"
$bodyIn = Join-Path $env:TEMP "t02-in.json"
$bodyOut = Join-Path $env:TEMP "t02-out.txt"
$headers = Join-Path $env:TEMP "t02-headers.txt"
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
    $sql = Join-Path $env:TEMP "t02-snapshot.sql"
    [IO.File]::WriteAllText($sql, @'
SELECT "Id", "StudentNumber", "Name", "Contact", "IsAllowedToBorrow" FROM "Students" ORDER BY "Id";
SELECT b."Id", s."StudentNumber", e."Identifier", e."IsAvailable", b."Status", b."DateBorrowed", b."ExpectedReturnDate", b."ReturnedAt"
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
        "T02 run at $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))" | Set-Content $log -Encoding utf8
        & $psql -h localhost -U postgres -d $db -v ON_ERROR_STOP=1 -f (Join-Path $root "scripts\reset-test-data.sql") | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Host "Reset failed. Nothing was tested." -ForegroundColor Red; exit 1 }
    } else {
        Log ""
        Log "=== After API restart, $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz')) ==="
    }

    $students = Call GET "$api/students" 200
    $s1 = @($students) | Where-Object { $_.studentNumber -eq "ST-100" }
    if (-not $s1) { Log "The API does not show the test dataset. Is it running against $db?" "Red"; exit 1 }

    if ($AfterRestart) {
        $new = @($students) | Where-Object { $_.studentNumber -eq "ST-500" }
        Check "ST-500 edits survived restart" ($new.fullName -eq "Eva Santos-Reyes" -and $new.contact -eq "eva@example.com")
        $history = Call GET "$api/students/$($s1.id)/borrowings" 200
        Check "S1 history has three records after restart" (@($history).Count -eq 3)
        Snapshot "after restart"
    }
    else {
        if (@($students).Count -ne 4) { Log "Expected exactly four students. Stopping." "Red"; exit 1 }
        $equipment = Call GET "$api/equipment" 200
        $e2 = @($equipment) | Where-Object { $_.equipmentCode -eq "EQ-002" }
        Snapshot "before"

        # Step 1: create, read by Location, update
        $new = Call POST "$api/students" 201 '{"studentNumber":"ST-500","fullName":"Eva Santos","contact":null}'
        Check "201 includes Location" ([bool]$script:location)
        if (-not ($new -and $script:location)) { Log "Cannot continue without the new student." "Red"; exit 1 }
        $read = Call GET "$hostUrl$($script:location)" 200
        Check "Location returns ST-500, eligible, no contact" ($read.studentNumber -eq "ST-500" -and $read.isEligible -eq $true -and $null -eq $read.contact)
        $upd = Call PUT "$api/students/$($new.id)" 200 '{"studentNumber":"ST-500","fullName":"Eva Santos-Reyes","contact":"eva@example.com"}'
        Check "name and contact changed" ($upd.fullName -eq "Eva Santos-Reyes" -and $upd.contact -eq "eva@example.com")

        # Step 2: rejected input
        Call POST "$api/students" 400 '{"studentNumber":"ST-501","fullName":"   ","contact":null}' | Out-Null
        Call POST "$api/students" 409 '{"studentNumber":" st-100 ","fullName":"Duplicate","contact":null}' | Out-Null
        $after = Call GET "$api/students" 200
        Check "rejected requests added no students (five total)" (@($after).Count -eq 5)

        # Step 3: restrict, return, permit, issue
        $r = Call PATCH "$api/students/$($s1.id)/eligibility" 200 '{"isEligible":false}'
        Check "S1 restricted" ($r.isEligible -eq $false)
        $issue = '{"studentId":' + $s1.id + ',"equipmentId":' + $e2.id + ',"expectedReturnDate":"2026-11-12"}'
        Call POST "$api/borrowings" 409 $issue | Out-Null
        $history = Call GET "$api/students/$($s1.id)/borrowings" 200
        $loan = @($history) | Where-Object { $_.equipmentCode -eq "EQ-001" }
        Check "restriction kept history and the active loan" (@($history).Count -eq 2 -and $loan.status -eq "Active")
        $ret = Call POST "$api/borrowings/$($loan.id)/return" 200 '{}'
        Check "E1 returned on the fixed date 2026-11-10" ($ret.returnedDate -eq "2026-11-10")
        $p = Call PATCH "$api/students/$($s1.id)/eligibility" 200 '{"isEligible":true}'
        Check "S1 permitted" ($p.isEligible -eq $true)
        $ok = Call POST "$api/borrowings" 201 $issue
        Check "new issue dated 2026-11-10" ($ok.borrowedDate -eq "2026-11-10")
        $before = Call GET "$api/students/$($s1.id)/borrowings" 200
        Call PATCH "$api/students/$($s1.id)/eligibility" 200 '{"isEligible":true}' | Out-Null
        $same = Call GET "$api/students/$($s1.id)/borrowings" 200
        Check "repeating eligibility left history unchanged" ((ConvertTo-Json @($before) -Compress) -eq (ConvertTo-Json @($same) -Compress))
        Check "S1 history has three records" (@($same).Count -eq 3)
        Snapshot "after"
    }

    Log ""
    if ($script:failures -eq 0) { Log "All T02 script checks passed." "Green" }
    else { Log "$($script:failures) check(s) failed." "Red" }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
