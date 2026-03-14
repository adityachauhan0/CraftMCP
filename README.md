![CraftMCP header](assets/cute_female_with_an_axe_rotations_8dir.gif)

# CraftMCP

CraftMCP is an open-source Windows design workspace for human and AI collaboration.

The project is focused on a simple idea: AI agents should be able to create and update designs locally, and human users should be able to inspect, edit, and export those same designs without leaving the workflow. CraftMCP is intentionally lightweight. It is not trying to become a full professional design suite.

## What CraftMCP Is For

CraftMCP is being designed for workflows such as:

- creating social graphics from prompts,
- generating UI mockups for apps,
- making slide-style layouts,
- reopening an existing design and refining it with an AI model,
- exporting structured design data so another agent can continue the work.

## Product Direction

The MVP is centered around a local single-canvas editor with:

- text, shape, image, and group primitives,
- direct human editing of AI-generated output,
- structured agent operations instead of brittle UI automation,
- PNG export for rendered output,
- deterministic JSON export for exact machine-readable design context.

The project is Windows-first, local-first, and open-source by design.

## Why This Exists

Most design software is built for direct human interaction, cloud collaboration, or broad professional feature depth. That is a poor fit for AI-driven visual workflows.

CraftMCP is aimed at a narrower but important problem: giving AI agents a reliable local design environment that still stays understandable and editable for humans. That requires:

- one clean scene model,
- deterministic operations,
- reversible edits,
- local file handling,
- and exports that preserve full structure instead of reducing everything to a screenshot.

## Current Status

The project has moved past the idea stage.

Current progress includes:

- product definition and MVP scope,
- architecture decisions for the first release,
- an implementation plan and engineering backlog,
- a .NET 8 solution with explicit project boundaries,
- a bootstrapped Avalonia desktop shell,
- foundational domain models for the scene graph,
- validation logic for document integrity,
- an initial automated test suite.

The current architecture baseline is:

- app shell: Avalonia + .NET 8,
- source of truth: one canonical `DocumentState`,
- editing model: the same scene model supports human edits and agent edits,
- persistence direction: `.craft` package with bundled assets,
- export rule: deterministic JSON is a release gate.

## Repository Structure

The repository is currently organized around clear boundaries:

- `src/CraftMCP.App` for the Avalonia shell,
- `src/CraftMCP.Domain` for IDs, value objects, scene models, and validation,
- `src/CraftMCP.Rendering` for rendering and export-facing composition,
- `src/CraftMCP.Persistence` for `.craft` packaging and hydration,
- `src/CraftMCP.Agent` for the agent integration boundary,
- `tests/CraftMCP.Tests` for domain and integration coverage,
- `docs/engineering-conventions.md` for repository conventions.

## What Makes CraftMCP Different

CraftMCP is intentionally constrained.

It is not trying to be:

- a Figma replacement,
- a full vector illustration suite,
- a cloud-first collaboration platform,
- or a product packed with every design feature under the sun.

The goal is to be a practical open-source tool people can pair with their own AI models while still keeping direct human control over the final design.

## Near-Term Roadmap

The next implementation wave is focused on:

1. finalizing the v1 JSON export contract,
2. adding deterministic JSON fixture coverage,
3. defining command contracts for edits,
4. extending persistence, rendering, and agent execution around the shared scene model.

## Contributing

Contributions are welcome as the project moves through the core implementation phases.

High-value areas include:

- scene graph and document model design,
- deterministic JSON serialization,
- command schema and command execution,
- rendering and export consistency,
- Windows-first UX,
- local asset and package handling.

## Vision

CraftMCP should become a practical open-source design workspace for people who want to work with their own AI models locally, generate a strong first pass quickly, and still retain full human control over the final result.
