# Use-Case Diagram — Laboratory Activity 4

The custodian is the primary actor. PostgreSQL is the supporting system. No include or extend relationships are drawn. Deferred cases (UC-10, UC-11, UC-17 to UC-27) and future cases (UC-28 to UC-33) are outside the system boundary.

```mermaid
flowchart LR
    C(["Laboratory Staff /<br/>Equipment Custodian<br/>(primary actor)"])
    DB[("PostgreSQL<br/>(supporting system)")]

    subgraph S["Campus Equipment Borrowing System"]
        direction TB
        UC01(["UC-01 View Equipment Catalog"])
        UC02(["UC-02 Find Available Equipment"])
        UC03(["UC-03 Borrow Equipment"])
        UC04(["UC-04 View Active Borrowings"])
        UC05(["UC-05 Return Equipment"])
        UC06(["UC-06 Check Out Multiple Equipment Items"])
        UC07(["UC-07 View Student Borrowing History"])
        UC08(["UC-08 View Equipment Borrowing History"])
        UC09(["UC-09 Search, Filter, and Sort Borrowings"])
        UC12(["UC-12 Register Student"])
        UC13(["UC-13 Update Student Information"])
        UC14(["UC-14 Change Student Borrowing Eligibility"])
        UC15(["UC-15 Register Equipment"])
        UC16(["UC-16 Update Equipment Information"])
    end

    C --- UC01
    C --- UC02
    C --- UC03
    C --- UC04
    C --- UC05
    C --- UC06
    C --- UC07
    C --- UC08
    C --- UC09
    C --- UC12
    C --- UC13
    C --- UC14
    C --- UC15
    C --- UC16

    S -.-> DB
```

GitHub renders the diagram above. To get a PNG for the submission, paste the Mermaid block into https://mermaid.live and export the image to `docs/use-case-diagram.png`.
