# Implementation Plan: Action Panel Expand/Collapse

**Branch**: `003-action-panel-expand-collapse` | **Date**: 2026-09-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-action-panel-expand-collapse/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add an accessible control that toggles the shared blue action panel between its existing 250px expanded form and a compact control-only rail. Keep transient state in `MainLayout`, preserve the existing authorized `NavMenu`, use flex sizing for the main content, and reset only when the normalized route path changes. Query-string and fragment-only changes retain panel state.

## Technical Context

**Language/Version**: C# 13, Razor, CSS on .NET 9  
**Primary Dependencies**: ASP.NET Core 9 Blazor Server, Bootstrap 5, Bootstrap Icons; bUnit added for rendered-component tests  
**Storage**: N/A; panel state is transient and layout-local  
**Testing**: xUnit 2.9.2, bUnit component tests, and browser-based responsive/accessibility validation  
**Target Platform**: Modern web browsers at the required 390x844, 1024x768, and 1440x900 viewports  
**Project Type**: Single ASP.NET Core web application with a separate test project  
**Performance Goals**: Immediate feedback with final layout settled within 500ms  
**Constraints**: Offline training environment; pointer, Enter, and Space operation; preserve both header rows, page state, routes, active links, and authorization; query/fragment changes do not reset state  
**Scale/Scope**: One shared layout, one navigation component, shared CSS, one test fixture, and focused component tests affecting all routed screens using `MainLayout`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Design Gate

- **Training-First Scope: PASS** - The feature is local presentation behavior with no production identity, external service, or live data dependency.
- **Secure-by-Default Learning: PASS** - Existing `AuthorizeView` role visibility and route authorization remain unchanged and receive explicit regression coverage.
- **Spec-Driven Delivery: PASS** - [spec.md](spec.md) defines bounded routes, headers, viewports, authorization behavior, and measurable acceptance criteria.
- **Test-First and Verifiable Behavior: PASS** - Failing component tests will precede implementation; browser scenarios cover layout, keyboard behavior, and named page-state regressions.
- **Simplicity, Clarity, and Maintainability: PASS** - State stays in the owning layout and styling stays in the existing stylesheet; no service, database, or persistence abstraction is added.

### Post-Design Gate

- **Training-First Scope: PASS** - Design artifacts retain the offline sample scope.
- **Secure-by-Default Learning: PASS** - The UI contract preserves Administrator-only Document Reports visibility in both states.
- **Spec-Driven Delivery: PASS** - Research, state model, UI contract, and quickstart trace the 18 requirements and 8 success criteria.
- **Test-First and Verifiable Behavior: PASS** - The validation guide covers component tests, required viewport sizes, route-path semantics, page-state retention, and moderated first use.
- **Simplicity, Clarity, and Maintainability: PASS** - The design touches only the shared layout, menu presentation, stylesheet, and focused test support.

## Project Structure

### Documentation (this feature)

```text
specs/003-action-panel-expand-collapse/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/
│   └── action-panel-ui.md
├── checklists/
│   └── first-use-validation.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Shared/
│   ├── MainLayout.razor       # Owns state, toggle, route-path reset, disposal
│   └── NavMenu.razor          # Preserves existing authorized links and brand row
└── wwwroot/
    └── css/
        └── site.css           # Stable widths, transitions, focus, responsive rules

ContosoDashboard.Tests/
├── ContosoDashboard.Tests.csproj
└── Shared/
    ├── MainLayoutTestContext.cs
    └── MainLayoutTests.cs
```

**Structure Decision**: Extend the existing Blazor Server application and xUnit project. `MainLayout` owns changing geometry and route observation, while `NavMenu` retains current link and authorization definitions.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations require justification.
