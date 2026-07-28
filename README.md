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
     # Set ProgramDesigner.Infrastructure as the Default Project
     Add-Migration InitialCreate
     Update-Database
     ```
> **Note:** Ensure the correct startup project (`ProgramDesigner.APIs`) is selected before running migration commands.


---


## 💡 Business Assumptions & Domain Design Decisions

### 1. Educational Hierarchy Integrity (Parent/Ancestor Prerequisite Rejection)
> **Business Rule**: A topic or step inside a course module cannot list its parent container (or any grandparent container) as a prerequisite.
* **Domain Context**: A parent container (e.g., `"Foundations Group"`) is defined by the completion of its child steps. If a child step depends on the parent group, a structural deadlock occurs where neither can ever start.
* **Example**:
  ```
  Foundations (Group)
  └── Introduction to Computing (Step) -> Prerequisite: Foundations (INVALID: Parent Deadlock)
  ```

---

### 2. Production Identity Management (GUID Linkages & Dual-Property Outputs)
> **Business Rule**: Client applications submit node linkages using GUID identifiers (`id` and `prerequisiteId`) to prevent curriculum name collisions.
* **Domain Context**: Educational institutions frequently reuse common labels across tracks (e.g., `"Electives"`, `"Capstone"`, or `"Foundations"`). Linking via GUIDs eliminates ambiguity when identical names exist across different branches.
* **User Experience**: Response outputs automatically include both `prerequisiteId` (GUID) and `prerequisiteName` (Human-Readable String) for clear UI rendering.

---

### 3. Elective Track Selection vs. Sequential Execution
> **Business Rule**: Choice group options are parallel as for example electives courses not must taken inOrder tracks, so sequential "Forward Reference" constraints do not apply. However, mutual dependency cycles ($A \rightarrow B \rightarrow A$) are strictly forbidden.
* **Domain Context**: When a student chooses 1 of 3 elective modules, the options are unordered alternatives. Ordering position inside a Choice container does not imply order of execution, but two elective tracks cannot mutually require each other, so for that example be invalid addition that its warning for prerequist from choice.
* **Example**:
  ```
  Major Specialization (Choice Group)
  ├── AI Module (Group) -> Prerequisite: IT Module
  └── IT Module (Group) -> Prerequisite: AI Module (INVALID: Mutual Dependency Cycle)
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
Evaluates curriculum prerequisite integrity, identifying impossible prerequisites and reachability warnings.

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
Simulates a participant's progress based on selected choice tracks and completed steps. Filtered output returns only active statuses (`Completed`, `Unlocked`, `Blocked`).

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
