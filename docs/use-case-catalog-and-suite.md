# Use-Case Catalog and Suite — Laboratory Activity 4

Primary actor for every case: **Laboratory Staff / Equipment Custodian** (the custodian).
Supporting system: **PostgreSQL**. No include or extend relationships are used; internal validation is not a use case.

## 1. Catalog and classification

| ID | Use case | Lab 4 classification | Implemented in |
|---|---|---|---|
| UC-01 | View Equipment Catalog | Required continuation | Equipment screen |
| UC-02 | Find Available Equipment | Required continuation | Multi Checkout list (available items only); `IEquipmentRepository.GetAvailableAsync` |
| UC-03 | Borrow Equipment | Required continuation | Equipment screen, `BorrowEquipmentService` |
| UC-04 | View Active Borrowings | Required continuation | Active Borrowings screen |
| UC-05 | Return Equipment | Required continuation | Active Borrowings screen, `ReturnEquipmentService` |
| UC-06 | Check Out Multiple Equipment Items | Required new | Multi Checkout screen, `MultiCheckoutService` |
| UC-07 | View Student Borrowing History | Required new | Student History screen |
| UC-08 | View Equipment Borrowing History | Required new | Equipment History screen |
| UC-09 | Search, Filter, and Sort Borrowings | Required new | Search Borrowings screen |
| UC-10, UC-11 | Overdue / Current holder | Deferred to Laboratory Activity 5 | — |
| UC-12 | Register Student | Required new | Students screen, `StudentManagementService` |
| UC-13 | Update Student Information | Required new | Students screen |
| UC-14 | Change Student Borrowing Eligibility | Required new | Students screen (Permit / Restrict) |
| UC-15 | Register Equipment | Required new | Manage Equipment screen, `EquipmentManagementService` |
| UC-16 | Update Equipment Information | Required new | Manage Equipment screen |
| UC-17 – UC-27 | Retire, maintenance, restore, condition, renew, multi-return, due soon, active count, utilization, categories, void | Deferred to Laboratory Activity 5 | — |
| UC-28 – UC-33 | Reservations, approvals, audit, accounts, export, integrations | Future scope | — |

Notes on UC-02: the catalog screen lists all items with availability (UC-01). The available-only view is the item list in Multi Checkout, which loads `GetAvailableAsync`.

## 2. Common fields (identical in all fourteen cases)

| Field | Value |
|---|---|
| Level | User Goal |
| Primary Actor | Laboratory Staff / Equipment Custodian |
| Stakeholders and Interests | Custodian: accurate, understandable workflow. Students: correct records and preserved history. |
| Failure Guarantee | A rejected or known failed operation preserves existing data; an unknown commit outcome is handled under BR-18. (UC-06: a known failed or rejected transaction leaves no changes; if commit acknowledgement is lost the outcome is unknown until verified, with no partial commit.) |
| Includes / Extends | None required. |
| Open Issues | None. |

## 3. The fourteen cases

### UC-01 — View Equipment Catalog

| Field | Specification |
|---|---|
| Scope | Equipment catalog |
| Trigger | Staff opens the equipment catalog. |
| Preconditions | Application is running. |
| Success Guarantee | Current identifiers, names, descriptions and availability are displayed; no records change. |
| Main Success Scenario | 1. Staff opens the catalog. 2. System retrieves equipment from PostgreSQL. 3. System displays each item and availability. |
| Alternative Flows | 2a. No equipment: system displays an empty state. |
| Exception Flows | 2e. Database unavailable: system displays an error and a retry option. |
| Business Rules | BR-05–07, BR-10, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Catalog matches database records including availability; empty and error states are distinguishable. (A01) |
| Implementation | `EquipmentView`, `EquipmentViewModel.LoadAsync`, `EfEquipmentRepository.GetAllAsync` |

### UC-02 — Find Available Equipment

| Field | Specification |
|---|---|
| Scope | Equipment catalog |
| Trigger | Staff requests available equipment. |
| Preconditions | Application is running. |
| Success Guarantee | Only currently available items are shown; no records change. |
| Main Success Scenario | 1. Staff chooses the available-items view. 2. System retrieves current equipment state. 3. System displays only available items. |
| Alternative Flows | 3a. No items available: system shows an empty result ("No equipment is currently available."). |
| Exception Flows | 2e. Database failure: system shows an error and permits retry (Refresh). |
| Business Rules | BR-10, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Available items appear, unavailable items do not, and no-match and error states differ. (A02) |
| Implementation | `MultiCheckoutView`, `MultiCheckoutViewModel.LoadAsync`, `EfEquipmentRepository.GetAvailableAsync` |

### UC-03 — Borrow Equipment

| Field | Specification |
|---|---|
| Scope | Borrowing |
| Trigger | Staff submits a single-item borrowing. |
| Preconditions | Application is running; staff can select a student and item. |
| Success Guarantee | One active borrowing and the corresponding unavailable state are persisted together. |
| Main Success Scenario | 1. Staff selects a student, item and expected return date. 2. System revalidates student, limit, date and item. 3. System saves the active borrowing and availability change. 4. System confirms and refreshes. |
| Alternative Flows | 2a. Missing, ineligible, over-limit, unavailable or invalid-date request: system rejects with a reason and no change. |
| Exception Flows | 2e. Stale record or conflict: system rejects and refreshes. 3e. Database failure: system reports failure or unknown outcome under BR-18. |
| Business Rules | BR-08–11, BR-14–16, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Valid single issue persists; each invalid condition is rejected without changes; outcome survives restart. (A03) |
| Implementation | `BorrowEquipmentService` → `IBorrowingTransactions.CheckoutAsync` (one item), `CheckoutPolicy`, `EfBorrowingTransactions` |

### UC-04 — View Active Borrowings

| Field | Specification |
|---|---|
| Scope | Borrowing |
| Trigger | Staff opens active borrowings. |
| Preconditions | Application is running. |
| Success Guarantee | Only active records with related students and equipment are displayed; nothing changes. |
| Main Success Scenario | 1. Staff opens active borrowings. 2. System retrieves active records with student and equipment. 3. System displays identities and relevant dates. |
| Alternative Flows | 3a. No active records: system displays an empty state. |
| Exception Flows | 2e. Database failure: system reports an error and allows retry. |
| Business Rules | BR-14, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Active records and related identities are correct; returned records are excluded; empty and error states differ. (A04) |
| Implementation | `BorrowingsView`, `EfBorrowingRepository.GetActiveAsync` |

### UC-05 — Return Equipment

| Field | Specification |
|---|---|
| Scope | Borrowing |
| Trigger | Staff submits the return of a selected active borrowing. |
| Preconditions | An active borrowing can be selected. |
| Success Guarantee | The same borrowing is returned with a return date and the item becomes available, with history preserved. |
| Main Success Scenario | 1. Staff selects an active borrowing and confirms return. 2. System verifies its current status. 3. System records return and availability together. 4. System confirms and refreshes. |
| Alternative Flows | 2a. Already returned: system rejects the repeat without changing data. |
| Exception Flows | 2e. Missing or stale record: system reports the issue and refreshes. 3e. Database failure: system reports failure or unknown outcome under BR-18. |
| Business Rules | BR-10, BR-13–16, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Return persists across restart, equipment is available, history remains, repeat return is rejected. (A05) |
| Implementation | `ReturnEquipmentService` → `EfBorrowingTransactions.ReturnAsync` (row lock, status check, single commit) |

### UC-06 — Check Out Multiple Equipment Items

| Field | Specification |
|---|---|
| Scope | Borrowing |
| Trigger | Staff submits a multi-item checkout. |
| Preconditions | Application is running; student and equipment can be selected. |
| Success Guarantee | One active borrowing per selected item and all corresponding availability changes are committed together. |
| Main Success Scenario | 1. Staff selects a student, at least two items and an expected return date. 2. System revalidates references, eligibility, count, availability and date. 3. System commits all records and availability changes as one transaction. 4. System confirms and refreshes. |
| Alternative Flows | 2a. Duplicate or fewer than two items, invalid date, ineligible student, limit violation or unavailable item: system rejects with a reason; staff may correct. |
| Exception Flows | 2e. Stale or conflicting state: system rejects and refreshes. 3e. Known write failure: system rolls back and reports failure. 3f. Commit outcome unknown: system follows BR-18. |
| Business Rules | BR-08–12, BR-14–16, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Two valid items produce two persisted active records; each invalid request produces none; a controlled in-transaction failure leaves no partial changes. (A06) |
| Implementation | `MultiCheckoutService` (at least two items), `EfBorrowingTransactions.CheckoutAsync`, `IFailureInjector` test seam |

### UC-07 — View Student Borrowing History

| Field | Specification |
|---|---|
| Scope | Student history |
| Trigger | Staff selects a student and opens history. |
| Preconditions | Application is running; student selection is available. |
| Success Guarantee | Only the selected student's active and returned records and related equipment are shown; no data changes. |
| Main Success Scenario | 1. Staff selects a student. 2. System asynchronously retrieves related borrowings and equipment. 3. System displays student identity, item, checkout, expected return, status and actual return when present. |
| Alternative Flows | 3a. No history: system displays an empty state. 1a. Staff selects another student: system replaces results. |
| Exception Flows | 2e. Missing or stale student or database failure: system explains the problem and allows reselection or retry. |
| Business Rules | BR-14, BR-18–19 |
| Special Requirements | PostgreSQL-backed asynchronous loading; responsive window; loading and error states. |
| Acceptance Criteria | Two students' records do not mix; active, returned and empty results are accurate; loading is responsive. (A07) |
| Implementation | `StudentHistoryView/ViewModel`, `BorrowingHistoryService.ForStudentAsync`, `EfBorrowingQueries.GetByStudentAsync` |

### UC-08 — View Equipment Borrowing History

| Field | Specification |
|---|---|
| Scope | Equipment history |
| Trigger | Staff selects equipment and opens history. |
| Preconditions | Application is running; equipment selection is available. |
| Success Guarantee | Only the selected item's active and returned records and related students are shown; no data changes. |
| Main Success Scenario | 1. Staff selects an item. 2. System asynchronously retrieves its borrowings and students. 3. System displays equipment identity, student, checkout, expected return, status and actual return when present. |
| Alternative Flows | 3a. No history: system displays an empty state. 1a. Staff selects another item: system replaces the details. |
| Exception Flows | 2e. Missing or stale item or database failure: system explains the problem and allows reselection or retry. |
| Business Rules | BR-14, BR-18–19 |
| Special Requirements | PostgreSQL-backed asynchronous loading; responsive window; loading and error states. |
| Acceptance Criteria | Two items' records do not mix; active, returned and empty results are accurate; loading is responsive. (A08) |
| Implementation | `EquipmentHistoryView/ViewModel`, `BorrowingHistoryService.ForEquipmentAsync`, `EfBorrowingQueries.GetByEquipmentAsync` |

### UC-09 — Search, Filter, and Sort Borrowings

| Field | Specification |
|---|---|
| Scope | Borrowing queries |
| Trigger | Staff enters criteria or opens the borrowing list. |
| Preconditions | Application is running. |
| Success Guarantee | Only matching records are shown in the requested order; no data changes. |
| Main Success Scenario | 1. Staff opens borrowings and chooses search text, status filter and/or sort. 2. System asynchronously loads matching related records. 3. System displays student, equipment, dates and status in the selected order. 4. Staff may clear criteria. |
| Alternative Flows | 1a. Criteria may be used separately or together. 3a. No matches: system shows an empty result. 4a. Clear restores the full list. |
| Exception Flows | 2e. Database failure: system reports an error and offers retry. |
| Business Rules | BR-17–19 |
| Special Requirements | PostgreSQL-backed asynchronous loading; responsive window; loading and error states. |
| Acceptance Criteria | Search each specified field, each status, both sort fields and directions, combined criteria and reset against known data. (A09) |
| Implementation | `BorrowingSearchView/ViewModel`, `EfBorrowingQueries.SearchAsync` (`ILIKE` with escaped wildcards, borrowing id tie-breaker, 300 ms debounce, version-tagged loads) |

### UC-12 — Register Student

| Field | Specification |
|---|---|
| Scope | Student management |
| Trigger | Staff submits a new student. |
| Preconditions | Application is running; registration form is accessible. |
| Success Guarantee | One uniquely identified student with the supplied details and default eligibility is persisted. |
| Main Success Scenario | 1. Staff enters institutional number, full name and optional contact. 2. System validates required fields and uniqueness. 3. System saves the student. 4. System confirms and displays the saved record. |
| Alternative Flows | 2a. Missing or blank required data, or duplicate normalized number: system rejects and identifies the problem. 1a. Optional contact omitted: stored as absent. |
| Exception Flows | 3e. Concurrent duplicate or database failure: system reports failure or unknown outcome under BR-18; no duplicate is created. |
| Business Rules | BR-01–02, BR-16, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Valid student can be retrieved after restart; empty name or number and duplicate number are rejected. (A12) |
| Implementation | `StudentManagementService.RegisterAsync`, `Student` constructor, `UX_Students_StudentNumber_Normalized`, `23505` mapped to a duplicate message |

### UC-13 — Update Student Information

| Field | Specification |
|---|---|
| Scope | Student management |
| Trigger | Staff submits changes to a selected student. |
| Preconditions | A student exists and can be located and selected. |
| Success Guarantee | Allowed changes persist for the same student; borrowing relationships and history remain intact. |
| Main Success Scenario | 1. Staff finds and selects a student. 2. Staff edits permitted fields and submits. 3. System validates and saves. 4. System confirms and displays the updated record. |
| Alternative Flows | 2a. Optional contact left blank: system clears it. 3a. Blank required field or duplicate number: system rejects without changes. |
| Exception Flows | 3e. Student no longer exists, conflicting update or database failure: system reports the issue and refreshes or retries under BR-18. |
| Business Rules | BR-01–03, BR-14, BR-16, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Locate and update a student, restart, and verify changed details and unchanged linked borrowings; reject invalid and duplicate edits. (A13) |
| Implementation | `StudentManagementService.UpdateAsync`, `Student.UpdateDetails`; search box locates students |

### UC-14 — Change Student Borrowing Eligibility

| Field | Specification |
|---|---|
| Scope | Student management |
| Trigger | Staff requests a change to a student's eligibility. |
| Preconditions | A student exists and can be selected. |
| Success Guarantee | Requested eligibility is persisted; existing active borrowings and history are unchanged. |
| Main Success Scenario | 1. Staff finds and selects a student. 2. Staff chooses permit or restrict and submits. 3. System saves and confirms the current eligibility. |
| Alternative Flows | 2a. Requested state already set: system informs staff and makes no change. 3a. A restricted student with active loans may still return them. |
| Exception Flows | 3e. Student missing or stale, or database failure: system reports failure or unknown outcome under BR-18. |
| Business Rules | BR-04, BR-08, BR-13–14, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Restriction persists and blocks a new issue without changing active loans; return remains possible; permitting allows an otherwise valid request. (A14) |
| Implementation | `StudentManagementService.SetEligibilityAsync` (`EligibilityChange.Unchanged` gives the "already ..." message); return path does not check eligibility |

### UC-15 — Register Equipment

| Field | Specification |
|---|---|
| Scope | Equipment management |
| Trigger | Staff submits a new physical item. |
| Preconditions | Application is running; registration form is accessible. |
| Success Guarantee | One uniquely identified item is persisted as available with no active borrowing. |
| Main Success Scenario | 1. Staff enters an identifier, name and optional description. 2. System validates required fields and uniqueness. 3. System saves the item with initial availability. 4. System confirms and displays it. |
| Alternative Flows | 2a. Required field blank or normalized identifier duplicate: system rejects and identifies the issue. 1a. Optional description omitted: stored as absent. |
| Exception Flows | 3e. Concurrent duplicate or database failure: system reports failure or unknown outcome under BR-18; no duplicate is created. |
| Business Rules | BR-05–06, BR-10, BR-16, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Valid item appears as available after restart; missing required fields and duplicate identifier are rejected. (A15) |
| Implementation | `EquipmentManagementService.RegisterAsync`, `Equipment` constructor (available by default), `UX_Equipment_Identifier_Normalized` |

### UC-16 — Update Equipment Information

| Field | Specification |
|---|---|
| Scope | Equipment management |
| Trigger | Staff submits changes to selected equipment. |
| Preconditions | An item exists and can be located and selected. |
| Success Guarantee | Allowed descriptive changes persist for the same item; availability and borrowing history remain intact. |
| Main Success Scenario | 1. Staff finds and selects an item. 2. Staff edits permitted identifier, name or description and submits. 3. System validates and saves. 4. System confirms and displays updated details. |
| Alternative Flows | 2a. Staff clears optional description: stored as absent. 3a. Blank required field or duplicate identifier: system rejects without changes. 2b. Availability and borrowing state cannot be edited as descriptive fields. |
| Exception Flows | 3e. Item missing or stale, conflicting update or database failure: system reports the issue and refreshes or retries under BR-18. |
| Business Rules | BR-05–07, BR-10, BR-14, BR-16, BR-18–19 |
| Special Requirements | PostgreSQL persistence; meaningful feedback; separation of responsibilities. |
| Acceptance Criteria | Edit and restart preserves new details and linked history and availability; duplicate and blank required edits are rejected. (A16) |
| Implementation | `EquipmentManagementService.UpdateAsync`, `Equipment.UpdateDetails` (no availability field in the form or the method) |
