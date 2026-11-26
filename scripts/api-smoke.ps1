param([string]$BaseUrl = "http://localhost:5171/api/v1")

Write-Host "This script WRITES data through the API."
$answer = Read-Host "Is the API running against the DISPOSABLE database equipment_borrowing_test? (y/n)"
if ($answer -ne "y") { Write-Host "Stopped. Start the API with the test database first."; exit 1 }

$probe = & curl.exe -sS -o NUL -w "%{http_code}" "http://localhost:5171/swagger/v1/swagger.json" 2>&1
if ($LASTEXITCODE -ne 0 -or $probe -ne "200") {
    Write-Host "Cannot reach the API at localhost:5171." -ForegroundColor Red
    Write-Host "Start it in ANOTHER PowerShell window and leave that window open, then run this script again."
    exit 1
}

$script:failures = 0

function Send($label, $expected, $method, $path, $json) {
    $curlArgs = @("-sS", "-X", $method, "-w", "`n%{http_code}")
    if ($json) {
        $file = Join-Path $env:TEMP "api-body.json"
        [IO.File]::WriteAllText($file, $json)
        $curlArgs += @("-H", "Content-Type: application/json", "--data-binary", "@$file")
    }
    $curlArgs += "$BaseUrl$path"

    $raw = & curl.exe @curlArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] $label : cannot reach the server" -ForegroundColor Red
        $script:failures++
        return $null
    }

    $lines = (($raw | Out-String).TrimEnd()) -split "`r?`n"
    $status = [int]$lines[$lines.Length - 1]
    $body = ""
    if ($lines.Length -gt 1) { $body = ($lines[0..($lines.Length - 2)] -join " ") }

    if ($status -eq $expected) {
        Write-Host "[PASS] $label  expected=$expected got=$status" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $label  expected=$expected got=$status" -ForegroundColor Red
        $script:failures++
    }
    Write-Host "       $body"

    # Only successful responses are returned, so later steps never use an error body.
    if ($status -ge 300) { return $null }
    $parsed = $null
    try { $parsed = $body | ConvertFrom-Json } catch { }
    return $parsed
}

function Check($label, $condition) {
    if ($condition) {
        Write-Host "[PASS] $label" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $label" -ForegroundColor Red
        $script:failures++
    }
}

$tag = (Get-Date).ToString("HHmmss")
$sn = "ST-$tag"
$eq = "EQ-$tag"
$due = "2026-12-30"

$student = Send "create student" 201 POST "/students" ('{"studentNumber":"' + $sn + '","fullName":"Eli Tan","contact":null}')
Send "duplicate student number (case and spaces)" 409 POST "/students" ('{"studentNumber":" ' + $sn.ToLower() + ' ","fullName":"Dup","contact":null}') | Out-Null
Send "blank full name" 400 POST "/students" '{"studentNumber":"ST-BLANK","fullName":"  ","contact":null}' | Out-Null
Send "unknown field id in student body" 400 POST "/students" '{"studentNumber":"ST-X","fullName":"X","contact":null,"id":5}' | Out-Null
Send "malformed JSON" 400 POST "/students" '{"studentNumber":' | Out-Null

if ($student) {
    $sid = $student.id
    Send "update student, clear contact" 200 PUT "/students/$sid" ('{"studentNumber":"' + $sn + '","fullName":"Eli T. Tan","contact":null}') | Out-Null
    Send "restrict student" 200 PATCH "/students/$sid/eligibility" '{"isEligible":false}' | Out-Null
    Send "restrict again (no-op)" 200 PATCH "/students/$sid/eligibility" '{"isEligible":false}' | Out-Null
    Send "eligibility field missing" 400 PATCH "/students/$sid/eligibility" '{}' | Out-Null
    Send "permit student" 200 PATCH "/students/$sid/eligibility" '{"isEligible":true}' | Out-Null
}
Send "eligibility of missing student" 404 PATCH "/students/999999/eligibility" '{"isEligible":true}' | Out-Null
Send "update missing student" 404 PUT "/students/999999" '{"studentNumber":"ST-NONE","fullName":"None","contact":null}' | Out-Null

$item = Send "create equipment" 201 POST "/equipment" ('{"equipmentCode":"' + $eq + '","name":"Scope","description":null}')
Send "duplicate equipment code" 409 POST "/equipment" ('{"equipmentCode":" ' + $eq.ToLower() + ' ","name":"Dup","description":null}') | Out-Null

if ($item) {
    $eid = $item.id
    Send "equipment update with protected field isAvailable" 400 PUT "/equipment/$eid" ('{"equipmentCode":"' + $eq + '","name":"Scope","description":null,"isAvailable":false}') | Out-Null
    Send "equipment update (descriptive fields)" 200 PUT "/equipment/$eid" ('{"equipmentCode":"' + $eq + '","name":"Scope Mk2","description":"Bench scope"}') | Out-Null
}

if ($student -and $item) {
    $borrowing = Send "borrow one item" 201 POST "/borrowings" ('{"studentId":' + $sid + ',"equipmentId":' + $eid + ',"expectedReturnDate":"' + $due + '"}')
    Send "borrow the same item again (unavailable)" 409 POST "/borrowings" ('{"studentId":' + $sid + ',"equipmentId":' + $eid + ',"expectedReturnDate":"' + $due + '"}') | Out-Null
    Send "checkout with one item" 400 POST "/checkout-requests" ('{"studentId":' + $sid + ',"equipmentIds":[' + $eid + '],"expectedReturnDate":"' + $due + '"}') | Out-Null
    Send "checkout with a duplicate item" 400 POST "/checkout-requests" ('{"studentId":' + $sid + ',"equipmentIds":[' + $eid + ',' + $eid + '],"expectedReturnDate":"' + $due + '"}') | Out-Null
    if ($borrowing) {
        $bid = $borrowing.id
        $returned = Send "return the borrowing" 200 POST "/borrowings/$bid/return" '{}'
        Check "return response includes studentNumber and studentName" ($returned.studentNumber -and $returned.studentName)
        Send "return it again" 409 POST "/borrowings/$bid/return" '{}' | Out-Null
    }
}

# Multi-item checkout and restricted student
$ids = @()
foreach ($suffix in "A", "B", "C") {
    $m = Send "create equipment $suffix" 201 POST "/equipment" ('{"equipmentCode":"' + $eq + '-' + $suffix + '","name":"Probe ' + $suffix + '","description":null}')
    if ($m) { $ids += $m.id }
}

if ($student -and $ids.Count -eq 3) {
    $a = $ids[0]; $b = $ids[1]; $c = $ids[2]
    $tail = '],"expectedReturnDate":"' + $due + '"}'

    Send "restrict before borrowing" 200 PATCH "/students/$sid/eligibility" '{"isEligible":false}' | Out-Null
    Send "restricted student cannot borrow" 409 POST "/borrowings" ('{"studentId":' + $sid + ',"equipmentId":' + $a + ',"expectedReturnDate":"' + $due + '"}') | Out-Null
    Send "permit again" 200 PATCH "/students/$sid/eligibility" '{"isEligible":true}' | Out-Null

    Send "checkout two items" 201 POST "/checkout-requests" ('{"studentId":' + $sid + ',"equipmentIds":[' + $a + ',' + $b + $tail) | Out-Null
    Send "checkout with one unavailable item" 409 POST "/checkout-requests" ('{"studentId":' + $sid + ',"equipmentIds":[' + $c + ',' + $a + $tail) | Out-Null
    Send "other item was not taken by the failed checkout" 201 POST "/borrowings" ('{"studentId":' + $sid + ',"equipmentId":' + $c + ',"expectedReturnDate":"' + $due + '"}') | Out-Null
}

Send "borrow with a missing equipment id" 404 POST "/borrowings" ('{"studentId":1,"equipmentId":999999,"expectedReturnDate":"' + $due + '"}') | Out-Null
Send "borrow with a past date" 400 POST "/borrowings" '{"studentId":1,"equipmentId":1,"expectedReturnDate":"2020-01-01"}' | Out-Null
Send "return a missing borrowing" 404 POST "/borrowings/999999/return" '{}' | Out-Null
Send "unknown route" 404 GET "/nothing" $null | Out-Null
Send "unsupported method" 405 DELETE "/students" $null | Out-Null

Write-Host ""
if ($script:failures -eq 0) { Write-Host "All checks passed." -ForegroundColor Green }
else { Write-Host "$($script:failures) check(s) failed. Paste this output and the API window's last lines." -ForegroundColor Red }
