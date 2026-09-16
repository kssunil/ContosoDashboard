# Phase 0 Research: Action Panel Expand/Collapse

## Decision 1: Own panel state in the shared layout

- **Decision**: Store a boolean expanded/collapsed presentation state in `MainLayout`, which already composes the sidebar, header, and routed body.
- **Rationale**: The layout controls both areas whose sizes change. Keeping the state there avoids coupling routed pages or business services to a transient presentation concern and preserves each page component instance while the layout changes.
- **Alternatives considered**: A scoped state service was rejected because the state must not persist across screens. Browser storage was rejected because refresh and direct navigation must reset the panel. Per-page state was rejected because it duplicates behavior and cannot consistently control the shared sidebar.

## Decision 2: Reset on Blazor route changes

- **Decision**: Subscribe the layout to `NavigationManager.LocationChanged`, set the panel to expanded when the URI changes, request a render, and unsubscribe when the layout is disposed.
- **Rationale**: Blazor commonly retains the layout while replacing routed page content, so component initialization alone does not guarantee a reset between screens. The location event covers action-link navigation and programmatic navigation while explicit disposal prevents a retained event handler.
- **Alternatives considered**: Resetting only when a navigation link is clicked was rejected because it misses programmatic navigation, browser history, and links outside the side panel. Forcing full page loads was rejected because it changes existing navigation behavior and loses page state unnecessarily.

## Decision 3: Use layout classes and flex sizing

- **Decision**: Toggle a state class on the page/sidebar and define stable expanded and collapsed widths in the existing stylesheet. Keep `main` flexible so it automatically fills the remaining width, and constrain overflow during the transition.
- **Rationale**: The current desktop layout already uses a flex row with a fixed 250px sidebar and flexible main region. Class-driven sizing is a small extension of that pattern and does not require scripting to calculate content dimensions.
- **Alternatives considered**: Inline width styles were rejected because responsive and reduced-motion rules belong in CSS. JavaScript DOM measurement was rejected because the widths are known and CSS flexbox already performs the required layout. Removing the sidebar from layout with `display: none` was rejected because the expansion control must remain visible.

## Decision 4: Keep one accessible control in both states

- **Decision**: Render one semantic button at the panel's top-right with a Bootstrap Icon where available, an explicit accessible name that changes between "Collapse action panel" and "Expand action panel", and `aria-expanded` reflecting state.
- **Rationale**: A native button supports pointer and keyboard activation without custom key handling. A state-specific name communicates the available action, while `aria-expanded` provides the current relationship state to assistive technology.
- **Alternatives considered**: Literal `<>` text remains acceptable under the specification but is less immediately recognizable than directional icons. A clickable `div` or image was rejected because it requires recreating button semantics and keyboard behavior.

## Decision 5: Preserve navigation authorization and active state

- **Decision**: Leave existing `NavLink` elements and `AuthorizeView` boundaries intact. Collapse them through panel presentation state rather than rebuilding or changing their destinations.
- **Rationale**: This preserves route matching, role-sensitive visibility, destinations, and authorization behavior. Hiding the navigation container while collapsed also removes hidden links from keyboard navigation when the implementation uses conditional rendering or the HTML `hidden` contract.
- **Alternatives considered**: Replacing links with icon-only navigation was rejected because the requirement says only the expansion control remains in collapsed mode. Moving links into a new component tree was rejected because it increases regression risk without adding value.

## Decision 6: Respect responsive and motion preferences

- **Decision**: Use stable responsive widths for both states, prevent sidebar/content overlap, and disable or minimize width transitions under `prefers-reduced-motion: reduce`.
- **Rationale**: The feature applies at supported viewport sizes and specifies a 500ms maximum. Stable dimensions avoid layout shifts, while reduced-motion handling preserves accessibility without changing the final state.
- **Alternatives considered**: Desktop-only behavior was rejected because the success criteria cover supported viewport sizes. Viewport-proportional font or control scaling was rejected because it risks unstable sizing and text fit.

## Decision 7: Add focused Blazor component tests with bUnit

- **Decision**: Add bUnit to the existing xUnit test project for rendered-component checks of default state, toggle semantics, hidden links, route reset, and role-sensitive link preservation; supplement with responsive browser validation from the quickstart.
- **Rationale**: Existing tests do not include a component renderer, and raw xUnit cannot directly assert Razor output or interactions. bUnit is purpose-built for Blazor and allows behavior-first tests without introducing an application runtime dependency.
- **Alternatives considered**: Extracting panel state into a separately unit-tested class was rejected as an abstraction created only to avoid testing the real component. Browser-only manual testing was rejected because it does not satisfy repeatable regression coverage. A full browser automation stack was rejected as disproportionate for this narrow feature.
