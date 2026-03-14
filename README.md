# CraftMCP

CraftMCP is an open-source Windows design tool for working with AI models on visual design tasks.

It is being built as a lightweight local workspace where AI agents can generate and edit designs, and human users can directly refine the result. The focus is practical collaboration with your own models, not feature parity with tools like Figma or Illustrator.

## Overview

CraftMCP is meant to support workflows like:

- creating social graphics from prompts,
- generating UI mockups for apps,
- making slide-style layouts,
- reopening an existing design and iterating with an AI model,
- exporting structured design data so another agent can continue the work.

The project is Windows-first, local-first, and open-source.

## Goals

The MVP is intended to provide:

- a canvas-based desktop app,
- basic design primitives such as text, shapes, and images,
- structured agent operations instead of brittle UI automation,
- direct human editing of AI-generated output,
- PNG export for final output,
- JSON export for exact machine-readable design context.

## Why This Project

Most design software is built either for direct human use or for cloud collaboration. CraftMCP is aimed at a different problem: giving AI agents a reliable local design environment that still stays understandable and editable for humans.

That means a few things matter here from the start:

- local files and assets,
- a clean scene model,
- deterministic actions,
- reversible edits,
- and exports that preserve full design structure.

## What CraftMCP Is

CraftMCP is:

- lightweight,
- local-first,
- open-source,
- built for human and AI collaboration,
- focused on structured design editing rather than broad suite features.

## What CraftMCP Is Not

CraftMCP is not trying to be:

- a full Figma replacement,
- a professional print design suite,
- a cloud-first collaboration platform,
- or an everything-in-one graphics product.

The project is intentionally constrained so it stays useful and buildable.

## Planned MVP Surface

- Canvas with common presets
- Text, shape, and image elements
- Layer panel and property editing
- Prompt input for agent instructions
- Human editing of AI output
- Save/load for local project files
- PNG export
- Deterministic JSON export of the scene

## JSON Export

JSON export is a core part of the project.

The intent is to preserve the full design context so agents can work with the actual scene rather than inferring from screenshots. That includes things like:

- canvas dimensions,
- layers and hierarchy,
- element types,
- positions and sizes,
- text content and styling,
- image references,
- ordering and grouping.

## Status

The project is currently in product-definition stage. The MVP scope, use cases, and JSON export direction have been defined. Architecture and implementation planning are the next steps.

## Contributing

Contributions are welcome as the project moves into architecture and implementation.

The most useful areas are likely to be:

- desktop app architecture,
- scene graph and document model design,
- command schema for agent operations,
- deterministic JSON serialization,
- Windows-first UX,
- and local asset/file handling.

## Vision

CraftMCP should become a practical open-source design workspace for people who want to use their own AI models locally, generate a first pass quickly, and keep direct human control over the final design.
