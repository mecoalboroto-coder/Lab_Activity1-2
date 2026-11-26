# T01: independent API reads. Read-only. Run with the controlled dataset and the desktop app closed.
$hostUrl = "http://localhost:5171"
$api = "$hostUrl/api/v1"
$root = Split-Path $PSScriptRoot -Parent
$outDir = Join-Path $root "docs\evidence"
New-Item -ItemType Directory -Force $outDir | Out-Null
$log = Join-Path $outDir "T01.txt"
$tmp = Join-Path $env:TEMP "t01-body.txt"
$script:failures = 0

"T01 run at $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))" | Set-Content $log -Encoding utf8

function Log($text, $color = "Gray") {
    Write-Host $text -ForegroundColor $color
    Add-Content $log $text -Encoding utf8
}

function Check($label, $condition) {
    if ($condition) { Log "[PASS] $label" "Green" }
    else { Log "[FAIL] $label" "Red"; $script:failures++ }
}

function Call($method, $url, $expected) {
    if (Test-Path $tmp) { Remove-Item $tmp }
    $status = & curl.exe -sS -X $method -o $tmp -w "%{http_code}" $url
    $body = ""
    if (Test-Path $tmp) { $body = [IO.File]::ReadAllText($tmp) }
    Log ""
    Log "$method $url"
    Log "Status: $status"
    Log "Body: $body"
    Check "expected status $expected" ("$status" -eq "$expected")
    $script:lastBody = $body
    if ("$status" -eq "200" -and $body) { $parsed = $body | ConvertFrom-Json; return $parsed }
}

function Codes($rows) { return ((@($rows) | ForEach-Object { $_.equipmentCode } | Sort-Object) -join ",") }

# Step 1: lists and single records, using IDs returned by the API
$equipment = Call GET "$api/equipment" 200
Check "all seven items" ((Codes $equipment) -eq "EQ-001,EQ-002,EQ-003,EQ-004,EQ-005,EQ-006,EQ-007")
$available = Call GET "$api/equipment?available=true" 200
Check "available excludes EQ-001, EQ-004, EQ-005" ((Codes $available) -eq "EQ-002,EQ-003,EQ-006,EQ-007")
Call GET "$api/equipment?available=maybe" 400 | Out-Null
$students = Call GET "$api/students" 200
Check "four students" (@($students).Count -eq 4)

$s1 = @($students) | Where-Object { $_.studentNumber -eq "ST-100" }
$s4 = @($students) | Where-Object { $_.studentNumber -eq "ST-400" }
$e1 = @($equipment) | Where-Object { $_.equipmentCode -eq "EQ-001" }
$e2 = @($equipment) | Where-Object { $_.equipmentCode -eq "EQ-002" }
if (-not ($s1 -and $s4 -and $e1 -and $e2)) { Log "Dataset not found. Run the reset script first." "Red"; exit 1 }

$one = Call GET "$api/students/$($s1.id)" 200
Check "S1 is Ada Cruz" ($one.fullName -eq "Ada Cruz")
$item = Call GET "$api/equipment/$($e1.id)" 200
Check "E1 is unavailable" ($item.isAvailable -eq $false)

# Step 2: histories and active borrowings
$h1 = Call GET "$api/students/$($s1.id)/borrowings" 200
Check "S1 history has EQ-001 and EQ-003" ((Codes $h1) -eq "EQ-001,EQ-003")
$loan = @($h1) | Where-Object { $_.equipmentCode -eq "EQ-001" }
$known = Call GET "$api/borrowings/$($loan.id)" 200
Check "known borrowing is Active, 2026-11-01 to 2026-11-09" ($known.status -eq "Active" -and $known.borrowedDate -eq "2026-11-01" -and $known.expectedReturnDate -eq "2026-11-09")
$eh = Call GET "$api/equipment/$($e1.id)/borrowings" 200
Check "E1 history has one record for ST-100" (@($eh).Count -eq 1 -and @($eh)[0].studentNumber -eq "ST-100")
$active = Call GET "$api/borrowings?status=Active" 200
Check "active borrowings are EQ-001, EQ-004, EQ-005" ((Codes $active) -eq "EQ-001,EQ-004,EQ-005")

# Step 3: empty histories, unknown IDs, wrong route and method
Call GET "$api/students/$($s4.id)/borrowings" 200 | Out-Null
Check "S4 history is []" ($script:lastBody.Trim() -eq "[]")
Call GET "$api/equipment/$($e2.id)/borrowings" 200 | Out-Null
Check "E2 history is []" ($script:lastBody.Trim() -eq "[]")
foreach ($path in "students/999999", "equipment/999999", "borrowings/999999", "students/999999/borrowings", "equipment/999999/borrowings") {
    Call GET "$api/$path" 404 | Out-Null
}
Call GET "$api/nothing" 404 | Out-Null
Call DELETE "$api/students" 405 | Out-Null

# Step 4: OpenAPI description
$specFile = Join-Path $root "docs\openapi.json"
$status = & curl.exe -sS -o $specFile -w "%{http_code}" "$hostUrl/swagger/v1/swagger.json"
Log ""
Log "GET $hostUrl/swagger/v1/swagger.json"
Log "Status: $status (saved to docs/openapi.json)"
Check "OpenAPI document retrieved" ("$status" -eq "200")

$spec = [IO.File]::ReadAllText($specFile) | ConvertFrom-Json
$count = 0
foreach ($p in $spec.paths.PSObject.Properties) {
    foreach ($m in $p.Value.PSObject.Properties) {
        $codes = ($m.Value.responses.PSObject.Properties.Name) -join ","
        Log "  $($m.Name.ToUpper()) $($p.Name)  responses: $codes"
        $count++
    }
}
Check "sixteen operations published (found $count)" ($count -eq 16)

Log ""
if ($script:failures -eq 0) { Log "All T01 script checks passed." "Green" }
else { Log "$($script:failures) check(s) failed." "Red" }
