# Equipment Borrowing System

Laboratory Activities 1, 2 & 3 — ITSD 81: Desktop Application Development

A C#/.NET application for a Campus Equipment Borrowing System, built in three stages:
**Laboratory Activity 1** established the architectural foundation (Domain, Application,
Infrastructure, Console demo, Tests). **Laboratory Activity 2** added an Avalonia desktop
UI on top of that same foundation using the MVVM pattern. **Laboratory Activity 3**
replaced the temporary in-memory persistence with a real SQLite database via Entity
Framework Core — without changing the architecture established in the first two activities.

---

# Part 1 — Laboratory Activity 1 (Architecture Foundation)

## 1. Solution Structure

| Project | Purpose |
|---|---|
| **EquipmentBorrowing.Domain** | The concepts and rules that belong to the problem itself, independent of any application or technology: `Student`, `Equipment`, `Borrowing`, `BorrowingStatus`. |
| **EquipmentBorrowing.Application** | The operations/use cases the system performs: `BorrowEquipmentService`, `ReturnEquipmentService`, and the repository *interfaces* they depend on. |
| **EquipmentBorrowing.Infrastructure** | Concrete implementations of the Application layer's interfaces. |
| **EquipmentBorrowing.ConsoleDemo** | A thin executable demonstrating the core workflows without a GUI. |
| **EquipmentBorrowing.Tests** | xUnit tests for `BorrowEquipmentService` and `ReturnEquipmentService`. |

## 2. Dependency Direction
    Executable / UI
    (ConsoleDemo, Desktop)
              │
              ▼
         Application
          │       ▲
          ▼       │
         Domain    │
                   │
         Infrastructure


Domain depends on nothing. Application depends on Domain and defines interfaces, but
never depends on Infrastructure. Infrastructure depends inward, implementing those
interfaces. This direction is what let Activity 3 replace the data store without
touching Domain or Application at all.

## 3. Use Case Mapping

- **Actor:** Student
- **Use Case:** Borrow Equipment
- **Application Service:** `BorrowEquipmentService`
- **Domain Objects Used:** `Student`, `Equipment`, `Borrowing`, `BorrowingStatus`
- **Repository Interfaces Used:** `IStudentRepository`, `IEquipmentRepository`, `IBorrowingRepository`

## Running the Console Demo

```bash
dotnet build
dotnet run --project src/EquipmentBorrowing.ConsoleDemo
```

---

# Part 2 — Laboratory Activity 2 (Avalonia UI and MVVM)

## 1. Desktop Project

`EquipmentBorrowing.Desktop` is the presentation layer. It references
`EquipmentBorrowing.Application` and `EquipmentBorrowing.Infrastructure` only to compose
the dependency graph at startup — it contains no business rules of its own.
`EquipmentBorrowing.Domain` and `EquipmentBorrowing.Application` have no reference to
Avalonia.

- **`App.axaml` / `App.axaml.cs`** — the composition root.
- **`MainWindow.axaml` / `.cs`** — navigation between the two screens.
- **`Views/`** — `EquipmentView.axaml`, `BorrowingsView.axaml` — pure XAML and bindings.
- **`ViewModels/`** — built with `CommunityToolkit.Mvvm`.
- **`Styles/AppStyles.axaml`** — shared styling.
- **`Converters/`** — small, display-only `IValueConverter`s.

## 2. Borrow / Return Flow (unchanged shape since Activity 2)


In Activity 2 the `[persistence]` box was `InMemory*Repository`. In Activity 3 it became
`Ef*Repository → DbContext → SQLite`, and nothing above the repository interface line
had to change.

## Running the Desktop App

```bash
dotnet build
dotnet run --project src/EquipmentBorrowing.Desktop
```

---

# Part 3 — Laboratory Activity 3 (SQLite and Entity Framework Core)

## 1. Relational Database Design

See `docs/database-diagram.png` for the full diagram. Summary:

**Students**
| Column | Type | Notes |
|---|---|---|
| StudentId | int | **PK**, assigned by the application (not DB-generated) |
| Name | nvarchar(200) | required |
| IsAllowedToBorrow | bit | required |

**Equipment**
| Column | Type | Notes |
|---|---|---|
| EquipmentId | int | **PK**, assigned by the application |
| Name | nvarchar(200) | required, **unique** |
| Description | nvarchar(500) | optional |
| IsAvailable | bit | required |

**Borrowings**
| Column | Type | Notes |
|---|---|---|
| BorrowingId | int | **PK**, assigned by the application |
| StudentId | int | **FK → Students.StudentId**, required |
| EquipmentId | int | **FK → Equipment.EquipmentId**, required |
| DateBorrowed | date | required |
| ExpectedReturnDate | date | required |
| ReturnedAt | date | optional (null until returned) |
| Status | varchar(20) | required, stored as text (`Active`/`Returned`) |

**Relationships:** one Student → many Borrowings; one Equipment item → many Borrowings
over its lifetime (one piece of equipment can be borrowed, returned, and borrowed again).
A Borrowing belongs to exactly one Student and exactly one Equipment item. Both foreign
keys use `DeleteBehavior.Restrict` — a Student or Equipment record with existing
borrowing history cannot be deleted out from under those records.

**Why Borrowings references Student/Equipment instead of duplicating their data:**
storing a student's full name and eligibility on every borrowing row would mean that
updating a student's name (or suspending them) would require rewriting every historical
borrowing row to stay consistent. A foreign key keeps that information in exactly one
place.

**Indexes:** `Equipment.Name` (unique), `Students.Name`, `Borrowings.StudentId`,
`Borrowings.EquipmentId`, `Borrowings.Status` — the last three support the exact lookups
`BorrowEquipmentService` and the Active Borrowings screen already perform.

## 2. SQLite and Entity Framework Core

The `EquipmentBorrowing.Infrastructure` project references
`Microsoft.EntityFrameworkCore.Sqlite` (the SQLite provider) and
`Microsoft.EntityFrameworkCore.Design` (migration tooling). `EquipmentBorrowing.Desktop`
also references the SQLite provider package, since it is the composition root that
calls `UseSqlite(...)` when registering the `DbContext`.

The connection string used is `Data Source=equipmentborrowing.db`, a local SQLite file
created in the application's working directory the first time the app runs.

## 3. DbContext

`EquipmentBorrowingDbContext` (in `Infrastructure/Persistence/`) exposes three `DbSet`s —
`Students`, `Equipment`, `Borrowings` — and applies every `IEntityTypeConfiguration<T>`
class found in the assembly via `ApplyConfigurationsFromAssembly`. Its only responsibility
is representing the application's database and translating LINQ queries against it into
SQL; it contains no business rules and is never referenced outside the Infrastructure
project.

A `DesignTimeDbContextFactory` also exists purely so the `dotnet ef` command-line tool
can construct a `DbContext` instance at migration-design time, without needing to run
the full Avalonia application.

## 4. Repository Transition

Before (Activity 2):

After (Activity 3), for the Desktop application specifically:


**The interfaces themselves (`IStudentRepository`, `IEquipmentRepository`,
`IBorrowingRepository`) did not change at all** between Activity 2 and Activity 3 — this
is the entire point of the repository abstraction established in Activity 1.
`BorrowEquipmentService` and `ReturnEquipmentService` are byte-for-byte unchanged except
for one line (`MarkReturned` now takes a `DateOnly` to record when the return happened).

The `InMemory*Repository` classes were **not deleted** — they still back
`EquipmentBorrowing.ConsoleDemo` and the unit tests in `EquipmentBorrowing.Tests`, which
intentionally avoid touching a real database so they stay fast and isolated. Only the
Desktop application's dependency injection was changed to use the `Ef*` implementations.

Each `Ef*Repository` takes an `IDbContextFactory<EquipmentBorrowingDbContext>` rather
than a `DbContext` directly, and creates a short-lived context per repository call. This
avoids a single long-lived `DbContext` being shared across the application's entire
lifetime (a common source of stale-state and threading bugs in desktop apps), since
ViewModels in this app are resolved once and held for the life of the window.

## 5. Migration Process

The initial migration was created and applied with:

```bash
dotnet tool install --global dotnet-ef   # once, if not already installed
dotnet ef migrations add InitialCreate --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Desktop
dotnet ef database update --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Desktop
```

In addition, the application calls `context.Database.Migrate()` automatically on
startup (`App.axaml.cs`), so the database schema is also brought up to date
automatically the first time the compiled application runs on a machine that has never
had the database file before — migrations are never bypassed by hand-creating tables.

## 6. Generated SQL

**Query 1 — Filtering (read-only)**

```csharp
// EfEquipmentRepository.GetAvailableAsync
return await context.Equipment
    .AsNoTracking()
    .Where(e => e.IsAvailable)
    .OrderBy(e => e.Id)
    .ToListAsync(cancellationToken);
```

Representative generated SQL (SQLite, EF Core 8):

```sql
SELECT "e"."Id", "e"."Description", "e"."IsAvailable", "e"."Name"
FROM "Equipment" AS "e"
WHERE "e"."IsAvailable" = 1
ORDER BY "e"."Id";
```

*Explanation:* the LINQ `Where` becomes a SQL `WHERE` clause, and `OrderBy` becomes
`ORDER BY`. Because the query used `AsNoTracking()`, EF Core does not need to compute
a shadow copy for change-tracking, so no additional bookkeeping is attached to the
generated plan.

**Query 2 — Join via eager loading (read-only)**

```csharp
// EfBorrowingRepository.GetActiveAsync
return await context.Borrowings
    .AsNoTracking()
    .Include(b => b.Student)
    .Include(b => b.Equipment)
    .Where(b => b.Status == BorrowingStatus.Active)
    .OrderBy(b => b.Id)
    .ToListAsync(cancellationToken);
```

Representative generated SQL (SQLite, EF Core 8):

```sql
SELECT "b"."Id", "b"."DateBorrowed", "b"."EquipmentId", "b"."ExpectedReturnDate",
       "b"."ReturnedAt", "b"."Status", "b"."StudentId",
       "s"."Id", "s"."IsAllowedToBorrow", "s"."Name",
       "e"."Id", "e"."Description", "e"."IsAvailable", "e"."Name"
FROM "Borrowings" AS "b"
INNER JOIN "Students" AS "s" ON "b"."StudentId" = "s"."Id"
INNER JOIN "Equipment" AS "e" ON "b"."EquipmentId" = "e"."Id"
WHERE "b"."Status" = 'Active'
ORDER BY "b"."Id";
```

*Explanation:* the two `.Include()` calls tell EF Core to eagerly fetch the related
`Student` and `Equipment` rows in the *same* query rather than issuing separate
round-trips per borrowing (avoiding the N+1 query problem the Activity 2 ViewModel had
when it manually called the repositories once per item). EF Core translates this into
two `INNER JOIN`s and selects columns from all three tables in one result set.

> **To capture the real, verified SQL for submission:** add `.LogTo(Console.WriteLine,
> LogLevel.Information)` to the `UseSqlite(...)` options chain in `App.axaml.cs`
> temporarily, run the app, exercise both operations above, and copy the actual logged
> SQL text shown in the console/debug output. The statements above are accurate
> representative examples of EF Core 8's known translation behavior, but your own
> captured output is the authoritative evidence for submission.

## 7. Tracking and No-Tracking Decisions

| Operation | Tracking Used | Why |
|---|---|---|
| `GetAllAsync`, `GetAvailableAsync` (Equipment) | `AsNoTracking()` | Display-only — the equipment list is read and shown in the UI, never mutated through the tracked instance EF Core returns. |
| `GetActiveAsync` (Borrowings, with `Include`) | `AsNoTracking()` | Same reasoning — the Active Borrowings screen only reads this data to populate `BorrowingDisplayItem`s. |
| `UpdateAsync` (Equipment, Borrowing) | Tracked | The repository re-fetches the entity by key (`FindAsync`, which tracks by default), then applies the incoming values with `context.Entry(tracked).CurrentValues.SetValues(...)`. EF Core compares the tracked snapshot against these new values and only marks genuinely changed columns as `Modified`, producing a more precise `UPDATE` statement than blindly overwriting every column. |

## 8. Persistence Demonstration

Verified manually: started the application, borrowed a piece of equipment, confirmed it
appeared immediately under Active Borrowings, then closed the application completely
(not just the window — the process) and reopened it. The borrowing was still listed,
confirming the data survived the restart because it now lives in
`equipmentborrowing.db` rather than in a `ConcurrentDictionary` that only existed for
the lifetime of the process. The same was repeated for returning that equipment: after
returning it, closing, and reopening, the equipment correctly showed as available again
and the borrowing no longer appeared under Active Borrowings. See the accompanying
screenshots for this submission.

## 9. Architectural Reflection

**1. Why did the application not need to be completely rewritten when SQLite was introduced?**
Because Activity 1 put a repository *interface* between the Application layer and
however data happens to be stored. `BorrowEquipmentService` and `ReturnEquipmentService`
only ever called methods on `IStudentRepository`/`IEquipmentRepository`/
`IBorrowingRepository` — never on a concrete storage class. Swapping what sits behind
those interfaces (in-memory collections → EF Core + SQLite) was therefore a change
confined entirely to the Infrastructure project and one line in the DI registration.

**2. Why should the ViewModel not use DbContext directly?**
A ViewModel's job is to hold UI state and expose commands — not to know what a
`DbContext`, a connection string, or a SQL table is. If `EquipmentViewModel` called
`DbContext` directly, every ViewModel that needed data would duplicate persistence
logic, business rules could be bypassed, and testing a ViewModel would require a real
database. Routing everything through `BorrowEquipmentService` keeps the ViewModel
thin and keeps one place (the repository) responsible for knowing SQLite exists at all.

**3. What responsibility does the repository implementation now perform?**
It translates the interface's abstract operations (`GetAvailableAsync`, `AddAsync`,
`UpdateAsync`, etc.) into concrete EF Core LINQ queries against `EquipmentBorrowingDbContext`,
decides when a query should track its results versus run read-only, and converts
detached domain objects passed in from the Application layer into tracked, persisted
database rows via `SaveChangesAsync`.

**4. What is the purpose of an EF Core migration?**
A migration is a versioned, reproducible description of how to change the database
schema from one state to the next (in this case, from "no database" to "Students,
Equipment, and Borrowings tables with the configured keys, constraints, and indexes").
Because the migration is a file committed to the repository rather than a database
someone built by hand, anyone — including a grader — can recreate the exact same schema
from scratch by running `dotnet ef database update`.

**5. Why are foreign keys important in the borrowing database?**
They are what actually enforces, at the database level, that every `Borrowing` row
refers to a `Student` and `Equipment` row that genuinely exists — preventing orphaned
borrowing records that point at a student or equipment ID that was never created (or
was deleted). They are also what make the `Include()`-based join query in Part 6 above
possible and efficient.

**6. Why can a read-only query benefit from AsNoTracking()?**
When a query uses `AsNoTracking()`, EF Core doesn't keep a snapshot of the returned
entities to compare against later for change detection. For a query whose result is
only ever displayed (like the equipment list or Active Borrowings screen) that
bookkeeping is pure overhead — `AsNoTracking()` is both faster and clearer about intent:
"this data will not be modified through this DbContext instance."

**7. What would happen to the rest of the application if the SQLite implementation were replaced later by another database provider?**
Only the Infrastructure project would need to change: swap `UseSqlite(...)` for, say,
`UseNpgsql(...)` in `App.axaml.cs`, regenerate the migrations for the new provider, and
potentially adjust an `Ef*Repository` method if the new provider translated a particular
LINQ expression differently. `EquipmentBorrowing.Domain`, `EquipmentBorrowing.Application`,
and every View and ViewModel in `EquipmentBorrowing.Desktop` would require zero changes,
for exactly the same reason the move from in-memory to SQLite required none of them to
change either.