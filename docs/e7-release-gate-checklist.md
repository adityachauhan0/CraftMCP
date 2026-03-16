# E7 MVP Release Gate Checklist

## Goal
Ship the MVP only if the three core workflows, reopen flow, export guarantees, and the human-plus-agent editing loop all hold together without expanding scope.

## Evidence Map
### Automated workflow evidence
- `tests/CraftMCP.Tests/Unit/App/WorkflowValidationTests.cs`
  - social graphic fixture workflow
  - UI mockup fixture workflow
  - slide fixture workflow
  - reopen, revise, and re-export workflow
- `tests/CraftMCP.Tests/Unit/App/WorkspaceViewModelTests.cs`
  - stale window-title `*` regression check on new-document creation

### Existing deterministic export and portability evidence
- `tests/CraftMCP.Tests/Unit/Exports/DocumentJsonExporterTests.cs`
- `tests/CraftMCP.Tests/Unit/Persistence/CraftPackageRoundTripTests.cs`
- `tests/CraftMCP.Tests/Unit/Rendering/DocumentPngExporterTests.cs`

## Release Gates
Mark each gate `Go`, `No-Go`, or `Pending Human Check`.

| Gate | Status | Evidence |
| --- | --- | --- |
| Social graphic workflow completes with review, human edit, and export | Go | `WorkflowValidationTests.SocialGraphicFixture_WorkflowSupportsAgentReviewManualRefinementAndExport` |
| UI mockup workflow keeps hierarchy legible and exports structured JSON | Go | `WorkflowValidationTests.UiMockupFixture_WorkflowKeepsLayerHierarchyLegibleAndExportsStructuredJson` |
| Slide workflow supports review, human revision, and export | Go | `WorkflowValidationTests.SlideFixture_WorkflowSupportsCanvasProposalHumanRevisionAndExport` |
| Reopen, revise, and re-export preserves IDs, hierarchy, and assets | Go | `WorkflowValidationTests.ReopenReviseAndReExport_PreservesIdsHierarchyAndPackagedAssets` |
| Deterministic JSON export remains byte-stable | Go | `DocumentJsonExporterTests` |
| PNG export matches canvas dimensions and excludes overlays | Go | `DocumentPngExporterTests` |
| Undo and redo remain stable in normal validation flow | Go | social workflow validation test plus command-history coverage |
| Human user can directly edit agent output before export | Go | workflow validation tests |
| Project packages remain portable with packaged assets | Go | round-trip persistence coverage plus reopen validation |
| First-time user can complete the basic path inside 10 minutes | Pending Human Check | `docs/e7-first-time-user-path-check.md` |

## Non-Blocking Follow-Up
- Stale window-title `*` after fresh document creation
  - Current handling: tracked follow-up, not a release blocker for E7
  - Regression guard: `WorkspaceViewModelTests.CreateNewDocument_ClearsStaleDirtyMarkerFromDocumentTitle`

## Scope Cuts For CM-052
These items stay out of the MVP release hardening pass unless they become true release blockers:

- Fixing the stale window-title `*` bug beyond the regression guard
- Additional UI polish beyond current workflow clarity
- Broader planner intelligence beyond the current reviewable local-command mapping
- New editing tools, node types, or richer layout features
- Expanded onboarding instrumentation beyond the documented first-time-user release script

## No-Go Conditions
Do not ship the MVP if any of these occur:

- JSON export stops being deterministic
- PNG export stops matching the current canvas dimensions or loses portability
- Reopened `.craft` files lose IDs, hierarchy, or packaged assets
- Human edits bypass the command/history path
- Agent proposals mutate state without review
- A scope cut weakens deterministic export, undo or redo integrity, portability, or direct human editability

## Release Decision Record
Before release, record:

- verification command output
- first-time-user live timing result
- any remaining known issues accepted as non-blocking
- the exact note path for the release-hardening report in Obsidian
