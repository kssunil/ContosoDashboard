# Quickstart: Validate Action Panel Expand/Collapse

## Prerequisites

- .NET 9 SDK
- A modern browser with responsive and reduced-motion emulation
- Existing training users for Administrator and non-Administrator roles
- See [data-model.md](data-model.md) and [action-panel-ui.md](contracts/action-panel-ui.md)

## Restore and Build

```powershell
dotnet restore .\ContosoDashboard.sln
dotnet build .\ContosoDashboard.sln --no-restore
```

Expected: application and test projects build without feature-related errors or warnings.

## Automated Tests

```powershell
dotnet test .\ContosoDashboard.Tests\ContosoDashboard.Tests.csproj --no-build
```

Expected coverage:

- Initial expanded state and state-specific button semantics
- One-activation collapse and expansion
- Hidden content removed from keyboard navigation
- Existing link order, destinations, active state, and Administrator visibility
- Route-path reset and query/fragment retention
- Routed body state preservation and navigation-event disposal

## Run the Application

```powershell
dotnet run --project .\ContosoDashboard\ContosoDashboard.csproj
```

Open the local URL printed by the application and use the existing training login.

## Scenario 1: Reversible Panel Interaction

1. Open Dashboard and confirm the full blue panel, brand row, authorized links, and top-right control.
2. Activate the control with a pointer and confirm only the control remains visible in the panel.
3. Confirm main content fills the released width and both header rows retain their heights.
4. Activate the control again and confirm the original panel and content sizes return.
5. Repeat for five cycles and confirm no overlap or unstable state.

Expected: each action settles within 500ms and the final activation determines the stable state.

## Scenario 2: Keyboard and Assistive Semantics

1. Focus the control with `Tab` and confirm a visible focus indicator.
2. Confirm the expanded action name is "Collapse action panel" and `aria-expanded="true"`.
3. Press `Enter`, then confirm the collapsed name and `aria-expanded="false"`.
4. Confirm hidden links are not reached while tabbing.
5. Press `Space` on the control and confirm the panel expands once.

Expected: pointer, Enter, and Space each perform one action without hidden focus targets.

## Scenario 3: Route-Path Reset

For each documented destination (`/`, `/tasks`, `/projects`, `/documents`, `/documents/reports` as Administrator, `/team`, `/notifications`, `/profile`):

1. Open the destination directly and confirm the panel starts expanded.
2. Collapse it and refresh; confirm it starts expanded.
3. Collapse it and navigate from that path to another destination; confirm the destination starts expanded.
4. Collapse it, make one query-only change, and confirm it remains collapsed.
5. Make one fragment-only change on the same path and confirm it remains collapsed.

Expected: path changes reset; query-only and fragment-only changes retain state.

## Scenario 4: Authorization and Navigation Regression

1. As an Administrator, verify all eight documented links, destinations, order, and active indication before and after a collapse/expand cycle.
2. As a non-Administrator, verify the seven authorized links and confirm Document Reports remains absent before, during, and after a cycle.
3. Confirm no panel action changes authorization or destination behavior.

Expected: 100% of existing link and role rules are preserved.

## Scenario 5: Main-Content State Regression

Perform five collapse/expand cycles in each workflow:

1. Enter an unsaved editable value on Profile.
2. Apply a filter on Documents.
3. Interact with a task control on My Tasks without navigating away.

Expected: the named state and control behavior remain unchanged after every cycle.

## Scenario 6: Responsive and Reduced Motion

Validate 390x844, 1024x768, and 1440x900:

1. Capture expanded and collapsed states.
2. Confirm panel, button, both headers, and interactive content do not overlap or clip.
3. Confirm existing mobile menu behavior returns when the panel is expanded at 390x844.
4. Enable reduced motion and repeat both actions.

Expected: all three sizes remain usable and reduced motion reaches identical final states.

## Scenario 7: Moderated First Use

1. Recruit at least 10 representative dashboard users who have not used this feature.
2. Ask each participant to make more room for main content without identifying the control.
3. Record whether the participant independently identifies and activates the correct control.

Expected: at least 90% complete the action without assistance.
