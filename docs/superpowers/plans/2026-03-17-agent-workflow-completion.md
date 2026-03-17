# Agent Workflow Completion Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the review-first agent workflow feel complete for Phase 2 by improving prompt scope clarity, proposal review readability, selected-object agent context, and longer-session history readability without implying unbuilt MCP or agent capability.

**Architecture:** Keep the existing single command path intact and layer Phase 2 entirely on top of current view-model state, command provenance, and the accepted shell baseline. Add plain-language view-model properties for scope, proposal summaries, and activity metadata, then bind them into the existing shell so first-time users can follow prompt, proposal, review, and apply without losing direct editing.

**Tech Stack:** .NET 8, Avalonia XAML, C#, xUnit

---

## Chunk 1: View-model behavior and truthful review copy

### Task 1: Define failing tests for Phase 2 workflow messaging

**Files:**
- Modify: `tests/CraftMCP.Tests/Unit/App/WorkspaceViewModelTests.cs`

- [ ] **Step 1: Write failing tests**
  Add tests for:
  - plain-language prompt scope details for no selection, single selection, and multi-selection
  - proposal review summary/change summary text derived from the current proposal
  - selected-object agent context text that stays truthful when there is no proposal and when a proposal is pending
  - activity entries exposing more readable provenance labels for longer sessions

- [ ] **Step 2: Run tests to verify they fail**

Run:
```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test tests\CraftMCP.Tests\CraftMCP.Tests.csproj --filter WorkspaceViewModelTests
```

Expected:
- At least the new assertions fail because the new properties or output formats do not exist yet.

- [ ] **Step 3: Write minimal implementation**
  Update `src/CraftMCP.App/ViewModels/WorkspaceViewModel.cs` and `src/CraftMCP.App/Models/WorkspaceActivityEntry.cs` to expose only the new truthful Phase 2 properties and formatting needed by the tests.

- [ ] **Step 4: Run tests to verify they pass**

Run:
```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test tests\CraftMCP.Tests\CraftMCP.Tests.csproj --filter WorkspaceViewModelTests
```

Expected:
- `WorkspaceViewModelTests` pass.

## Chunk 2: Shell wiring and layout assertions

### Task 2: Define failing layout tests for the Phase 2 review surfaces

**Files:**
- Modify: `tests/CraftMCP.Tests/Unit/App/MainWindowLayoutTests.cs`
- Modify: `src/CraftMCP.App/MainWindow.axaml`

- [ ] **Step 1: Write failing tests**
  Add assertions that the shell contains:
  - prompt target context copy in the prompt composer
  - richer proposal review structure instead of only a raw command preview text block
  - clearer agent-context content in the inspector
  - longer-session activity cards with richer metadata bindings
  - only lightweight proposal-aware affordances

- [ ] **Step 2: Run tests to verify they fail**

Run:
```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test tests\CraftMCP.Tests\CraftMCP.Tests.csproj --filter MainWindowLayoutTests
```

Expected:
- New layout assertions fail against the current shell.

- [ ] **Step 3: Write minimal implementation**
  Update `src/CraftMCP.App/MainWindow.axaml` to bind the new Phase 2 view-model properties into the existing left rail and inspector without changing save/open/export/direct-edit structure.

- [ ] **Step 4: Run tests to verify they pass**

Run:
```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test tests\CraftMCP.Tests\CraftMCP.Tests.csproj --filter MainWindowLayoutTests
```

Expected:
- `MainWindowLayoutTests` pass.

## Chunk 3: Workflow validation and release proof

### Task 3: Protect the review-first workflow at the scenario level

**Files:**
- Modify: `tests/CraftMCP.Tests/Unit/App/WorkflowValidationTests.cs`

- [ ] **Step 1: Write failing workflow assertions**
  Extend one or more workflow tests to assert that proposal approval remains the only mutation path and that the richer review/history surfaces do not disturb save, reopen, export, or manual follow-up edits.

- [ ] **Step 2: Run tests to verify they fail**

Run:
```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test tests\CraftMCP.Tests\CraftMCP.Tests.csproj --filter WorkflowValidationTests
```

Expected:
- The new workflow-level assertions fail until the Phase 2 properties and shell bindings exist.

- [ ] **Step 3: Write minimal implementation**
  Make only the smallest production changes needed to satisfy the workflow assertions while keeping the existing command path and review gates unchanged.

- [ ] **Step 4: Run tests to verify they pass**

Run:
```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test tests\CraftMCP.Tests\CraftMCP.Tests.csproj --filter WorkflowValidationTests
```

Expected:
- `WorkflowValidationTests` pass.

## Chunk 4: Full verification and reporting

### Task 4: Run the required release checks and write the report note

**Files:**
- Create: `CraftMCP/<next note number> Phase 2 Agent Workflow Completion Implementation Report.md` in the `big-brain-vault`

- [ ] **Step 1: Run the required verification commands**

Run:
```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test tests\CraftMCP.Tests\CraftMCP.Tests.csproj
& 'C:\Program Files\dotnet\dotnet.exe' build CraftMCP.sln
```

- [ ] **Step 2: Read outputs completely**
  Capture exact passed/failed counts, warning/error counts, and elapsed time where available.

- [ ] **Step 3: Write the implementation report note**
  Record:
  - implemented scope
  - exact files changed
  - truthful adaptations from the redesign concept
  - exact verification results
  - remaining gaps before real MCP integration

- [ ] **Step 4: Summarize completion for the user**
  Report only claims backed by the fresh verification output.
