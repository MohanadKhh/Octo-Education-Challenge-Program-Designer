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
All **17 unit test scenarios** pass cleanly in under 1 second.

---

## 💡 Key Assumptions & Design Decisions

### 1. Ancestor & Parent Prerequisite Restriction (Containment Cycle)
> A node cannot depend on its parent, grandparent, or any ancestor container.
* **Why**: A parent group cannot complete until all its children complete. If a child depends on its parent, neither can ever start.
* **Example**:
  ```
  Module A (Group)
  └── Step A1 (Step) -> Prerequisite: Module A (INVALID: Parent dependence)
  ```

---

### 2. Client GUID Alignment (Production Standard)
> All node `id` and `prerequisiteId` values are expected as GUIDs from the Frontend.
* **Why**: Prevents ambiguity if multiple nodes in different branches share the same name (e.g. `"Electives"` or `"Intro"`). Output responses conveniently return both `prerequisiteId` and `prerequisiteName`.
* **Example Payload**:
  ```json
  {
    "id": "10000000-0000-0000-0000-000000000002",
    "name": "AI Capstone",
    "type": "step",
    "stepType": "submit work",
    "prerequisiteId": "10000000-0000-0000-0000-000000000001"
  }
  ```

---

### 3. Choice Groups & Mutual Cycle Validation
> Nodes inside a `Choice` group do not execute sequentially (in-order), so Forward Reference rules do NOT apply inside a `Choice` group. Prerequisites inside `Choice` groups are validated for mutual dependency cycles ($A \rightarrow B \rightarrow A$).
* **Why**: Choice group options are parallel alternatives. Order position does not matter, but circular dependencies ($A \rightarrow B \rightarrow A$) are strictly invalid.
* **Example**:
  ```
  Track Selection (Choice Group)
  ├── Module A (Group) -> Prerequisite: Module B
  └── Module B (Group) -> Prerequisite: Module A (INVALID: Mutual Dependency Cycle)
  ```

---

## 🏗️ Architecture Overview

Built following **Clean Architecture** and **DDD** principles:

```
ProgramDesigner.APIs (ASP.NET Core REST Controllers)
  ├── ProgramDesigner.Application (Validator, Simulator, Services, DTOs, Mappers)
  ├── ProgramDesigner.Infrastructure (EF Core InMemory, UnitOfWork, Repositories)
  └── ProgramDesigner.Domain (Pure Entities, Enums - Zero External Dependencies)
```

### Result Pattern
All application services return a standardized `GeneralResult<T>` wrapper (`Success`, `NotFound`, `ValidationError`, `Failure`) to avoid using exceptions for control flow.

---

## 🔌 Core API Endpoints

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/programs` | Create a new program from a JSON tree |
| `GET` | `/programs/{id}` | Retrieve full program tree |
| `POST` | `/programs/{id}/validate` | Validate prerequisite logic (Impossible & Warnings) |
| `POST` | `/programs/{id}/simulate` | Simulate participant progress (`Completed`, `Unlocked`, `Blocked`) |

---

### Sample Program Payload (`POST /programs`)

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
