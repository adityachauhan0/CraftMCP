# CraftMCP Rendering Approach

## Decision
CraftMCP will use a single render-plan pipeline for both viewport rendering and PNG export:

1. Traverse canonical `DocumentState` into a deterministic ordered render plan.
2. Resolve packaged image bytes through a render asset source.
3. Draw the scene with one Skia-backed raster renderer.
4. Apply overlays only as a separate opt-in pass.
5. Encode PNG from the same base scene render with overlays disabled.

## Why This Path
- It preserves one scene interpretation for viewport and export.
- It keeps rendering driven by canonical `DocumentState`, not editor/session state.
- It avoids filesystem-coupled image loading by requiring packaged asset bytes.
- It is testable in-process without starting workspace editing flows.

## Tradeoffs Considered
### Direct Avalonia `DrawingContext` rendering
- Pros: closer to a future custom canvas control.
- Cons: harder to verify headlessly, and export parity would depend on separate off-screen control wiring.

### Shared Skia-backed renderer hosted by Avalonia
- Pros: one renderer for viewport and PNG, deterministic test surface, direct PNG encoding path.
- Cons: the viewport host will need an adapter layer in the app when richer canvas interaction arrives.

## Chosen Boundary
- `DocumentRenderPlanBuilder` owns deterministic traversal from `DocumentState`.
- `SkiaDocumentRenderer` owns drawing v1 nodes from that plan.
- `DocumentPngExporter` encodes PNG by calling the same renderer with overlays disabled.
- `RenderOverlayState` stays outside the document model so selection/guides cannot leak into export output.

## Known Ambiguity Resolved
The architecture notes required an Avalonia-compatible rendering approach but did not specify whether the shared implementation should be `DrawingContext`-first or renderer-first. This phase chooses renderer-first so export parity is exact and future viewport hosting can stay a thin adapter over the same render plan.
