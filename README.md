# Octo Education — Program Designer API Coding Challenge

A clean, production-grade **.NET 10 REST API** for designing, validating, and simulating educational programs modeled as recursive trees of **Steps** and **Groups**.


---


## ⚡ Quick Start (Run in under 2 minutes)

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Run the API
```bash
dotnet run --project ProgramDesigner.APIs
```
The API starts at `https://localhost:7198` (or `http://localhost:5198`).  
Scalar API Reference UI: `https://localhost:7198/scalar/v1`  
OpenAPI JSON Spec: `https://localhost:7198/openapi/v1.json`

### 2. Run the Test Suite
```bash
dotnet test ProgramDesigner.Tests --verbosity normal
```
All **18 unit test scenarios** pass cleanly in under 1 second.

### 3. Database Options (In-Memory vs Real Database)
* **Default (In-Memory)**: Enabled out-of-the-box for zero setup. *(Note: Data is temporary and resets when the app stops).*
* **Real Persistent Database (SQL Server / PostgreSQL / SQLite)**:
  To connect to a real database:
  1. Open `ProgramDesigner.Infrastructure/DependencyInjection.cs`.
  2. Comment out `UseInMemoryDatabase` and uncomment `UseSqlServer` (or your database provider).
  3. Install the EF Core package for your provider if not using SQL Server (e.g. `Npgsql.EntityFrameworkCore.PostgreSQL`).
  4. Create and apply EF Core migrations using the **Package Manager Console**:
     ```powershell
     Add-Migration InitialCreate
     Update-Database
     ```
> **Note:** Ensure Set ProgramDesigner.Infrastructure as the Default Project and the correct startup project (`ProgramDesigner.APIs`) is selected before running migration commands.


---


## 💡 Business Assumptions & Design Decisions

### 1. A Node cannot require any of his Ancestors

> **Scenario**: Imagine a "Foundations" module that contains "Introduction to Computing". If we set "Introduction to Computing" to require "Foundations" to be completed first, we create a deadlock — the module can't complete until the step is done, but the step can't start until the module is done. Neither can ever begin.

This applies to all ancestor levels, the only mentioned in description the case of require of descendants not ancestors.

```
Foundations (Module)                        
└── Introduction to Computing (Step)  →  Prerequisite: Foundations  ❌ Deadlock
```

---

### 2. Choice Group items are unordered

> **Scenario**: When a student pick 2 of 3 specializations (AI, IT, or Programming), these are parallel alternatives — not a sequence. The student doesn't need to complete "AI" before "IT" because elective choices don't depend on each other. So, the usual rule of "you can't depend on something that comes after you" doesn't apply inside elective (choice) groups.

> so Forward reference doesn't apply here **However**, two elective options **cannot require each other**. If the "AI Module" requires "IT Module" will valid if the "IT Module" doesn't require "AI Module", neither can ever be started — this is a mutual dependency cycle and is always invalid.

```
Major Specialization (Choose 1)
├── AI Module  →  Prerequisite: IT Module
└── IT Module  →  Prerequisite: AI Module     ❌ Mutual cycle — neither can start
```

---

### 3. A prerequisite on a specific item inside a "pick all" (Group, CHOICE: 3 of 3) is safe, not warning
> **Scenario**: A Choice group set to "pick 3 of 3" requires every item inside it to be completed — there's no alternative path, only one way through. So, a prerequisite pointing at one specific item inside it is just as safe as pointing at a mandatory step, even though the item technically sits inside a Choice container.

This is different from a normal Choice group (e.g. "pick 2 of 3"), where a specific item might never be selected and any prerequisite on it is flagged as a reachability **warning**.

---

### 4. Each Node with its prerequisite is identified by a unique GUID from frontend, not by name

> **Scenario**: A university might have an "Electives" group inside both the AI track and the IT track. If we link prerequisites by name, the system can't tell which "Electives" is meant. By using unique identifiers (GUIDs), every item in the curriculum is unambiguous — even when two items share the same display name.

In API responses, we return **both** the ID and the human-readable name of each prerequisite, so the frontend can display friendly labels while keeping reliable linkages under the hood — [see example of Create Program Endpoint request with GUID and response with prerequisite(ID & Name) below](#1-post-programs--create-program).

```
AI Track                          IT Track
└── Electives (id: AAA)           └── Electives (id: BBB)     ← Same name, different items
```---
```

---

## 🤖 AI Tool Usage

- **Antigravity** — Help me in building the implementation: the Clean Architecture solution, domain model, validation logic (cycle detection, transitive reachability), API endpoints, and xUnit test suite.
- **Claude** — used to think through the business concepts before implementation: the two validation categories (impossible vs. reachability-risk prerequisites), all possible edge cases like mutual cycles inside Choice groups, and prerequisites on a Choice group as a whole vs. a specific child inside one — see "Business Assumptions & Domain Design Decisions" above.


---


## 🏗️ Architecture Overview

Built following **Clean Architecture** and **DDD** principles:

```
ProgramDesigner.APIs (ASP.NET Core REST Controllers)
  ├── ProgramDesigner.Application (Validator, Simulator, Services, DTOs, Mappers)
  ├── ProgramDesigner.Infrastructure (EF Core InMemory, UnitOfWork, Repositories)
  └── ProgramDesigner.Domain (Pure Entities, Enums - Zero External Dependencies)
```

### GeneralResult Pattern
All application services return a standardized `GeneralResult<T>` wrapper (`Success`, `NotFound`, `ValidationError`, `Failure`) to maintain predictable HTTP responses without throwing control-flow exceptions.

---

## 🔌 API Endpoints & Contract Samples

### 1. `POST /programs` — Create Program
Creates a new learning program tree.

#### Request Body Sample
```json
{
  "name": "Computer Science Qualification",
  "rootNode": {
    "id": "10000000-0000-0000-0000-000000000001",
    "name": "Computer Science",
    "type": "group",
    "rule": "inOrder",
    "children": [
      {
        "id": "10000000-0000-0000-0000-000000000002",
        "name": "Foundations",
        "type": "group",
        "rule": "inOrder",
        "children": [
          {
            "id": "10000000-0000-0000-0000-000000000003",
            "name": "Introduction to Computing",
            "type": "step",
            "stepType": "attend session"
          }
        ]
      },
      {
        "id": "10000000-0000-0000-0000-000000000004",
        "name": "Major",
        "type": "group",
        "rule": "choice",
        "choiceCount": 1,
        "prerequisiteId": "10000000-0000-0000-0000-000000000002"
      }
    ]
  }
}
```

#### Response Sample (`201 Created`)
```json
{
  "isSuccess": true,
  "status": 0,
  "message": "Program created successfully.",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Computer Science Qualification",
    "createdAt": "2026-07-28T13:40:00Z",
    "rootNode": {
      "id": "10000000-0000-0000-0000-000000000001",
      "name": "Computer Science",
      "type": "group",
      "rule": "inOrder",
      "children": [
        {
          "id": "10000000-0000-0000-0000-000000000002",
          "name": "Foundations",
          "type": "group",
          "rule": "inOrder",
          "children": [
            {
              "id": "10000000-0000-0000-0000-000000000003",
              "name": "Introduction to Computing",
              "type": "step",
              "stepType": "attend session"
            }
          ]
        },
        {
          "id": "10000000-0000-0000-0000-000000000004",
          "name": "Major",
          "type": "group",
          "rule": "choice",
          "choiceCount": 1,
          "prerequisiteId": "10000000-0000-0000-0000-000000000002",
          "prerequisiteName": "Foundations"
        }
      ]
    }
  }
}
```

---

### 2. `GET /programs/{id}` — Get Program Details
Retrieves the full program tree structure by ID.

#### Response Sample (`200 OK`)
```json
{
  "isSuccess": true,
  "status": 0,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Computer Science Qualification",
    "createdAt": "2026-07-28T13:40:00Z",
    "rootNode": {
      "id": "10000000-0000-0000-0000-000000000001",
      "name": "Computer Science",
      "type": "group",
      "rule": "inOrder",
      "children": [ ... ]
    }
  }
}
```

---

### 3. `POST /programs/{id}/validate` — Validate Program Logic
Checks whether the curriculum's prerequisite rules make sense before going live. Returns two categories:
- **Impossible Prerequisites** (`isValid: false`): Structural errors that make the program uncompletable (e.g., deadlocks, circular dependencies). These **must** be fixed.
- **Reachability Warnings** (`isValid: true`): The program is valid, but some prerequisites point to items inside an elective group that a student might not choose — worth reviewing but not blocking.

#### Response Sample (`200 OK`)
```json
{
  "isSuccess": true,
  "status": 0,
  "data": {
    "isValid": true,
    "impossiblePrerequisites": [],
    "reachabilityWarnings": [
      {
        "nodeId": "10000000-0000-0000-0000-000000000012",
        "nodeName": "AI Capstone",
        "prerequisiteTargetId": "10000000-0000-0000-0000-000000000009",
        "prerequisiteTargetName": "Computer Vision",
        "reason": "Prerequisite 'Computer Vision' is inside a Choice group ('Electives'). A participant might pick other electives, making 'AI Capstone' unreachable."
      }
    ]
  }
}
```

---

### 4. `POST /programs/{id}/simulate` — Simulate Participant Progress
Simulates a participant's journey through the program based on their elective choices and completed steps. Shows what they've done, what's available next, and what's still locked:
- **Completed**: The student has finished this step.
- **Unlocked**: All prerequisites are met — the student can start this step now.
- **Blocked**: One or more prerequisites are not yet completed.

#### Request Body Sample
```json
{
  "choiceSelections": {
    "Major": ["AI"],
    "Electives": ["Computer Vision", "Natural Language Processing"]
  },
  "completedSteps": [
    "Introduction to Computing",
    "Mathematics for Computing",
    "Machine Learning Basics"
  ]
}
```

#### Response Sample (`200 OK`)
```json
{
  "isSuccess": true,
  "status": 0,
  "data": {
    "statuses": [
      {
        "nodeId": "10000000-0000-0000-0000-000000000003",
        "nodeName": "Introduction to Computing",
        "status": "Completed"
      },
      {
        "nodeId": "10000000-0000-0000-0000-000000000004",
        "nodeName": "Mathematics for Computing",
        "status": "Completed"
      },
      {
        "nodeId": "10000000-0000-0000-0000-000000000007",
        "nodeName": "Machine Learning Basics",
        "status": "Completed"
      },
      {
        "nodeId": "10000000-0000-0000-0000-000000000009",
        "nodeName": "Computer Vision",
        "status": "Unlocked"
      },
      {
        "nodeId": "10000000-0000-0000-0000-000000000010",
        "nodeName": "Natural Language Processing",
        "status": "Unlocked"
      },
      {
        "nodeId": "10000000-0000-0000-0000-000000000012",
        "nodeName": "AI Capstone",
        "status": "Blocked",
        "blockedReason": "Prerequisite 'Electives' is not completed."
      }
    ]
  }
}
```
