# T06: histories, search, filters, sorting and empty results. Resets the data, then only reads.
# Run: .\scripts\t06.ps1

$hostUrl = "http://localhost:5171"
$api = "$hostUrl/api/v1"
$db = "equipment_borrowing_test"
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$root = Split-Path $PSScriptRoot -Parent
$outDir = Join-Path $root "docs\evidence"
New-Item -ItemType Directory -Force $outDir | Out-Null
$log = Join-Path $outDir "T06.txt"
$bodyIn = Join-Path $env:TEMP "t06-in.json"
$bodyOut = Join-Path $env:TEMP "t06-out.txt"
$headers = Join-Path $env:TEMP "t06-headers.txt"
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
    $script:lastBody = $body
    if ("$status" -like "2*" -and $body) { $parsed = $body | ConvertFrom-Json; return $parsed }
}

function Snapshot($title) {
    $sql = Join-Path $env:TEMP "t06-snapshot.sql"
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

function Codes($rows) { return ((@($rows) | ForEach-Object { $_.equipmentCode } | Sort-Object) -join ",") }

function Expect($query, $codes) {
    $rows = Call GET "$api/borrowings$query" 200
    if ($codes -eq "") { Check "returns [] for '$query'" ($script:lastBody.Trim() -eq "[]") }
    else { Check "'$query' returns $codes" ((Codes $rows) -eq $codes) }
}

# Dates must be in order. Ascending ties go by ID ascending; descending ties must use one consistent ID direction.
function Sorted($field, $direction) {
    $url = "$api/borrowings?sort=$field&direction=$direction"
    $rows = @(Call GET $url 200)
    $first = $script:lastBody
    $ordered = $true
    $tieSteps = @()
    for ($i = 1; $i -lt $rows.Count; $i++) {
        $cmp = [string]::CompareOrdinal($rows[$i - 1].$field, $rows[$i].$field)
        if ($direction -eq "asc" -and $cmp -gt 0) { $ordered = $false }
        if ($direction -eq "desc" -and $cmp -lt 0) { $ordered = $false }
        if ($cmp -eq 0) { $tieSteps += [Math]::Sign($rows[$i].id - $rows[$i - 1].id) }
    }
    Check "four rows ordered by $field $direction" ($rows.Count -eq 4 -and $ordered)
    $kinds = @($tieSteps | Sort-Object -Unique)
    if ($direction -eq "asc") { Check "ties in $field $direction ordered by ID ascending" ($kinds.Count -eq 1 -and $kinds[0] -eq 1) }
    else { Check "ties in $field $direction use a consistent ID order" ($kinds.Count -eq 1) }
    Call GET $url 200 | Out-Null
    Check "same order when repeated" ($script:lastBody -eq $first)
}

if (-not (Test-Path $psql)) { Write-Host "psql not found at $psql" -ForegroundColor Red; exit 1 }

$secure = Read-Host "PostgreSQL password" -AsSecureString
$env:PGPASSWORD = (New-Object System.Net.NetworkCredential("", $secure)).Password

try {
    & $psql -h localhost -U postgres -d $db -c "SELECT 1" | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host "Cannot connect. Check the password." -ForegroundColor Red; exit 1 }

    "T06 run at $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))" | Set-Content $log -Encoding utf8
    Reset-Data "Controlled dataset"

    $students = Call GET "$api/students" 200
    $equipment = Call GET "$api/equipment" 200
    if (-not (@($students) | Where-Object { $_.studentNumber -eq "ST-100" })) { Log "The API does not show the test dataset. Is it running against $db?" "Red"; exit 1 }
    $s1 = (@($students) | Where-Object { $_.studentNumber -eq "ST-100" }).id
    $s4 = (@($students) | Where-Object { $_.studentNumber -eq "ST-400" }).id
    $e1 = (@($equipment) | Where-Object { $_.equipmentCode -eq "EQ-001" }).id
    $e3 = (@($equipment) | Where-Object { $_.equipmentCode -eq "EQ-003" }).id
    $before = Snapshot "before"

    # Step 1: histories
    $h = Call GET "$api/students/$s1/borrowings" 200
    Check "S1 history: EQ-001 Active and EQ-003 Returned" ((Codes $h) -eq "EQ-001,EQ-003" -and (@($h) | Where-Object { $_.equipmentCode -eq "EQ-001" }).status -eq "Active" -and (@($h) | Where-Object { $_.equipmentCode -eq "EQ-003" }).status -eq "Returned")
    Call GET "$api/students/$s4/borrowings" 200 | Out-Null
    Check "S4 history is []" ($script:lastBody.Trim() -eq "[]")
    $h = @(Call GET "$api/equipment/$e1/borrowings" 200)
    Check "E1 history: one Active record for ST-100" ($h.Count -eq 1 -and $h[0].studentNumber -eq "ST-100" -and $h[0].status -eq "Active")
    $h = @(Call GET "$api/equipment/$e3/borrowings" 200)
    Check "E3 history: returned by ST-100 on 2026-11-08" ($h.Count -eq 1 -and $h[0].studentNumber -eq "ST-100" -and $h[0].returnedDate -eq "2026-11-08")
    Call GET "$api/students/999999/borrowings" 404 | Out-Null
    Call GET "$api/equipment/999999/borrowings" 404 | Out-Null

    # Step 2: search, status filter, sorting
    Call GET "$api/borrowings" 200 | Out-Null
    $default = $script:lastBody
    Expect "?search=%20%20aDa%20%20" "EQ-001,EQ-003"
    Expect "?search=sT-30" "EQ-004,EQ-005"
    Expect "?search=jEcT" "EQ-003"
    Expect "?status=Active" "EQ-001,EQ-004,EQ-005"
    Expect "?status=Returned" "EQ-003"
    Expect "?status=All" "EQ-001,EQ-003,EQ-004,EQ-005"
    Sorted "borrowedDate" "asc"
    Sorted "borrowedDate" "desc"
    Sorted "expectedReturnDate" "asc"
    Sorted "expectedReturnDate" "desc"

    # Step 3: combined filters, reset, no matches, invalid values
    Expect "?search=ada&status=Active" "EQ-001"
    Expect "?search=ada&status=Returned" "EQ-003"
    $rows = @(Call GET "$api/borrowings?status=Active&sort=expectedReturnDate&direction=desc" 200)
    Check "combined filter and sort: EQ-001 (earliest due) is last of three" ($rows.Count -eq 3 -and $rows[2].equipmentCode -eq "EQ-001")
    Call GET "$api/borrowings" 200 | Out-Null
    Check "clearing criteria restores the default result" ($script:lastBody -eq $default)
    Call GET "$api/borrowings?status=All&sort=borrowedDate&direction=asc" 200 | Out-Null
    Check "default equals status=All, borrowedDate, asc" ($script:lastBody -eq $default)
    Expect "?search=zzz-no-match" ""
    Expect "?search=st-300&status=Returned" ""
    Call GET "$api/borrowings?status=Lost" 400 | Out-Null
    Call GET "$api/borrowings?sort=name" 400 | Out-Null
    Call GET "$api/borrowings?direction=sideways" 400 | Out-Null

    $after = Snapshot "after"
    Check "reads changed nothing in the database" ($before -eq $after)

    Log ""
    if ($script:failures -eq 0) { Log "All T06 script checks passed." "Green" }
    else { Log "$($script:failures) check(s) failed." "Red" }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
