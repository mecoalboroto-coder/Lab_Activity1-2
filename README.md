# Equipment Borrowing System

Laboratory Activity 1 — ITSD 81: Desktop Application Development
*From Requirements to Application Structure*

A C#/.NET architectural foundation for a Campus Equipment Borrowing System.
No database and no GUI are implemented yet — this activity focuses purely
on structure and separation of concerns.

## 1. Solution Structure

| Project | Purpose |
|---|---|
| **EquipmentBorrowing.Domain** | The concepts and rules that belong to the problem itself, independent of any application or technology: `Student`, `Equipment`, `Borrowing`, `BorrowingStatus`. These classes protect their own invariants (e.g. a `Borrowing` cannot be returned twice) but know nothing about databases, UI, or use-case orchestration. |
| **EquipmentBorrowing.Application** | The operations/use cases the system performs, expressed as services (`BorrowEquipmentService`, `ReturnEquipmentService`) and the repository *interfaces* they depend on (`IStudentRepository`, `IEquipmentRepository`, `IBorrowingRepository`). This layer coordinates Domain objects and enforces cross-entity business rules (e.g. "has the student reached the max active borrowings?"). |
| **EquipmentBorrowing.Infrastructure** | Concrete, technology-specific implementations of the Application layer's interfaces. Currently contains `InMemory*Repository` classes backed by simple in-memory collections. This is where a future SQLite/EF Core implementation would live, without the Application layer changing at all. |
| **EquipmentBorrowing.ConsoleDemo** | A thin executable (Part H) that wires the concrete repositories to the service via constructor injection and prints a success case and several failure cases. Contains no business logic. |
| **EquipmentBorrowing.Tests** | xUnit tests for `BorrowEquipmentService`, covering the success path and each rule-failure path. |

## 2. Dependency Direction

```
        Executable / Future UI
        (ConsoleDemo today, Avalonia later)
                  │
                  ▼
             Application
              │       ▲
              ▼       │
             Domain    │
                       │
             Infrastructure
```

- **Application depends on Domain** (it manipulates `Student`, `Equipment`, `Borrowing`).
- **Application defines interfaces** (`IEquipmentRepository`, etc.) but does **not** depend on Infrastructure.
- **Infrastructure depends on Application** (to implement its interfaces) **and Domain** (to construct/persist domain objects) — the arrow points *inward*, satisfying the Dependency Inversion Principle.
- **The executable/UI depends on all three**, only to compose the object graph at startup (constructor injection in `Program.cs`).
- **Domain depends on nothing** — it is the stable center of the design.

This means Infrastructure and the UI can change freely without forcing changes in Application or Domain, because the dependency always points toward the business rules, never away from them.

## 3. Use Case Mapping

- **Actor:** Student
- **Use Case:** Borrow Equipment
- **Application Service:** `BorrowEquipmentService`
- **Domain Objects Used:** `Student`, `Equipment`, `Borrowing`, `BorrowingStatus`
- **Repository Interfaces Used:** `IStudentRepository`, `IEquipmentRepository`, `IBorrowingRepository`
- **Infrastructure Implementations Used:** `InMemoryStudentRepository`, `InMemoryEquipmentRepository`, `InMemoryBorrowingRepository`

## 4. Reflection

**1. Why should the application service depend on a repository interface instead of directly depending on a database implementation?**
Because the *use case* — "check eligibility, check availability, create a borrowing" — doesn't change based on where data lives. Depending on an interface lets `BorrowEquipmentService` be tested with an in-memory fake (as done in `EquipmentBorrowing.Tests`) and later switched to SQLite, all without touching the service's code. It also keeps a one-directional dependency: Infrastructure depends on Application, not the reverse.

**2. Which parts of your current solution could remain unchanged if SQLite were added later?**
`EquipmentBorrowing.Domain` and `EquipmentBorrowing.Application` would remain completely unchanged — including `BorrowEquipmentService` itself. Only `EquipmentBorrowing.Infrastructure` would change: the `InMemory*Repository` classes would be replaced (or supplemented) by classes implementing the same interfaces using EF Core/SQLite, and the composition code in `Program.cs` would point to the new classes instead.

**3. Which project would eventually contain Avalonia Views?**
A new UI-focused project (e.g. `EquipmentBorrowing.Desktop`), sitting at the same layer as `EquipmentBorrowing.ConsoleDemo` today — depending on Application (and Infrastructure, for composition) but not the other way around.

**4. Should an Avalonia button directly execute database queries? Why or why not?**
No. A button's click handler should call an Application service (e.g. `BorrowEquipmentService.BorrowAsync`), the same way `Program.cs` does today. If UI code executed queries directly, business rules would be duplicated or bypassed, and the query logic would become untestable without spinning up the UI — exactly the coupling this layered structure is designed to avoid.

**5. What part of your implementation represents the actual business operation requested by the actor?**
`BorrowEquipmentService.BorrowAsync` in the Application layer. Everything else — the Domain classes, the repository interfaces, the in-memory implementations, the console program — exists to support that one method's ability to safely coordinate the borrowing operation.

## Running the Demo

```bash
dotnet build
dotnet run --project src/EquipmentBorrowing.ConsoleDemo
```

## Running the Tests

```bash
dotnet test
```
