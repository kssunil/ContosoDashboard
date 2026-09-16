# Phase 0 Research: Action Panel Expand/Collapse

## Decision 1: Keep transient state in MainLayout

- **Decision**: Store `IsActionPanelExpanded` and the current normalized route path in `MainLayout`.
- **Rationale**: The layout owns the sidebar and main-content geometry. Layout-local state preserves routed page component state during a toggle and naturally starts expanded after a full refresh or direct opening.
- **Alternatives considered**: A shared state service and browser persistence were rejected because state must not survive another screen or session. Per-page state was rejected because it duplicates shared behavior.

## Decision 2: Reset only when the normalized route path changes

- **Decision**: Observe `NavigationManager.LocationChanged`, derive the application-relative path without query or fragment, normalize root and trailing slashes, and reset only when that normalized path differs from the previous path. Unsubscribe when the layout is disposed.
- **Rationale**: Blazor can retain the layout across routed pages. Comparing only normalized paths implements the specification while allowing query filters and in-page fragments to retain panel state.
- **Alternatives considered**: Resetting on every location event was rejected because it violates query/fragment retention. Resetting only from panel-link clicks was rejected because it misses programmatic navigation, browser history, and links elsewhere in the application.

## Decision 3: Preserve the existing navigation component and authorization boundaries

- **Decision**: Add presentation parameters to `NavMenu` so its brand-row space remains stable while brand content and navigation links are hidden in the collapsed state. Keep every existing `NavLink` and the Administrator `AuthorizeView` unchanged.
- **Rationale**: This preserves destinations, ordering, active-state matching, and role visibility while ensuring hidden links are absent from keyboard navigation.
- **Alternatives considered**: Rebuilding the menu in `MainLayout` was rejected because it duplicates navigation and authorization. CSS-only visual hiding was rejected unless it also reliably removes hidden links from keyboard access.

## Decision 4: Use one native button in the panel header area

- **Decision**: Render one semantic button at the panel's top-right. Use directional Bootstrap Icons, state-specific accessible names, `aria-expanded`, and `aria-controls` targeting the navigation region.
- **Rationale**: A native button supports pointer, Enter, and Space without custom keyboard code. State-specific semantics communicate both the current relationship and available action.
- **Alternatives considered**: A clickable image, `div`, or literal-only glyph was rejected because it provides weaker default semantics and focus behavior.

## Decision 5: Extend the existing flex layout with stable CSS states

- **Decision**: Toggle expanded/collapsed classes on the page and sidebar, retain the 250px desktop expanded width, use a compact control-sized collapsed rail, and keep `main` flexible. Preserve both 3.5rem header-row heights and suppress overflow during transitions.
- **Rationale**: The existing desktop layout already uses a fixed sidebar and flexible main area. Class-based widths let CSS perform layout without DOM measurement and keep transitions below 500ms.
- **Alternatives considered**: JavaScript width calculations and inline styles were rejected as unnecessary. Removing the sidebar with `display: none` was rejected because the expansion control must remain available.

## Decision 6: Integrate with the existing mobile navigation behavior

- **Decision**: Retain the existing mobile menu checkbox behavior while applying action-panel state independently. At mobile widths, the collapsed state hides brand and navigation content but leaves the action-panel button visible; expansion restores the existing mobile brand row and menu behavior.
- **Rationale**: This avoids replacing an existing interaction and supports the required 390px viewport without overlapping controls.
- **Alternatives considered**: Disabling collapse on mobile was rejected by the viewport requirement. Replacing the mobile menu was rejected as out of scope and a navigation regression risk.

## Decision 7: Respect reduced-motion preferences

- **Decision**: Use a short width transition for normal motion settings and disable or minimize it under `prefers-reduced-motion: reduce` while preserving identical final states.
- **Rationale**: This satisfies the 500ms target without making animation a prerequisite for understanding or using the control.
- **Alternatives considered**: A mandatory animation was rejected for accessibility; no visible transition at all was rejected because the specification asks for immediate visible feedback and a settled transition.

## Decision 8: Add focused bUnit and browser validation

- **Decision**: Add bUnit to the existing xUnit project for initial state, toggle semantics, hidden links, authorized links, route-path reset, query/fragment retention, page-body preservation, and event disposal. Use browser validation for geometry, focus visibility, motion preference, and named page workflows.
- **Rationale**: bUnit verifies actual Razor behavior and interactions; browser checks cover visual properties a component renderer cannot establish reliably.
- **Alternatives considered**: Raw class unit tests were rejected because they would not test rendered accessibility or navigation. Browser-only testing was rejected because it provides slower and less focused regression feedback.
