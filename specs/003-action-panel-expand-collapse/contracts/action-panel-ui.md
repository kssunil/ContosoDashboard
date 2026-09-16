# UI Contract: Action Panel Expand/Collapse

## Boundary

This contract governs the shared action-panel layout on routed application screens. Standalone authentication pages are outside scope. Existing routes, authorization, active-link behavior, and page controls are unchanged.

## Initial Expanded State

- The panel uses its existing expanded width.
- The 3.5rem panel brand row and 3.5rem main account header retain their existing heights and behavior.
- Brand content and all links authorized for the current user are visible in existing order.
- The control appears at the panel's top-right without covering brand content or links.
- The control is named "Collapse action panel", reports `aria-expanded="true"`, and identifies the controlled navigation region.

## Collapse Interaction

After one pointer, Enter, or Space activation:

- The panel becomes a compact control-only rail.
- Brand content and all navigation links are hidden and unavailable to keyboard focus.
- Brand-row height remains stable and the control remains visible.
- The control is named "Expand action panel" and reports `aria-expanded="false"`.
- Main content below the account header fills the released width.
- Current routed body state, control behavior, active destination, and authorization are unchanged.

## Expand Interaction

After one activation from collapsed state:

- The panel returns to its existing size.
- Brand content and authorized links return in their existing order.
- The control returns to "Collapse action panel" and `aria-expanded="true"`.
- Main content returns to its original size and position.

## Navigation State Contract

- Navigation to a different normalized route path resets the panel to expanded.
- Query-string-only changes retain current panel state.
- Fragment-only changes retain current panel state.
- Direct opening and refresh create an expanded panel.
- Equivalent root or trailing-slash path forms do not count as another screen.

## Preserved Links

| Label | Route | Existing visibility |
|---|---|---|
| Dashboard | `/` | Existing application visibility |
| My Tasks | `/tasks` | Existing application visibility |
| My Projects | `/projects` | Existing application visibility |
| Documents | `/documents` | Existing application visibility |
| Document Reports | `/documents/reports` | Administrator only |
| Team | `/team` | Existing application visibility |
| Notifications | `/notifications` | Existing application visibility |
| Profile | `/profile` | Existing application visibility |

## Responsive and Motion Contract

- Both states remain usable without overlap at the required 390x844, 1024x768, and 1440x900 viewports.
- Required validation sizes are 390x844, 1024x768, and 1440x900.
- Expanded mode retains existing mobile navigation behavior.
- Collapsed mobile mode leaves the panel control visible while brand and navigation content are hidden.
- Normal transitions settle within 500ms.
- Reduced-motion preference produces the same final layout without depending on animation.

## Accessibility Contract

- The control is a native button with visible keyboard focus.
- Pointer, Enter, and Space each perform one state change.
- Accessible name and `aria-expanded` always match the available action and current state.
- Hidden navigation content is not reachable by keyboard or assistive-technology navigation.
- Focus is not trapped or unexpectedly moved when panel state changes.
