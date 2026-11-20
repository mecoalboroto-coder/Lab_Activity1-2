# Traceability and Evidence — Laboratory Activity 4

Status key: **Captured** = evidence exists in `docs/evidence` or the repo. **Partial** = some scenarios shown, others still to capture. **To capture** = not yet done.
Every database readback uses a fresh `psql` connection, separate from the application.

## 1. Use case to implementation to acceptance

| Use case | Rubric | Implementation | Automated tests | Acceptance |
|---|---|---|---|---|
| UC-01 View catalog | R1 | `EquipmentView`, `EfEquipmentRepository.GetAllAsync` | — | A01 |
| UC-02 Find available | R1 | `GetAvailableAsync`, Multi Checkout list | — | A02 |
| UC-03 Borrow | R1, R2 | `BorrowEquipmentService`, `EfBorrowingTransactions`, `CheckoutPolicy` | `BorrowEquipmentServiceTests` (7) | A03 |
| UC-04 Active borrowings | R1 | `EfBorrowingRepository.GetActiveAsync` | — | A04 |
| UC-05 Return | R1, R2 | `ReturnEquipmentService`, `EfBorrowingTransactions.ReturnAsync` | `ReturnEquipmentServiceTests` (3) | A05 |
| UC-06 Multi checkout | R3 | `MultiCheckoutService`, `EfBorrowingTransactions`, `IFailureInjector` | `MultiCheckoutServiceTests` (10) | A06 |
| UC-07 Student history | R6, R7 | `StudentHistoryViewModel`, `BorrowingHistoryService` | `BorrowingHistoryServiceTests` (5, shared) | A07 |
| UC-08 Equipment history | R6, R7 | `EquipmentHistoryViewModel`, `BorrowingHistoryService` | shared | A08 |
| UC-09 Search/filter/sort | R6, R7 | `BorrowingSearchViewModel`, `EfBorrowingQueries.SearchAsync` | — | A09 |
| UC-12 Register student | R4, R2 | `StudentManagementService.RegisterAsync` | — | A12 |
| UC-13 Update student | R4 | `StudentManagementService.UpdateAsync` | — | A13 |
| UC-14 Eligibility | R4 | `StudentManagementService.SetEligibilityAsync` | — | A14 |
| UC-15 Register equipment | R5, R2 | `EquipmentManagementService.RegisterAsync` | — | A15 |
| UC-16 Update equipment | R5 | `EquipmentManagementService.UpdateAsync` | — | A16 |

Total automated tests: 25 (in-memory repositories; PostgreSQL behavior is evidenced below).

## 2. Business rule to enforcement

| Rule | Application check | PostgreSQL enforcement |
|---|---|---|
| BR-01, BR-05 unique normalized identity | `NumberExistsAsync` / `IdentifierExistsAsync` | `UX_Students_StudentNumber_Normalized`, `UX_Equipment_Identifier_Normalized` |
| BR-02, BR-06 required, non-blank | domain constructors | `CK_*_NotBlank` |
| BR-09 limit of three | `CheckoutPolicy` | (application, under row locks) |
| BR-10 one active per item | availability check under `FOR UPDATE` | `UX_Borrowings_EquipmentId_Active` |
| BR-11 return later than checkout | `CheckoutPolicy`, `Borrowing` constructor | `CK_Borrowings_ExpectedReturn_After_Borrowed` |
| BR-13 return transition | `ReturnAsync` status and date checks | `CK_Borrowings_Status_ReturnedAt_Consistent` |
| BR-14 history, relationships | no delete operations | foreign keys `ON DELETE RESTRICT` |
| BR-15 revalidation, atomicity | revalidate inside the transaction | single transaction, rollback |
| BR-17 search | `ILIKE`, escaped wildcards, id tie-breaker | — |
| BR-18 unknown outcome | `OutcomeUnknown` results, no retry | — |

## 3. Evidence table A01–A19

| ID | Scenario | Status | Evidence / what remains |
|---|---|---|---|
| A01 | Catalog vs database; empty; failed connection | Partial | Seed readback matches the equipment list (screenshot). Remaining: stop-service error screenshot. |
| A02 | Available only; none; failed load | Partial | Multi Checkout list shows only available items (screenshots). Remaining: empty state, failed load. |
| A03 | Issue one; reject ineligible, missing, unavailable, over-limit, invalid date | Partial | Success with readback (borrowing rows 2–4). Remaining: Pedro, Oscilloscope rejections with unchanged readback. |
| A04 | Active list, empty, error | Partial | Active Borrowings screenshots; returned row excluded. Remaining: error state. |
| A05 | Return; repeat; stale | Partial | Return success screenshot and readback (`Returned`, `ReturnedAt` set, item `t`). Remaining: repeat-return rejection, restart readback. |
| A06 | Multi checkout; rejections; forced in-transaction failure | Mostly captured | `a06-before/after/recovered.txt`, failure and success screenshots, sequence 5 → 7 proves rollback after writes. Automated rejection tests. Remaining: UI screenshots of each rejection; two-window conflict. |
| A07 | Student history | Partial | Screenshots for Juan, Maria, Ana (empty). Remaining: failed load and recovery, missing student. |
| A08 | Equipment history | Partial | Screenshots for Multimeter, Oscilloscope, Function Generator (empty). Remaining: failed load, missing item. |
| A09 | Search/filter/sort | Partial | `juan`, `2024-0002`, Active filter, ordering matches `psql`. Remaining: equipment-name search, Returned, all four sorts, combined plus Clear, no match, `_` wildcard, connection error. |
| A12 | Register student | Captured | Duplicate rejected in UI; raw inserts rejected by `UX_Students_StudentNumber_Normalized` and `CK_Students_StudentNumber_NotBlank`; count unchanged. |
| A13 | Update student | Partial | Juan's contact cleared (readback). Remaining: duplicate-number edit and blank-field rejection screenshots, linked borrowings unchanged. |
| A14 | Eligibility | Partial | Restrict persisted (readback). Remaining: restricted student rejected on borrow, return still works, repeat feedback, permit. |
| A15 | Register equipment | Captured | `EQ-200` available with null description; duplicate and blank inserts rejected by the database; count unchanged. |
| A16 | Update equipment | Captured | Oscilloscope renamed while borrowed; borrowing still `Active`, availability `f`. |
| A17 | Clean migration; invalid FK, duplicates, second active borrowing | Partial | Migration applied to a clean database; `\d` output shows all constraints; duplicate identifier and student number rejected. Remaining: invalid foreign key and second active borrowing (commands below). |
| A18 | Failure and recovery; layers and lifetimes | Partial | Injected failure then retry succeeded; README sections 4.8–4.9. Remaining: stop-service disconnect and retry, code walkthrough. |
| A19 | Documents match implementation; README steps | To capture | Follow the README on a clean database; fill the Team line. |

## 4. Remaining database captures (A17)

Run on the **disposable** database. Both statements must fail and the count must not change.

```powershell
@'
INSERT INTO "Borrowings" ("StudentId","EquipmentId","DateBorrowed","ExpectedReturnDate","Status") VALUES (999,1,'2026-11-19','2026-11-26','Active');
INSERT INTO "Borrowings" ("StudentId","EquipmentId","DateBorrowed","ExpectedReturnDate","Status") VALUES (1,2,'2026-11-19','2026-11-26','Active');
SELECT count(*) FROM "Borrowings";
'@ | psql -U postgres -h localhost -d equipment_borrowing_test 2>&1 | Out-File -Encoding utf8 docs\evidence\a17-fk-and-second-active.txt
Get-Content docs\evidence\a17-fk-and-second-active.txt
```

Expected: a foreign-key violation on `FK_Borrowings_Students_StudentId` and a unique violation on `UX_Borrowings_EquipmentId_Active`.

## 5. Integrated demonstration sequence

Follow the nine-step sequence in the activity (student foundation, equipment foundation, cross-workflow borrowing, eligibility and return, coordinated checkout, invalid and failed checkout, related histories, retrieval controls, restart and recovery) on a freshly created database, and record each step in `docs/evidence`.
