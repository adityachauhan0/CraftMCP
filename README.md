<p align="center">
  <img src="assets/cute_female_with_an_axe_rotations_8dir.gif" alt="CraftMCP pixel mascot" width="72" />
</p>

<h1 align="center">CraftMCP</h1>

<p align="center">
  Open-source Windows design tooling for humans and AI agents.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows-2b7fff?style=flat-square" alt="Windows" />
  <img src="https://img.shields.io/badge/runtime-.NET%208-512bd4?style=flat-square" alt=".NET 8" />
  <img src="https://img.shields.io/badge/ui-Avalonia-f59e0b?style=flat-square" alt="Avalonia" />
  <img src="https://img.shields.io/badge/status-active%20prototype-22c55e?style=flat-square" alt="Status" />
</p>

CraftMCP is a lightweight local design workspace where AI agents can create and update designs, and human users can inspect, edit, and export those same designs without leaving the workflow.

The project is intentionally focused. It is not trying to become a full professional design suite. It is trying to become a practical open-source tool people can pair with their own AI models for visual design work.

## Why CraftMCP

Most design tools are optimized for direct human interaction, cloud collaboration, or very broad professional feature sets. That makes them awkward foundations for agent-driven workflows.

CraftMCP is built around a different set of constraints:

- one canonical scene model,
- the same model for human edits and agent edits,
- deterministic exports,
- local-first file handling,
- and clear boundaries between UI, domain, rendering, persistence, and agent integration.

## Intended Use Cases

- Social graphics from prompts
- UI mockups for desktop and mobile apps
- Slide-style layouts and presentation visuals
- Reopening an existing design and refining it with an AI model
- Exporting structured design data so another agent can continue the work

## MVP Principles

| Principle | Meaning |
| --- | --- |
| Human + AI collaboration | AI can generate and modify designs, but humans stay in control and can edit directly. |
| Local-first | Core editing and export should not depend on cloud services. |
| Deterministic output | JSON export is a product contract, not a debug dump. |
| Lightweight scope | Useful open-source tooling matters more than feature breadth. |
| Shared source of truth | `DocumentState` remains the canonical scene model across editing, persistence, and export. |

## Current Progress

The project has moved well beyond the concept stage.

### Phase 1 completed

- .NET 8 solution scaffolded with explicit project boundaries
- Avalonia desktop shell bootstrapped
- Stable ID model added for documents, nodes, and assets
- Core value objects and scene graph contracts implemented
- `DocumentState`, `CanvasModel`, and hierarchy validation added
- Initial test harness and domain tests landed

### Phase 2 completed

- v1 deterministic JSON export contract implemented
- `DocumentJsonExporter` added as the export boundary
- Fixture-based export tests added for social, UI, and slide documents
- Byte-identical repeat export behavior verified
- Transport-neutral command contracts added for the command layer
- `DesignCommand`, `CommandBatch`, `CommandResult`, and `HistoryEntry` implemented
- Initial typed command families defined for create, update, delete, reorder, grouping, canvas, asset import, duplication, visibility, and lock state

### Latest verification

- `dotnet test tests/CraftMCP.Tests/CraftMCP.Tests.csproj` -> 29 passed, 0 failed
- `dotnet build CraftMCP.sln` -> 0 warnings, 0 errors

## Current Architecture Baseline

| Area | Current direction |
| --- | --- |
| App shell | Avalonia + .NET 8 |
| Source of truth | Canonical `DocumentState` |
| Editing model | Same scene model for human edits and agent edits |
| Persistence | Native `.craft` package with bundled assets |
| JSON export | Deterministic top-level artifact envelope |
| Agent model | Internal prompt-driven command engine in MVP |

## Repository Layout

```text
src/
  CraftMCP.App          Avalonia shell
  CraftMCP.Domain       IDs, value objects, scene graph, validation, exports, commands
  CraftMCP.Rendering    Rendering and export-facing composition
  CraftMCP.Persistence  .craft packaging and document hydration
  CraftMCP.Agent        Agent integration boundary
tests/
  CraftMCP.Tests        Domain, export, and command contract coverage
docs/
  engineering-conventions.md
```

## What Exists Today

Today the repo already contains:

- a working desktop shell,
- foundational scene graph and document models,
- deterministic JSON export infrastructure,
- command-layer contracts for future execution and history behavior,
- test fixtures and automated coverage for the core domain.

What does not exist yet is the full command execution pipeline, undo/redo semantics, and the user-facing editing experience on top of those contracts.

## Near-Term Roadmap

The next major implementation work is centered on command execution:

1. command validation pipeline,
2. transactional command execution,
3. inverse generation and history behavior,
4. undo and redo flows,
5. continued rendering and persistence integration around the stable scene model.

## What CraftMCP Is Not

CraftMCP is not trying to be:

- a Figma replacement,
- a full vector illustration suite,
- a cloud-first collaboration platform,
- or an everything-in-one design product.

The scope stays intentionally narrow so the core human-plus-agent workflow can be strong.

## Contributing

Contributions are welcome, especially in areas such as:

- scene graph and document model design,
- deterministic JSON serialization,
- command schema and execution,
- rendering/export consistency,
- Windows-first UX,
- local asset and package handling.

See [docs/engineering-conventions.md](docs/engineering-conventions.md) for current repository conventions.

## Vision

CraftMCP should become a practical open-source design workspace for people who want to work with their own AI models locally, generate a strong first pass quickly, and still retain full human control over the final result.
