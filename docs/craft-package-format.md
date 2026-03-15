# `.craft` Package Format

## Overview
CraftMCP persists editable projects as a zip-based `.craft` package.

The package is the persistence format for canonical `DocumentState`. It is not the export JSON format.

## Required entries
- `document.json`
- `meta.json`

## Reserved entries
- `assets/<content-hash>.<ext>`

## Optional entries
- `preview.png`

## `document.json`
- Stores canonical `DocumentState` serialized with the default domain serializer.
- Preserves stable `doc_`, `node_`, and `asset_` identifiers unchanged.
- Keeps schema version information in the document payload.
- Does not contain export JSON, render output, UI state, or absolute source asset paths.

## `meta.json`
`meta.json` is a small package-level contract with:
- `packageVersion`
- `documentSchemaVersion`
- `documentId`
- `assetCount`

`packageVersion` versions the package container itself. `documentSchemaVersion` versions the canonical document payload.

## Asset packaging
- Assets are stored under `assets/`.
- Asset entry names are derived from manifest metadata as `assets/{contentHash}{originalExtension}`.
- Image nodes continue to reference `AssetId`.
- The persistence boundary resolves `AssetId` to packaged payload bytes.
- The writer must not persist arbitrary absolute source paths.

## Load behavior
The reader performs structure validation before hydration.

Fatal package errors:
- missing `document.json`
- missing `meta.json`
- invalid JSON in required entries
- unsupported `packageVersion`
- mismatch between `meta.json` and `document.json` schema or document identity
- invalid `DocumentState` after hydration

Warn-and-hydrate conditions:
- manifest is valid but an expected packaged asset entry is missing
- packaged asset bytes are unreadable or hash-mismatched
- extra orphan payloads exist under `assets/`

Warnings do not change canonical document structure. They only affect the loaded packaged asset payload set returned by persistence.

## Save behavior
`.craft` writes must be atomic enough to avoid corrupting an existing package.

Required save flow:
1. Build the full zip in a temp file in the target directory.
2. Flush and close the temp file completely.
3. Replace the existing target with `File.Replace`, or move into place if the target does not yet exist.
4. Delete temp artifacts on failure.

The writer must never update an existing `.craft` archive in place.
