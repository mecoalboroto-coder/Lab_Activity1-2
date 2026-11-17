-- ============================================================
-- Equipment Borrowing System
-- Laboratory Activity 3, Part C: Basic SQL demonstration
--
-- These statements demonstrate understanding of the relational
-- structure directly. The application itself does NOT run these
-- statements; it performs its real persistence operations through
-- the repository + EF Core architecture (see Infrastructure/Repositories).
-- Table names match EquipmentBorrowingDbContext's configured mappings:
-- Students, Equipment, Borrowings.
-- ============================================================


-- ----------------------------------------------------------
-- 1. Basic Retrieval
-- Retrieve all equipment.
-- ----------------------------------------------------------
SELECT *
FROM Equipment;


-- ----------------------------------------------------------
-- 2. Filtering
-- Retrieve only currently available equipment.
-- ----------------------------------------------------------
SELECT Id, Name, Description, IsAvailable
FROM Equipment
WHERE IsAvailable = 1;


-- ----------------------------------------------------------
-- 3. Join
-- Retrieve active borrowings together with the corresponding
-- student and equipment information.
-- ----------------------------------------------------------
SELECT
    s.Name     AS Student,
    e.Name     AS Equipment,
    b.DateBorrowed       AS Borrowed,
    b.ExpectedReturnDate AS Due
FROM Borrowings b
INNER JOIN Students  s ON s.Id = b.StudentId
INNER JOIN Equipment  e ON e.Id = b.EquipmentId
WHERE b.Status = 'Active'
ORDER BY b.DateBorrowed;


-- ----------------------------------------------------------
-- 4. Aggregate
-- Number of active borrowings per student (only students who
-- currently have at least one active borrowing are listed).
-- ----------------------------------------------------------
SELECT
    s.Name                  AS Student,
    COUNT(b.Id)              AS ActiveBorrowingCount
FROM Students s
INNER JOIN Borrowings b ON b.StudentId = s.Id AND b.Status = 'Active'
GROUP BY s.Id, s.Name
ORDER BY ActiveBorrowingCount DESC;

-- Additional aggregate example: total number of active borrowings system-wide.
SELECT COUNT(*) AS TotalActiveBorrowings
FROM Borrowings
WHERE Status = 'Active';


-- ----------------------------------------------------------
-- 5. Update
-- Mark a specific borrowing as returned and make its equipment
-- available again (the two statements the application's
-- ReturnEquipmentService performs conceptually, shown here as
-- raw SQL for demonstration purposes only).
-- ----------------------------------------------------------
UPDATE Borrowings
SET Status = 'Returned',
    ReturnedAt = date('now')
WHERE Id = 9000;

UPDATE Equipment
SET IsAvailable = 1
WHERE Id = (
    SELECT EquipmentId FROM Borrowings WHERE Id = 9000
);
