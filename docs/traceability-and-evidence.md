# Traceability and Evidence — Laboratory Activity 4

Evidence files are in [`docs/evidence`](evidence). Every database readback used a fresh `psql` connection, separate from the application. Status key: **Captured** = shown in the evidence; **Partial** = some scenarios shown, the rest listed as remaining.

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

## 3. Evidence A01–A19

| ID | Status | Evidence | Remaining |
|---|---|---|---|
| A01 | Partial | [catalog](evidence/a01-catalog.png) | Catalog-screen error state (error states are shown on the history and search screens, A07 and A09). |
| A02 | Partial | Multi Checkout lists only available items: [one item](evidence/a06-one-item-rejected.png), [checkout success](evidence/a06-checkout-success.png) | Empty and error states. |
| A03 | Captured (UI) | [success](evidence/a03-borrow-success.png), [restricted student](evidence/a03-pedro-rejected.png), [unavailable item](evidence/a03-unavailable-rejected.png), [over limit](evidence/a06-over-limit-rejected.png) | Invalid date is covered by automated tests only. |
| A04 | Partial | [active list](evidence/a04-active-borrowings.png) | Error state. |
| A05 | Partial | [return](evidence/a05-return-success.png), [item available again](evidence/a05-equipment-available-after-return.png), [final readback](evidence/a19-final-readback.txt) | A repeat return cannot be submitted from the UI because the row leaves the list; stale selection not shown. |
| A06 | Captured | [before](evidence/a06-before.txt), [after](evidence/a06-after.txt), [recovered](evidence/a06-recovered.txt); [failure message](evidence/a06-failure-message.png), [success](evidence/a06-checkout-success.png); rejections: [one item](evidence/a06-one-item-rejected.png), [over limit](evidence/a06-over-limit-rejected.png), [ineligible](evidence/a06-ineligible-rejected.png). Rollback proof: tables unchanged while `Borrowings_Id_seq` advanced 5 to 7. | Duplicate, unavailable-item, missing-item and past-date requests are covered by automated tests. |
| A07 | Captured | [Juan](evidence/a07-student-history-juan.png), [Maria](evidence/a07-student-history-maria.png), [no history](evidence/a07-student-history-empty.png), [failed load](evidence/a07-history-db-error.png) | Missing-student state is covered by a test. |
| A08 | Partial | [Multimeter](evidence/a08-equipment-history-multimeter.png), [Oscilloscope](evidence/a08-equipment-history-oscilloscope.png), [no history](evidence/a08-equipment-history-empty.png) | Failed-load screenshot. |
| A09 | Mostly captured | [all](evidence/a09-search-all.png), [name](evidence/a09-search-juan.png), [student number](evidence/a09-search-student-number.png), [equipment name](evidence/a09-search-equipment-solder.png), [Active](evidence/a09-filter-active.png), [Returned](evidence/a09-filter-returned.png), [no match](evidence/a09-search-no-match.png), [sort checkout asc](evidence/a09-sort-checkout-asc.png), [sort checkout desc](evidence/a09-sort-checkout-desc.png), [connection error](evidence/a09-search-db-error.png) | Expected-return sort, combined criteria with Clear, `_` wildcard. |
| A12 | Partial | [registered](evidence/a12-student-registered.png), [blank number rejected](evidence/a12-blank-student-rejected.png), constraints in [a17-schema.txt](evidence/a17-schema.txt) | UI screenshot and saved output of the duplicate rejection. |
| A13 | Partial | [before](evidence/a13-student-before-update.png), [updated, contact cleared](evidence/a13-student-updated.png) | Duplicate-number and blank-field rejection on edit. |
| A14 | Partial | [restrict](evidence/a14-restrict.png), [restrict Ana](evidence/a14-restrict-ana.png), [repeat restrict](evidence/a14-repeat-restrict.png), [repeat permit](evidence/a14-repeat-permit.png), [restricted borrow rejected](evidence/a14-restricted-borrow-rejected.png) | The Permit action itself and a borrow after it; returning a loan as a restricted student. |
| A15 | Partial | [registered](evidence/a15-equipment-registered.png), [duplicate rejected](evidence/a15-duplicate-rejected.png) | Blank-field rejection. |
| A16 | Captured | [before](evidence/a16-equipment-before-edit.png), [edited](evidence/a16-equipment-edited.png), [history and availability kept after rename](evidence/a16-history-kept-after-rename.png) | — |
| A17 | Captured | [schema and constraints](evidence/a17-schema.txt), [invalid foreign key and second active borrowing](evidence/a17-fk-and-second-active.txt) | — |
| A18 | Captured | [stop](evidence/a18-stop-service.png), [error](evidence/a18-error-while-stopped.png), [start](evidence/a18-start-service.png), [recovered](evidence/a18-recovered.png); injected failure and retry under A06; README 4.8 and 4.9 | — |
| A19 | Partial | [after restart: active borrowings](evidence/a19-restart-active.png), [after restart: students](evidence/a19-restart-students.png), [final readback](evidence/a19-final-readback.txt) | Keep this table in step with the evidence before submitting. |

## 4. Remaining captures

1. **A14:** Restrict then Permit Ana and capture "now permitted", then borrow as her.
2. **A09:** Sort by Expected return date, combine criteria and press Clear, type `_`.
3. **A12, A13, A15:** Capture the duplicate-student, duplicate-edit and blank-equipment rejections.
4. **A08:** Equipment History with PostgreSQL stopped.

## 5. Integrated demonstration sequence

The nine-step sequence in the activity was run on a freshly created database; the evidence above records each step.
