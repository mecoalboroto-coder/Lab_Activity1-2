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
