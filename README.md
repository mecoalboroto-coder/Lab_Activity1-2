# Equipment Borrowing System

Laboratory Activities 1 & 2 — ITSD 81: Desktop Application Development

A C#/.NET application for a Campus Equipment Borrowing System, built in two stages:
**Laboratory Activity 1** established the architectural foundation (Domain, Application,
Infrastructure, Console demo, Tests). **Laboratory Activity 2** adds an Avalonia desktop
UI on top of that same foundation using the MVVM pattern.

---

# Part 1 — Laboratory Activity 1 (Architecture Foundation)

## 1. Solution Structure

| Project | Purpose |
|---|---|
| **EquipmentBorrowing.Domain** | The concepts and rules that belong to the problem itself, independent of any application or technology: `Student`, `Equipment`, `Borrowing`, `BorrowingStatus`. These classes protect their own invariants but know nothing about databases, UI, or use-case orchestration. |
| **EquipmentBorrowing.Application** | The operations/use cases the system performs, expressed as services (`BorrowEquipmentService`, `ReturnEquipmentService`) and the repository *interfaces* they depend on (`IStudentRepository`, `IEquipmentRepository`, `IBorrowingRepository`). This layer coordinates Domain objects and enforces cross-entity business rules. |
| **EquipmentBorrowing.Infrastructure** | Concrete, technology-specific implementations of the Application layer's interfaces. Currently contains `InMemory*Repository` classes backed by simple in-memory collections. |
| **EquipmentBorrowing.ConsoleDemo** | A thin executable that wires the concrete repositories to the services via constructor injection and prints a success case and several failure cases. Contains no business logic. |
| **EquipmentBorrowing.Tests** | xUnit tests for `BorrowEquipmentService` and `ReturnEquipmentService`, covering the success path and each rule-failure path. |

## 2. Dependency Direction

```
        Executable / UI
        (ConsoleDemo, and now Desktop)
                  │
                  ▼
             Application
              │       ▲
              ▼       │
             Domain    │
                       │
             Infrastructure
```

- **Application depends on Domain.**
- **Application defines interfaces** but does **not** depend on Infrastructure.
- **Infrastructure depends on Application and Domain** — the arrow points *inward*.
- **The executable/UI depends on all three**, only to compose the object graph at startup.
- **Domain depends on nothing** — it is the stable center of the design.

## 3. Use Case Mapping

- **Actor:** Student
- **Use Case:** Borrow Equipment
- **Application Service:** `BorrowEquipmentService`
- **Domain Objects Used:** `Student`, `Equipment`, `Borrowing`, `BorrowingStatus`
- **Repository Interfaces Used:** `IStudentRepository`, `IEquipmentRepository`, `IBorrowingRepository`
- **Infrastructure Implementations Used:** `InMemoryStudentRepository`, `InMemoryEquipmentRepository`, `InMemoryBorrowingRepository`

## 4. Reflection (Activity 1)

**1. Why should the application service depend on a repository interface instead of directly depending on a database implementation?**
Because the *use case* — "check eligibility, check availability, create a borrowing" — doesn't change based on where data lives. Depending on an interface lets `BorrowEquipmentService` be tested with an in-memory fake and later switched to SQLite, all without touching the service's code.

**2. Which parts of your current solution could remain unchanged if SQLite were added later?**
`EquipmentBorrowing.Domain` and `EquipmentBorrowing.Application` would remain completely unchanged. Only `EquipmentBorrowing.Infrastructure` would change, and the composition code would point to the new classes instead.

**3. Which project would eventually contain Avalonia Views?**
`EquipmentBorrowing.Desktop` — which is exactly what Laboratory Activity 2 added (see Part 2 below).

**4. Should an Avalonia button directly execute database queries? Why or why not?**
No. A button's click handler should call an Application service, the same way `Program.cs` does today. Direct queries from UI code would duplicate or bypass business rules and make that logic untestable without spinning up the UI.

**5. What part of your implementation represents the actual business operation requested by the actor?**
`BorrowEquipmentService.BorrowAsync` in the Application layer.

## Running the Console Demo

```bash
dotnet build
dotnet run --project src/EquipmentBorrowing.ConsoleDemo
```

## Running the Tests

```bash
dotnet test
```

---

# Part 2 — Laboratory Activity 2 (Avalonia UI and MVVM)

## 1. Desktop Project

`EquipmentBorrowing.Desktop` is the presentation layer added in this activity. It is
responsible for displaying information, collecting user input, holding presentation
state, invoking application operations through ViewModels, and giving the user
feedback. It references `EquipmentBorrowing.Application` and `EquipmentBorrowing.Infrastructure`
(to compose the dependency graph at startup) — it does **not** contain any business
rules of its own. `EquipmentBorrowing.Domain` and `EquipmentBorrowing.Application`
have no reference to Avalonia; they remain exactly as they were built in Activity 1.

The Desktop project contains:
- **`App.axaml` / `App.axaml.cs`** — the composition root. Registers repositories and
  services with `Microsoft.Extensions.DependencyInjection`, builds the service
  provider, seeds demo data, and creates the main window.
- **`MainWindow.axaml` / `.cs`** — hosts navigation and a content area that swaps
  between the Equipment and Active Borrowings views based on the current ViewModel.
- **`Views/`** — `EquipmentView.axaml` and `BorrowingsView.axaml`, pure XAML with
  bindings, no business logic.
- **`ViewModels/`** — `MainWindowViewModel`, `EquipmentViewModel`, `BorrowingsViewModel`,
  `BorrowingDisplayItem` (a UI-only display wrapper), built with `CommunityToolkit.Mvvm`.
- **`Styles/AppStyles.axaml`** — shared styles for buttons, headings, cards, and form panels.
- **`Converters/`** — small `IValueConverter` implementations used purely for display
  (status message color, visibility).

## 2. Updated Architecture

```
Avalonia View (EquipmentView.axaml / BorrowingsView.axaml)
        │
        │  Binding / Command
        ▼
ViewModel (EquipmentViewModel / BorrowingsViewModel)
        │
        │  Application Operation
        ▼
Application Service (BorrowEquipmentService / ReturnEquipmentService)
        │
        ├──────────► Domain (Student, Equipment, Borrowing)
        │
        ▼
Repository Interface (IStudentRepository, IEquipmentRepository, IBorrowingRepository)
        ▲
        │
Infrastructure Implementation (InMemory*Repository)
```

The Desktop project sits entirely above the Application layer in this diagram — it
depends inward, the same direction established in Activity 1. Nothing in Domain or
Application knows the Desktop project, or Avalonia, exists.

## 3. Borrow Equipment Flow

1. The user selects a **Student**, an **Equipment** item, and an **Expected Return Date**
   in `EquipmentView`, then presses **Borrow Equipment**.
2. This is bound to `EquipmentViewModel.BorrowCommand` (a `[RelayCommand]`-generated
   `IAsyncRelayCommand`), which runs `BorrowAsync()`.
3. `BorrowAsync()` first does **presentation validation only**: is a student selected,
   is equipment selected, is a return date chosen, is that date not in the past. If any
   of these fail, the ViewModel sets `StatusMessage`/`IsError` directly and returns —
   no application service is called.
4. If presentation validation passes, the ViewModel calls
   `_borrowEquipmentService.BorrowAsync(studentId, equipmentId, returnDate)` — the exact
   same `BorrowEquipmentService` written in Activity 1. It does not reimplement any of
   its rules (eligibility, availability, max active borrowings).
5. `BorrowEquipmentService` checks those rules against the repositories and either
   creates a `Borrowing` and marks the `Equipment` unavailable, or returns a `BorrowResult`
   describing exactly which rule failed.
6. Back in the ViewModel, the result is translated into a `StatusMessage` the user can
   read. On success, the equipment list is reloaded and a `BorrowingCompleted` event
   fires so `MainWindowViewModel` can refresh the Active Borrowings screen too.
7. The bound `StatusMessage`/`IsError` properties update the UI automatically through
   data binding — no manual UI manipulation happens in the ViewModel.

## 4. Return Equipment Flow

1. The user opens **Active Borrowings**, selects a borrowing from the list, and presses
   **Return Equipment**.
2. This is bound to `BorrowingsViewModel.ReturnCommand`, which runs `ReturnAsync()`.
3. Presentation validation checks only that a borrowing is selected.
4. The ViewModel calls `_returnEquipmentService.ReturnAsync(borrowingId)` — the
   `ReturnEquipmentService` from the Application layer, which locates the borrowing,
   checks it hasn't already been returned, marks it `Returned`, and marks the
   associated `Equipment` available again.
5. The result (success or a specific failure reason, e.g. "already returned" or "not
   found") comes back as a `ReturnResult` and is shown via `StatusMessage`.
6. On success, the Active Borrowings list is reloaded and a `ReturnCompleted` event
   fires so `MainWindowViewModel` refreshes the Equipment screen — the returned item
   now shows as available there too.

## 5. Architectural Reflection (Activity 2)

**1. Why should the View not call a repository directly?**
A View's only job is to display bound data and raise commands. If it called a
repository directly, it would need to know about persistence details and would bypass
the Application layer's business rules entirely — anyone could "borrow" equipment
straight from a button click handler without eligibility or availability being checked.

**2. Why should business rules not be implemented in the ViewModel?**
If a rule like "equipment must be available" lived in the ViewModel, it would be
duplicated wherever else that rule needs to apply (console demo, future API, tests),
and any drift between the copies would create inconsistent behavior. Keeping the rule
in one place — `BorrowEquipmentService` — means there's exactly one authority on
whether a borrowing is valid, regardless of which UI calls it.

**3. What is the responsibility of the ViewModel?**
To hold presentation state (selected items, status messages, observable collections),
expose commands the View can bind to, perform lightweight presentation-level validation
(e.g. "is a field empty"), and translate the outcome of an application service call into
something the View can display. It is a translator between the UI and the Application
layer, not a decision-maker about business rules.

**4. Why can the existing Application layer work without knowing that Avalonia is being used?**
Because `BorrowEquipmentService` and `ReturnEquipmentService` only depend on repository
interfaces and Domain types — nothing about Avalonia. The same services already ran
correctly from `EquipmentBorrowing.ConsoleDemo` in Activity 1 with zero changes needed
to support the Desktop project in Activity 2.

**5. What advantage is gained from registering dependencies in one composition point?**
A single composition root (`App.axaml.cs`) is the only place that needs to know which
concrete implementations back each interface. Every other class — ViewModels included —
only asks for interfaces/services through its constructor, so changing an implementation
(e.g. swapping in a different repository) means editing one file, not hunting through
the codebase for every place a repository was constructed.

**6. If the in-memory repository were replaced by SQLite later, which parts of the current interface should remain largely unchanged?**
Everything above the Infrastructure layer: `EquipmentBorrowing.Domain`,
`EquipmentBorrowing.Application` (including both services), and the entire
`EquipmentBorrowing.Desktop` project — Views, ViewModels, and styles. Only the
`InMemory*Repository` classes in `EquipmentBorrowing.Infrastructure` would be replaced
with EF Core/SQLite-backed implementations of the same interfaces, and the
`ConfigureServices` registrations in `App.axaml.cs` would point to the new classes.

## Running the Desktop App

```bash
dotnet build
dotnet run --project src/EquipmentBorrowing.Desktop
```

Demo data is seeded on startup:
- **3 students** — Juan Dela Cruz and Maria Santos (both allowed to borrow), and Pedro
  Reyes, who is seeded as *not* allowed to borrow, to demonstrate a handled
  business-rule failure directly from the UI.
- **4 equipment items** — Digital Multimeter, Soldering Iron, and Function Generator
  (available), and the Oscilloscope, seeded as already unavailable, to demonstrate the
  "equipment unavailable" failure without any extra setup steps.

This makes it possible to demonstrate a successful borrow, a failed borrow (try Pedro
Reyes, or try the Oscilloscope), a successful return, and the corresponding UI refresh
in both directions, all without touching the seed data.
