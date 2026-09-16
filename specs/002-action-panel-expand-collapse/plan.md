# Implementation Plan: Action Panel Expand/Collapse

> **Superseded:** Do not use this plan. The canonical plan is [003-action-panel-expand-collapse/plan.md](../003-action-panel-expand-collapse/plan.md).

**Branch**: `002-action-panel-expand-collapse` | **Date**: 2026-09-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-action-panel-expand-collapse/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add an accessible control to the blue action panel that toggles it between its existing 250px expanded form and a compact control-only rail. Keep the state local to `MainLayout`, use CSS flex sizing so the main content automatically occupies released space, and reset to expanded whenever `NavigationManager` reports a route change. Preserve `NavMenu` link destinations, authorization, active-state behavior, and page component state.

## Technical Context

**Language/Version**: C# 13, Razor, CSS on .NET 9  
**Primary Dependencies**: ASP.NET Core 9 Blazor Server, Bootstrap 5, Bootstrap Icons; bUnit for component rendering tests  
**Storage**: N/A; panel state is intentionally page-local and not persisted  
**Testing**: xUnit 2.9.2, bUnit component tests, and responsive browser validation  
**Target Platform**: Modern desktop and mobile web browsers supported by the existing Blazor Server application  
**Project Type**: Single ASP.NET Core web application with a separate test project  
**Performance Goals**: Visible feedback immediately on activation; transition settled within 500ms  
**Constraints**: Offline training environment; keyboard and pointer operation; no header, navigation, authorization, or page-state regressions; no overlap at supported widths  
**Scale/Scope**: One shared layout, one navigation component, shared site CSS, and focused component tests affecting all routed application screens

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Design Gate

- **Training-First Scope: PASS** - The feature is local UI behavior and requires no production identity, cloud service, or live operational data.
- **Secure-by-Default Learning: PASS** - Existing `AuthorizeView`, route authorization, and role-based link visibility remain unchanged.
- **Spec-Driven Delivery: PASS** - The approved scope and acceptance criteria are recorded in [spec.md](spec.md).
- **Test-First and Verifiable Behavior: PASS** - Component checks will cover initial state, toggling, route reset, accessible naming, and role-sensitive links before implementation; responsive validation covers visual layout behavior.
- **Simplicity, Clarity, and Maintainability: PASS** - State remains in the owning layout and styling remains in the existing stylesheet; no service, database, or speculative abstraction is added.

### Post-Design Gate

- **Training-First Scope: PASS** - Research and contracts introduce no external runtime dependency.
- **Secure-by-Default Learning: PASS** - The UI contract explicitly preserves existing link visibility and authorization behavior.
- **Spec-Driven Delivery: PASS** - Data-model, interface contract, and quickstart artifacts trace back to requirements and measurable outcomes.
- **Test-First and Verifiable Behavior: PASS** - The quickstart defines automated component and end-to-end responsive scenarios.
- **Simplicity, Clarity, and Maintainability: PASS** - The design is limited to layout state, navigation presentation, CSS, and focused tests.

## Project Structure

### Documentation (this feature)

```text
specs/002-action-panel-expand-collapse/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/
│   └── action-panel-ui.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Shared/
│   ├── MainLayout.razor       # Owns panel state, toggle, and route reset
│   └── NavMenu.razor          # Preserves existing authorized navigation links
└── wwwroot/
  └── css/
    └── site.css           # Expanded/collapsed sizing and transitions

ContosoDashboard.Tests/
├── ContosoDashboard.Tests.csproj
└── Shared/
  └── MainLayoutTests.cs     # Component state, accessibility, and navigation reset
```

**Structure Decision**: Use the existing single Blazor Server application and xUnit test project. The shared layout is the owning abstraction because it already composes the sidebar, header, and routed body; navigation behavior remains inside the existing menu.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations require justification.
