# UI Contract: Action Panel Expand/Collapse

## Contract Boundary

This contract defines the observable behavior of the shared action panel and main content layout. It does not change application routes, authorization rules, page controls, or stored data.

## Initial State

When any routed screen, direct URL, refresh, or not-found screen is displayed:

- The action panel is expanded at its original width.
- Existing links are visible subject to their existing authorization rules.
- The control is visible at the panel's top-right.
- The control's accessible action name is "Collapse action panel".
- The control reports `aria-expanded="true"`.
- The header and main content use their existing expanded-panel layout.

## Collapse Interaction

When the user activates the control in the expanded state:

- The panel enters the collapsed state after one activation.
- All navigation links and their labels are hidden and unavailable to keyboard focus.
- Only the control remains visible in the panel.
- The control's accessible action name changes to "Expand action panel".
- The control reports `aria-expanded="false"`.
- The main content below the header expands into the released horizontal space.
- Existing page component state, controls, focus behavior, and authorization remain unchanged.

## Expand Interaction

When the user activates the control in the collapsed state:

- The panel returns to its original expanded width after one activation.
- All navigation links allowed for the current user are restored in their original order.
- The control's accessible action name changes to "Collapse action panel".
- The control reports `aria-expanded="true"`.
- The main content returns to its original width and position.
- The active `NavLink` destination remains indicated.

## Navigation Reset

When the application location changes through a panel link, another application link, programmatic navigation, browser history, or direct entry:

- The destination screen displays the panel expanded.
- Existing route outcomes and authorization behavior are unchanged.
- A refresh displays the panel expanded.

## Link Preservation

The expanded panel retains these existing links and route contracts based on security:

| Label | Route | Visibility |
|---|---|---|
| Dashboard | `/` | Existing authenticated application visibility |
| My Tasks | `/tasks` | Existing authenticated application visibility |
| My Projects | `/projects` | Existing authenticated application visibility |
| Documents | `/documents` | Existing authenticated application visibility |
| Document Reports | `/documents/reports` | Administrator role only |
| Team | `/team` | Existing authenticated application visibility |
| Notifications | `/notifications` | Existing authenticated application visibility |
| Profile | `/profile` | Existing authenticated application visibility |

## Layout and Timing

- The header retains its existing height and behavior in both states.
- The panel, control, and main content do not overlap at supported viewport widths.
- The main content remains flexible rather than using a fixed replacement width.
- Visible feedback begins immediately and the final layout settles within 500 milliseconds.
- Reduced-motion preferences yield the same final state without requiring animation.

## Accessibility

- The control is a semantic button operable by pointer, `Enter`, and `Space`.
- The button exposes its current action through an accessible name.
- `aria-expanded` matches the panel state.
- Keyboard focus remains visible and is not trapped in the panel.
- Hidden links are not reachable while the panel is collapsed.
