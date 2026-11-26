-- Lab 5 controlled dataset (reference section 3). Fixed application date: 2026-11-10.
-- DELETES ALL DATA, then seeds S1-S4 and E1-E7.
-- Refuses to run on any database other than equipment_borrowing_test.

DO $$
BEGIN
    IF current_database() <> 'equipment_borrowing_test' THEN
        RAISE EXCEPTION 'Refusing to reset database "%". Connect to equipment_borrowing_test.', current_database();
    END IF;

    TRUNCATE TABLE "Borrowings", "Equipment", "Students" RESTART IDENTITY;

    INSERT INTO "Students" ("StudentNumber", "Name", "Contact", "IsAllowedToBorrow") VALUES
        ('ST-100', 'Ada Cruz', NULL, TRUE),
        ('ST-200', 'Ben Diaz', NULL, FALSE),
        ('ST-300', 'Cara Lim', NULL, TRUE),
        ('ST-400', 'Dani Ong', NULL, TRUE);

    INSERT INTO "Equipment" ("Identifier", "Name", "Description", "IsAvailable") VALUES
        ('EQ-001', 'Camera',     NULL, FALSE),
        ('EQ-002', 'Laptop',     NULL, TRUE),
        ('EQ-003', 'Projector',  NULL, TRUE),
        ('EQ-004', 'Tablet',     NULL, FALSE),
        ('EQ-005', 'Microphone', NULL, FALSE),
        ('EQ-006', 'Tripod',     NULL, TRUE),
        ('EQ-007', 'Speaker',    NULL, TRUE);

    INSERT INTO "Borrowings"
        ("StudentId", "EquipmentId", "DateBorrowed", "ExpectedReturnDate", "ReturnedAt", "Status")
    SELECT s."Id", e."Id", v.borrowed::date, v.expected::date, v.returned::date, v.status
    FROM (VALUES
        ('ST-100', 'EQ-001', '2026-11-01', '2026-11-09', NULL,         'Active'),
        ('ST-100', 'EQ-003', '2026-11-02', '2026-11-12', '2026-11-08', 'Returned'),
        ('ST-300', 'EQ-004', '2026-11-03', '2026-11-12', NULL,         'Active'),
        ('ST-300', 'EQ-005', '2026-11-03', '2026-11-12', NULL,         'Active')
    ) AS v(student, item, borrowed, expected, returned, status)
    JOIN "Students"  s ON s."StudentNumber" = v.student
    JOIN "Equipment" e ON e."Identifier"    = v.item;
END $$;

-- Readback: 7 equipment rows, with borrowing details where they exist.
SELECT e."Identifier", e."Name", e."IsAvailable",
       s."StudentNumber", b."Status", b."DateBorrowed", b."ExpectedReturnDate", b."ReturnedAt",
       (SELECT COUNT(*) FROM "Students") AS student_count
FROM "Equipment" e
LEFT JOIN "Borrowings" b ON b."EquipmentId" = e."Id"
LEFT JOIN "Students"  s ON s."Id" = b."StudentId"
ORDER BY e."Identifier";
