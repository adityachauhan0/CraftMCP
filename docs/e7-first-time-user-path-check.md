# E7 First-Time-User Path Check

## Purpose
Document the MVP release-gate path for a brand-new user and keep the success bar tied to the architecture note:

- produce a basic design in under 10 minutes
- keep the human-plus-agent loop understandable
- preserve direct human editing after agent work
- finish with portable exports

## Scope Of This Check
This pass documents the release script and the expected gate for a live stopwatch run.

Automated coverage in this repo now proves:
- fixture-backed social, UI mockup, and slide workflows can be opened, revised, and exported
- reopen, revise, and re-export remains stable
- agent review and direct human editing both stay available in the same workflow

The timing portion still requires a human release pass in the desktop app because the current harness does not automate Avalonia desktop interaction end to end.

## Release Script
Use this exact path for the live first-time-user check:

1. Launch CraftMCP.
2. Create a new document with the `Square Post` preset.
3. Add one visible shape or text element.
4. Submit a simple prompt that produces a reviewable proposal.
5. Review the proposal and approve it.
6. Make one direct human edit in the properties panel.
7. Save the document as `.craft`.
8. Export JSON.
9. Export PNG.
10. Reopen the saved `.craft` file and confirm the edited result still appears usable.

## Pass Criteria
- Total elapsed time: under 10 minutes
- No step requires hidden developer knowledge
- User can tell when a proposal is pending review
- User can tell what changed after approval
- User can still directly edit the result before export
- Save, reopen, JSON export, and PNG export all succeed without manual recovery

## Current Assessment
- Workflow clarity: `Pass in automated coverage`
- Human-plus-agent edit loop: `Pass in automated coverage`
- Save, reopen, and re-export: `Pass in automated coverage`
- Stopwatch timing: `Pending live desktop signoff`

## Known Friction To Watch During The Live Check
- The stale window-title `*` bug remains a tracked follow-up and should be observed during the run, but it is not a release blocker for E7.
- The live reviewer should confirm that proposal status, activity log provenance, and export actions remain easy to understand without prior repo context.

## Required Evidence For Release
- Start and finish timestamps from the live run
- The saved `.craft` file path
- The exported `.json` file path
- The exported `.png` file path
- Any observed friction notes, even if the run passes
