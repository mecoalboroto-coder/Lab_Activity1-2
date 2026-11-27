# Lab 5 Test Execution Record

## T01: Independent API reads and OpenAPI description

| Field | Entry |
|---|---|
| Test ID / date / environment | T01, 2026-11-27. Windows, .NET 8, PostgreSQL 18, database `equipment_borrowing_test`, API at `http://localhost:5171`. Dataset loaded with `scripts/reset-test-data.sql`; fixed application date 2026-11-10. |
| Expected result | Reads match the seed data; empty histories return `[]`; unknown IDs and routes return 404; unsupported method returns 405; OpenAPI accurately describes API-01 to API-16; no database changes. |
| Observed result (run 1) | All scripted HTTP checks passed and 16 operations were published. Manual review found the OpenAPI schemas inaccurate. |
| Pass / fail (run 1) | **Fail** |
| Issue / diagnosis | Required request fields shown as optional and nullable; `available` typed as string; `status`, `sort` and `direction` had no allowed values; `Location` header not described. The generated description did not reflect the validation done in code. |
| Correction | Added `ContractDocumentFilter` (`src/EquipmentBorrowing.Api`) and registered it in `Program.cs`. |
| Retest outcome | Script checks passed; the regenerated `docs/openapi.json` was reviewed and the four issues are corrected. **Pass** |
| Evidence reference | Run 1: `docs/evidence/T01-run1.txt`, `docs/evidence/openapi-run1.json`. Retest: `docs/evidence/T01.txt`, `docs/openapi.json`. Database counts (4 students, 7 equipment, 4 borrowings, 3 active): pgAdmin screenshot. |

## T02: Student management and eligibility

| Field | Entry |
|---|---|
| Test ID / date / environment | T02, 2026-11-27. Same environment as T01; dataset reset before the run; fixed application date 2026-11-10. |
| Expected result | Create returns 201 with `Location`; updates 200; blank name 400; duplicate number 409; restricted issue 409; return 200 and later issue 201; repeating eligibility 200 with no history change; changes survive an API restart. |
| Observed result | As expected. ST-500 was created and edited; rejected requests added no students; S1 history and the active loan were preserved while restricted; PostgreSQL showed the changes before and after the API restart. |
| Pass / fail | **Pass** |
| Issue / diagnosis | None |
| Correction | None |
| Retest outcome | Not required |
| Evidence reference | `docs/evidence/T02.txt` (requests, responses, PostgreSQL reads before, after and after restart); `scripts/t02.ps1`. |

## T03: Equipment management and protected state

| Field | Entry |
|---|---|
| Test ID / date / environment | T03, 2026-11-27. Same environment as T01; dataset reset before the run; fixed application date 2026-11-10. |
| Expected result | Create returns 201 with `Location` and an available item; updates 200; blank name or protected field 400; duplicate code 409; missing item 404; E1 stays unavailable with the same history; changes survive an API restart. |
| Observed result | As expected. EQ-008 was created and renamed; E1's description changed while it stayed unavailable; rejected requests changed no equipment; borrowing records were unchanged; PostgreSQL showed the changes before and after the API restart. |
| Pass / fail | **Pass** |
| Issue / diagnosis | None |
| Correction | None |
| Retest outcome | Not required |
| Evidence reference | `docs/evidence/T03.txt` (requests, responses, PostgreSQL reads before, after and after restart); `scripts/t03.ps1`. |

## T04: Single borrowing, return, validation and persistence

| Field | Entry |
|---|---|
| Test ID / date / environment | T04, 2026-11-27. Same environment as T01; dataset reset before the run; fixed application date 2026-11-10. |
| Expected result | Issue 201 with `Location`, GET 200; invalid date 400; missing reference 404; ineligible or unavailable 409; return 200, repeat 409; rejected attempts write nothing; state survives an API restart. |
| Observed result | As expected. S4 borrowed E2 and returned it on 2026-11-10; the same record changed from Active to Returned and E2 became available again; rejected attempts changed no borrowings or equipment; PostgreSQL showed the same state after the restart. |
| Pass / fail | **Pass** |
| Issue / diagnosis | No application defect. The first after-restart reading (19:20:31) followed the main run too closely to show a restart, so the API was stopped and started again and the check repeated. |
| Correction | None to the application |
| Retest outcome | Restart check repeated at 19:23:10: passed. The restart (Ctrl+C, then `dotnet run`) was performed manually and is not captured in the script output. |
| Evidence reference | `docs/evidence/T04.txt` (use the 19:23:10 "After API restart" section); `scripts/t04.ps1`. |

## T05: Multi-item checkout, borrowing limit and atomic rollback

| Field | Entry |
|---|---|
| Test ID / date / environment | T05, 2026-11-27. Same environment as T01; dataset reset before each step; fixed application date 2026-11-10. Failure injected with `EQUIPMENT_BORROWING_FAIL_BEFORE_COMMIT=1` for step 4 only. |
| Expected result | Multi-item success 201; exactly three active items allowed, over the limit 409; duplicates, one item or invalid date 400; missing reference 404; unavailable or restricted 409; injected failure 500; rejected and failed requests leave no rows or availability changes. |
| Observed result (first attempts) | Attempt 1 (19:29): steps 1 to 2c behaved as expected, then the API could not be reached for the step 3 requests. Attempt 2 (19:32): every checkout or issue that should have succeeded returned 500 and wrote nothing. |
| Pass / fail (first attempts) | **Fail** |
| Issue / diagnosis | Test procedure errors. Attempt 1: the API was stopped while the script was still running. Attempt 2: the failure injector was still switched on during the normal steps. |
| Correction | Cleared `EQUIPMENT_BORROWING_FAIL_BEFORE_COMMIT`, restarted the API and let the run finish before changing it. No application code changed. |
| Retest outcome | Steps 1 to 3 passed (19:39). Step 4 passed (19:44): 500 `server_error`, and PostgreSQL was identical before and after. **Pass** |
| Failure point | `EfBorrowingTransactions.CheckoutAsync`: after `SaveChangesAsync`, before `CommitAsync`. |
| Evidence reference | Retest: `docs/evidence/T05.txt`. Attempt 2: `docs/evidence/T05-failed-run.txt`. Attempt 1 output was overwritten and is not retained. Script: `scripts/t05.ps1`. |
