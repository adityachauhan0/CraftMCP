# Engineering Conventions

## Project boundaries
- `CraftMCP.App`: Avalonia shell and future workspace composition only.
- `CraftMCP.Domain`: canonical persisted document state, value objects, IDs, and validation.
- `CraftMCP.Rendering`: render interpretation and export-facing composition.
- `CraftMCP.Persistence`: `.craft` packaging and document hydration boundaries.
- `CraftMCP.Agent`: prompt-planning integration boundary.
- `CraftMCP.Tests`: domain, fixture, and integration coverage.

## Nullability and language
- Nullable reference types stay enabled for every project.
- Shared value objects and document models should prefer immutable records or readonly record structs.

## Stable IDs
- Persisted entities use opaque prefixed string IDs: `doc_`, `node_`, and `asset_`.
- IDs are generated once and survive save, load, and export unchanged.
- UI/session state must not generate alternate identity systems.

## Serialization defaults
- `System.Text.Json` is the baseline serializer for the domain model.
- JSON names should stay camelCase.
- Property shape should remain deterministic by keeping models explicit and avoiding dynamic dictionaries beyond stable ID registries.

## Fixture layout
- `tests/CraftMCP.Tests/Fixtures/json`
- `tests/CraftMCP.Tests/Fixtures/craft`
- `tests/CraftMCP.Tests/Fixtures/render`

## Architectural guardrails
- `DocumentState` remains the canonical persisted source of truth.
- Mutation logic does not belong in the UI layer.
- Session-only state such as selection, viewport, hover, and tool mode stays outside persisted domain models.
