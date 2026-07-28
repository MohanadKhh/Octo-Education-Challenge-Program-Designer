# Octo Education — Program Designer API Coding Challenge

A .NET 10 Clean Architecture REST API for designing, validating, and simulating educational programs modeled as recursive trees of **Steps** and **Groups**.

---

## ⚡ Quick Start (Run in under 2 minutes)

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Run the API
```bash
dotnet run --project ProgramDesigner.APIs
```
The API starts at `https://localhost:7198` (or `http://localhost:5198`).  
Open Swagger UI in your browser: `https://localhost:7198/openapi/v1.json` or test via OpenAPI tooling.

### 2. Run the Test Suite
```bash
dotnet test ProgramDesigner.Tests --verbosity normal
```
All **11 unit test scenarios** pass cleanly in under 1 second.

---

## 🏗️ Architecture & Technical Stack

Built following **Clean Architecture** and **Domain-Driven Design (DDD)** principles:

```
ProgramDesigner.APIs (ASP.NET Core Controllers)
  ├── ProgramDesigner.Application (ProgramValidator, ProgramSimulator, Services, DTOs, Mappers, Interfaces)
  ├── ProgramDesigner.Infrastructure (EF Core InMemory, UnitOfWork, Repository)
  └── ProgramDesigner.Domain (Entities, Enums)
```

* **Domain Layer**: Zero dependencies on ASP.NET, EF Core, or any third-party framework. Contains pure domain entities (`Node`, `LearningProgram`) and core enums (`NodeType`, `GroupRule`).
* **Application Layer**: Program tree validation engine (`IProgramValidator` / `ProgramValidator`), simulation engine (`IProgramSimulator` / `ProgramSimulator`), use case orchestration (`IProgramService` / `ProgramService`), DTO definitions (`ProgramValidationResult`, `ProgramSimulationResult`), standardized response wrappers (`GeneralResult`, `GeneralResult<T>`, `ResultStatus`), and explicit recursive DTO↔Entity mapping (`NodeMapper`). Uses **Dependency Injection** for all services and **Unit of Work** (`IUnitOfWork`) for transaction control without throwing exceptions for control flow.
* **Infrastructure Layer**: DI registrations in `DependencyInjection.cs`, persistence via EF Core (InMemory provider configured by default for zero-setup execution). Can be swapped to SQL Server with a single line change.
* **API Layer**: ASP.NET Core REST API controllers with camelCase JSON formatting and string enum serialization.

---

## 🌲 Domain Model & Tree Design

The program is modeled as a single, unified recursive tree. Step vs Group is represented as a discriminated type (`NodeType`) rather than two unrelated class hierarchies:

* **Step (Leaf)**: Atomic activity (`attend session`, `pass test`, `submit work`).
* **Group (Container)**: Ordered list of child nodes (`Step` or nested `Group`).
  * `InOrder`: Every child must be completed in sequential order.
  * `Choice(N of M)`: Participant picks and completes any $N$ out of $M$ children.
* **Prerequisites**: Any node (Step or Group) may carry a `prerequisiteId` (or user-friendly `prerequisiteName`), blocking access until satisfied.

---

## 🎯 Validation Logic & Algorithm Design

Validation is split into two distinct categories:

### 1. Impossible Prerequisites (`isValid = false` — Rejected)
1. **Self-Reference**: A node pointing at itself.
2. **Containment Cycle**: A node pointing at a parent/ancestor container or a group pointing at one of its own descendants.
3. **InOrder Forward-Reference**: In an `InOrder` group, a node pointing to a step/group that appears later in the sequence. Evaluated by finding the Lowest Common Ancestor (LCA) and comparing child indices.
4. **Prerequisite Graph Cycles**: Mutual or indirect cross-branch cycles (e.g. Module A ↔ Module B). Detected using **DFS 3-Coloring Graph Traversal** (`white`, `gray`, `black` states).

### 2. Reachability Warnings (`isValid = true` — Warning Only)
* Flags prerequisites pointing to nodes inside a `Choice(N < M)` group that a participant might never select.
* **Key Distinction**: A prerequisite pointing to a `Choice` group **as a whole** (e.g. `Final Capstone → Major`) is **safe** (guaranteed completion for anyone reaching it). Only prerequisites pointing to a **specific child item inside** a choice branch generate a warning.
* **Co-Selection Optimization**: If both the source and target node are located under the *same* branch of a Choice group, no warning is generated because both are co-selected.

---

## 🔌 API Endpoints

### 1. `POST /programs`
Create a program from a JSON tree. Server assigns IDs and resolves `prerequisiteName` references automatically.

#### Example Request Body (Computer Science Qualification)
```json
{
  "name": "Computer Science",
  "rootNode": {
    "name": "Computer Science",
    "type": "group",
    "rule": "inOrder",
    "children": [
      {
        "name": "Foundations",
        "type": "group",
        "rule": "inOrder",
        "children": [
          { "name": "Introduction to Computing", "type": "step" },
          { "name": "Mathematics for Computing", "type": "step" }
        ]
      },
      {
        "name": "Major",
        "type": "group",
        "rule": "choice",
        "choiceCount": 1,
        "prerequisiteName": "Foundations",
        "children": [
          {
            "name": "AI",
            "type": "group",
            "rule": "inOrder",
            "children": [
              { "name": "Machine Learning Basics", "type": "step" },
              {
                "name": "Electives",
                "type": "group",
                "rule": "choice",
                "choiceCount": 2,
                "children": [
                  { "name": "Computer Vision", "type": "step" },
                  { "name": "Natural Language Processing", "type": "step" },
                  { "name": "Robotics", "type": "step" }
                ]
              },
              {
                "name": "AI Capstone",
                "type": "step",
                "prerequisiteName": "Electives"
              }
            ]
          },
          {
            "name": "IT",
            "type": "group",
            "rule": "inOrder",
            "children": [
              { "name": "Networks & Security", "type": "step" },
              { "name": "Systems Administration", "type": "step" }
            ]
          },
          {
            "name": "Programming",
            "type": "group",
            "rule": "inOrder",
            "children": [
              { "name": "Algorithms & Data Structures", "type": "step" },
              { "name": "Software Engineering", "type": "step" }
            ]
          }
        ]
      },
      {
        "name": "Final Capstone",
        "type": "step",
        "prerequisiteName": "Major"
      }
    ]
  }
}
```

---

### 2. `GET /programs/{id}`
Returns the complete program tree with all assigned GUIDs and structure.

---

### 3. `POST /programs/{id}/validate`
Validates prerequisite logic for the given program.

#### Example Response (CS Qualification Scenario)
```json
{
  "isValid": true,
  "impossiblePrerequisites": [],
  "reachabilityWarnings": []
}
```

---

### 4. `POST /programs/{id}/simulate` *(Bonus Feature)*
Simulates a participant's progress through the program based on their choice selections and completed steps.

#### Example Request Body
```json
{
  "choiceSelections": {
    "Major": ["AI"],
    "Electives": ["Computer Vision", "Robotics"]
  },
  "completedSteps": [
    "Introduction to Computing",
    "Mathematics for Computing"
  ]
}
```

#### Example Response
```json
{
  "completed": [
    { "nodeName": "Foundations", "nodeType": "group", "state": "Completed" },
    { "nodeName": "Introduction to Computing", "nodeType": "step", "state": "Completed" },
    { "nodeName": "Mathematics for Computing", "nodeType": "step", "state": "Completed" }
  ],
  "unlocked": [
    { "nodeName": "Major", "nodeType": "group", "state": "Unlocked" },
    { "nodeName": "Machine Learning Basics", "nodeType": "step", "state": "Unlocked" }
  ],
  "blocked": [
    { "nodeName": "Final Capstone", "nodeType": "step", "state": "Blocked", "reason": "Blocked: Prerequisite 'Major' is not completed yet." }
  ]
}
```

---

## 🧪 Test Suite & Scenarios

The xUnit test suite (`ProgramDesigner.Tests`) covers 11 test scenarios across 7 test classes:

| Test Class | Scenario Tested | Result |
| :--- | :--- | :--- |
| `CsQualificationTests` | Full CS Qualification tree | ✅ `isValid: true`, 0 impossible, 0 warnings |
| `SelfAndForwardReferenceTests` | Tree 1: Self-ref + Forward-ref | ✅ Rejected (2 impossible errors) |
| `ContainmentCycleTests` | Tree 2 & Child-Parent cycles | ✅ Rejected (Containment cycle errors) |
| `MutualCycleTests` | Tree 3: Cross-branch mutual cycle | ✅ Rejected (DFS graph cycle error) |
| `ReachabilityWarningTests` | Tree 4: Transitive reachability | ✅ `isValid: true`, 2 warnings generated |
| `FullyValidNoWarningsTests` | Tree 5: Prerequisite on Choice group | ✅ `isValid: true`, 0 warnings |
| `ProgramSimulatorTests` | Participant simulation flow | ✅ Correct status categorization |

---

## 🤖 AI Tool Usage Statement

As permitted and expected by the challenge guidelines, AI tools were used during development:
* **Tools Used**: Antigravity IDE (powered by Claude & Gemini).
* **Where Used**:
  * Scaffolding initial project layer structure and SDK-style project files.
  * Brainstorming edge-case test tree fixtures.
  * Generating markdown documentation and ASCII tree visualizations.
* **Human Oversight**: All core graph algorithms (DFS 3-coloring cycle detection, LCA forward reference checking, ancestor-chain co-selection reachability) were strictly reviewed, verified, and unit-tested for domain correctness.
