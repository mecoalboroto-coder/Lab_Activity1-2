# T03: equipment create, edit, uniqueness and protected state.
# Normal run: resets equipment_borrowing_test, runs the test, saves evidence.
# After restarting the API: .\scripts\t03.ps1 -AfterRestart   (no reset, reads only)
param([switch]$AfterRestart)

$hostUrl = "http://localhost:5171"
$api = "$hostUrl/api/v1"
$db = "equipment_borrowing_test"
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$root = Split-Path $PSScriptRoot -Parent
$outDir = Join-Path $root "docs\evidence"
New-Item -ItemType Directory -Force $outDir | Out-Null
$log = Join-Path $outDir "T03.txt"
$bodyIn = Join-Path $env:TEMP "t03-in.json"
$bodyOut = Join-Path $env:TEMP "t03-out.txt"
$headers = Join-Path $env:TEMP "t03-headers.txt"
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
    $sql = Join-Path $env:TEMP "t03-snapshot.sql"
    [IO.File]::WriteAllText($sql, @'
SELECT "Id", "Identifier", "Name", "Description", "IsAvailable" FROM "Equipment" ORDER BY "Id";
SELECT "Id", "StudentId", "EquipmentId", "Status", "DateBorrowed", "ExpectedReturnDate", "ReturnedAt" FROM "Borrowings" ORDER BY "Id";
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
        "T03 run at $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))" | Set-Content $log -Encoding utf8
        & $psql -h localhost -U postgres -d $db -v ON_ERROR_STOP=1 -f (Join-Path $root "scripts\reset-test-data.sql") | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Host "Reset failed. Nothing was tested." -ForegroundColor Red; exit 1 }
    } else {
        Log ""
        Log "=== After API restart, $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz')) ==="
    }

    $students = Call GET "$api/students" 200
    if (-not (@($students) | Where-Object { $_.studentNumber -eq "ST-100" })) { Log "The API does not show the test dataset. Is it running against $db?" "Red"; exit 1 }
    $equipment = Call GET "$api/equipment" 200
    $e1 = @($equipment) | Where-Object { $_.equipmentCode -eq "EQ-001" }

    if ($AfterRestart) {
        $new = @($equipment) | Where-Object { $_.equipmentCode -eq "EQ-008" }
        Check "EQ-008 edit survived restart" ($new.name -eq "Monitor Mk2" -and $new.isAvailable -eq $true)
        Check "E1 edit survived restart, still unavailable" ($e1.description -eq "Digital camera" -and $e1.isAvailable -eq $false)
        Snapshot "after restart"
    }
    else {
        if (@($equipment).Count -ne 7) { Log "Expected exactly seven items. Stopping." "Red"; exit 1 }
        $historyBefore = Call GET "$api/equipment/$($e1.id)/borrowings" 200
        Snapshot "before"

        # Create, read by Location, update
        $new = Call POST "$api/equipment" 201 '{"equipmentCode":"EQ-008","name":"Monitor","description":null}'
        Check "201 includes Location" ([bool]$script:location)
        if (-not ($new -and $script:location)) { Log "Cannot continue without the new item." "Red"; exit 1 }
        Check "new item is available" ($new.isAvailable -eq $true)
        $read = Call GET "$hostUrl$($script:location)" 200
        Check "Location returns EQ-008 with no description" ($read.equipmentCode -eq "EQ-008" -and $null -eq $read.description)
        $upd = Call PUT "$api/equipment/$($new.id)" 200 '{"equipmentCode":"EQ-008","name":"Monitor Mk2","description":null}'
        Check "EQ-008 name changed" ($upd.name -eq "Monitor Mk2")

        # Edit an unavailable item, then rejected input
        $edit = Call PUT "$api/equipment/$($e1.id)" 200 '{"equipmentCode":"EQ-001","name":"Camera","description":"Digital camera"}'
        Check "E1 description changed, still unavailable" ($edit.description -eq "Digital camera" -and $edit.isAvailable -eq $false)
        $before = Call GET "$api/equipment" 200
        Call PUT "$api/equipment/$($e1.id)" 400 '{"equipmentCode":"EQ-001","name":"   ","description":"Digital camera"}' | Out-Null
        Call POST "$api/equipment" 409 '{"equipmentCode":" eq-001 ","name":"Duplicate","description":null}' | Out-Null
        Call PUT "$api/equipment/$($e1.id)" 409 '{"equipmentCode":" eq-002 ","name":"Camera","description":"Digital camera"}' | Out-Null
        Call PUT "$api/equipment/$($e1.id)" 400 '{"equipmentCode":"EQ-001","name":"Camera","description":"Digital camera","isAvailable":true}' | Out-Null
        Call PUT "$api/equipment/999999" 404 '{"equipmentCode":"EQ-999","name":"Missing","description":null}' | Out-Null
        $after = Call GET "$api/equipment" 200
        Check "rejected requests changed no equipment" ((ConvertTo-Json @($before) -Compress) -eq (ConvertTo-Json @($after) -Compress))
        Check "eight items in total" (@($after).Count -eq 8)
        $historyAfter = Call GET "$api/equipment/$($e1.id)/borrowings" 200
        Check "E1 history unchanged" ((ConvertTo-Json @($historyBefore) -Compress) -eq (ConvertTo-Json @($historyAfter) -Compress))
        Snapshot "after"
    }

    Log ""
    if ($script:failures -eq 0) { Log "All T03 script checks passed." "Green" }
    else { Log "$($script:failures) check(s) failed." "Red" }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
