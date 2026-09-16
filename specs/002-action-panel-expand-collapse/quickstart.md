# Quickstart: Validate Action Panel Expand/Collapse

## Prerequisites

- .NET 9 SDK
- A modern browser with desktop and mobile responsive modes
- The repository restored successfully
- See [action-panel-ui.md](contracts/action-panel-ui.md) for expected behavior and [data-model.md](data-model.md) for state transitions

## Restore and Build

From the repository root:

```powershell
dotnet restore .\ContosoDashboard.sln
dotnet build .\ContosoDashboard.sln --no-restore
```

Expected outcome: both application and test projects build without warnings or errors introduced by this feature.

## Run Automated Tests

```powershell
dotnet test .\ContosoDashboard.Tests\ContosoDashboard.Tests.csproj --no-build
```

Expected component coverage:

- The panel starts expanded.
- One activation collapses the panel and a second activation expands it.
- The button's accessible name and `aria-expanded` value match each state.
- Links are not rendered or keyboard-reachable while collapsed.
- Existing links, active-route behavior, and Administrator-only report visibility are preserved when expanded.
- A simulated location change resets a collapsed panel to expanded.
- Event subscriptions are released when the layout is disposed.

## Run the Application

```powershell
dotnet run --project .\ContosoDashboard\ContosoDashboard.csproj
```

Open the local URL printed by the application and sign in using the existing training login.

## Scenario 1: Collapse and Expand

1. Open Dashboard and confirm the blue action panel shows its existing links and a control at the top-right.
2. Activate the control with a pointer.
3. Confirm only the control remains in the panel and the content below the header fills the released width.
4. Confirm the header dimensions and current page controls do not change behavior.
5. Activate the control again.
6. Confirm the panel returns to its original width, links return in their original order, and Dashboard remains active.

Expected outcome: both transitions settle within 500 milliseconds without overlap, clipped controls, or lost page state.

## Scenario 2: Keyboard and Accessibility

1. Use `Tab` to focus the panel control.
2. Confirm its visible focus indicator and accessible name communicate "Collapse action panel".
3. Press `Enter` or `Space` and confirm the panel collapses.
4. Confirm the same button remains focusable, reports "Expand action panel", and has `aria-expanded="false"`.
5. Continue tabbing and confirm hidden navigation links are not reached.
6. Expand the panel and confirm `aria-expanded="true"` and the collapse action name return.

Expected outcome: pointer and keyboard users complete each action with one activation and do not encounter hidden focus targets.

## Scenario 3: Navigation Reset

1. Collapse the panel on Dashboard.
2. Navigate to another screen using a link in the header or main content.
3. Confirm the destination screen displays the full expanded panel.
4. Collapse the panel, then use browser Back or Forward where available.
5. Confirm the newly displayed location shows the panel expanded.
6. Collapse the panel and refresh the page.
7. Confirm the refreshed page displays the panel expanded.

Expected outcome: collapsed state never persists across an application location change, direct opening, or refresh.

## Scenario 4: Link and Authorization Regression

1. While expanded, open Dashboard, My Tasks, My Projects, Documents, Team, Notifications, and Profile.
2. Confirm each link reaches the same destination and active-state styling remains correct.
3. Sign in as an Administrator and confirm Document Reports is visible and functional.
4. Sign in without the Administrator role and confirm Document Reports remains hidden.
5. Repeat a collapse/expand cycle and confirm visibility rules are unchanged.

Expected outcome: no navigation destination, role rule, or active-link behavior changes.

## Scenario 5: Responsive Layout

Validate at minimum these viewport sizes:

- 1440 x 900 desktop
- 1024 x 768 tablet landscape
- 390 x 844 mobile

At each size:

1. Capture the expanded state.
2. Collapse the panel and capture the collapsed state.
3. Expand it again.
4. Check that the panel, control, header, and content never overlap.
5. Check that long labels fit in expanded mode and are fully hidden in collapsed mode.
6. Enable reduced motion in browser or operating-system settings and confirm both actions still settle correctly.

Expected outcome: both states remain usable at every supported size, and reduced-motion preference does not prevent state changes.
