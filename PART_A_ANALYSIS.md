# Part A – Analyze the System Before Coding

## A. Actors

| Actor | What the actor expects the system to do |
|---|---|
| **Student** | Wants to check whether a piece of equipment is available, request to borrow it, and return it when finished. Expects the system to reject a request clearly if they are not eligible or the equipment is unavailable. |
| **Lab Staff / Equipment Custodian** | Wants the system to keep an accurate record of who currently holds each piece of equipment, so they can track overdue items and process returns. |

*(Only two actors are directly implied by the scenario. A future iteration could add an "Administrator" actor responsible for adding new equipment or adjusting a student's borrowing privileges, but nothing in the given scenario requires this yet, so it is not modeled in Part B–H.)*

## B. Use Cases

### Use Case 1

| Item | Description |
|---|---|
| Use Case | Borrow Equipment |
| Primary Actor | Student |
| Preconditions | Student is registered in the system; equipment record exists. |
| Main Action | Student requests to borrow a specific piece of equipment. |
| Expected Result | System verifies the student is allowed to borrow, the equipment exists and is available, and the student has not reached the maximum number of active borrowings. A new borrowing record is created with status `Active`, and the equipment becomes unavailable. |
| Possible Failure | Student is not allowed to borrow; equipment does not exist; equipment is already borrowed; student already has the maximum number of active borrowings. |

### Use Case 2

| Item | Description |
|---|---|
| Use Case | Return Equipment |
| Primary Actor | Student |
| Preconditions | An active borrowing record exists linking the student to the equipment. |
| Main Action | Student returns the borrowed equipment. |
| Expected Result | The borrowing record status changes to `Returned`, and the equipment becomes available again for other students. |
| Possible Failure | No active borrowing record exists for that student/equipment pair; equipment was already marked as returned. |

### Use Case 3

| Item | Description |
|---|---|
| Use Case | Find Available Equipment |
| Primary Actor | Student |
| Preconditions | None — any student may browse. |
| Main Action | Student requests a list of equipment that is currently available for borrowing. |
| Expected Result | System returns the set of equipment items whose availability status is `true`. |
| Possible Failure | No equipment is currently available (empty result is a valid, non-error outcome). |

## C. Domain Concepts

### Student

1. **Information it must contain:** an identifier, name, and an eligibility flag/status (whether the student is currently allowed to borrow — e.g. not suspended, no unresolved violations).
2. **Rules or state that belong to it:** whether the student is currently eligible to borrow. This is intrinsic to the student's own status and doesn't depend on any particular borrowing transaction.
3. **What it should *not* be responsible for:** it should not know how many items it currently has borrowed (that is derived from `Borrowing` records, not stored redundantly on `Student`), and it must not know anything about *how* equipment or borrowings are persisted.

### Equipment

1. **Information it must contain:** an identifier, a name/description, and an availability flag.
2. **Rules or state that belong to it:** whether it is currently available. Toggling this state (available → unavailable → available) is core equipment behavior.
3. **What it should *not* be responsible for:** it should not decide *whether a given student* is allowed to borrow it — that is a cross-cutting rule that depends on the student and the borrowing history, so it belongs in the application service, not in the `Equipment` class itself.

### Borrowing

1. **Information it must contain:** an identifier, the student who borrowed, the equipment borrowed, the date borrowed, the expected return date, and the current status (`Active`/`Returned`).
2. **Rules or state that belong to it:** the transition from `Active` to `Returned` is a legitimate behavior of the `Borrowing` record itself (e.g. a `MarkReturned()` method), since it only concerns the record's own state.
3. **What it should *not* be responsible for:** it should not validate borrowing eligibility (max active borrowings, student status, equipment availability) — those are cross-entity rules that require coordinating multiple objects and therefore belong in the `BorrowEquipmentService` (Application layer), not inside the `Borrowing` entity itself.
