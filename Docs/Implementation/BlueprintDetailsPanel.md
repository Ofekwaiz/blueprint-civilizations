# Blueprint Details Panel

## Purpose

The Blueprint Details Panel is a modular UI Toolkit projection displayed beside the Nexus Blueprint Board. It explains the currently selected Blueprint for production planning. It does not execute production, combat, progression, economy, or shop rules.

When no Blueprint is selected it displays: `Select a Blueprint to inspect its production and combat profile.` The Board remains the primary panel. On wide layouts the Details Panel sits to its right; when width is constrained, USS flex wrapping moves it below the Board. Its content region scrolls vertically.

## Runtime composition

- `BlueprintBoardPanelController` owns and disposes the paired Board and Details presenters.
- `BlueprintBoardPresenter` is the sole owner of the stable selected Blueprint definition/instance ID used by Milestone 1.
- `BlueprintDetailsPresenter` subscribes to the Board presenter's `SelectionChanged` event and to successful `BlueprintPlacementService` events.
- `IBlueprintDetailsResolver` converts the selected `BlueprintState`, current `BlueprintBoardState`, immutable catalog definitions, and adjacency queries into an immutable view model.
- `BlueprintDetailsView` implements `IBlueprintDetailsView` and only renders the supplied model into cloned UXML. It does not inspect definitions or calculate gameplay values.

Selection is not duplicated or persisted independently. Selecting a different card refreshes the projection. Moving, activating, benching, swapping, or reordering the selected Blueprint preserves its stable selection and refreshes location/neighbors. Clearing selection returns to the empty state. If the selected Blueprint is no longer owned, the Board presenter clears selection and the Details Panel becomes empty. A new Play Mode session starts unselected because current persistence intentionally stores board state, not presenter selection.

## View-model contract

`BlueprintDetailsViewModel` contains empty/error state, empty message, icon, display name, stable definition ID, developer-ID visibility, actionable diagnostic text, and ordered sections.

Each `BlueprintDetailsSectionViewModel` contains a heading, ordinary labeled values, and statistics. `BlueprintDetailsValueViewModel` supplies label, value, and tooltip. `BlueprintStatViewModel` supplies:

- label;
- authored base value;
- calculated current value;
- display unit;
- optional `BlueprintStatModifierViewModel` source/value rows;
- tooltip text suitable for a future full calculation explanation.

Milestone 1 intentionally sets current equal to base and supplies no modifier rows. Future refinement, Ascension, evolution, research, adjacency, philosophy, artifact, or Nexus services may provide current values and breakdowns to the resolver without changing the View or adding calculation code to UI.

## Displayed information

Identity includes icon/fallback, display name, developer-only stable ID, race or Neutral, content type, tier, role, description, and Active/Benched state.

Production includes spawn interval, spawn batch size, maximum population, initial spawn delay, and production priority for units. Structures show only applicable population/reconstruction/placement data and explicitly label inapplicable production priority.

Combat includes unit health, damage, interval, range, movement speed, armor, resistance, targeting profile, movement profile, and lane compatibility. Structures show authored health/armor/resistance, stationary movement, lane compatibility, attack availability, and authored support/adjacency summary.

Board assignment includes Active Board or Bench, zero-based active slot index or Bench, assigned lane, assigned stance, left neighbor, right neighbor, and a combined adjacency relationship summary.

Progression preview reads current stored runtime values only: copies owned, Ascension level, selected refinements, occupied research entries, attached research, and selected evolution. Copy thresholds 1, 4, 5, 9, and 10 are displayed as locked until progression exists. Missing selections are labeled `Not yet acquired`; no bonuses or unlock behavior are fabricated. In Milestone 1 the displayed socket count is the count of occupied `AttachedResearchIds`, not an implemented unlocked-socket calculation.

Shop information is authored planning data only: Gold cost, pool kind, base pool size, and shop tier. Live pool count is explicitly unavailable because Shops are outside Milestone 1.

## Diagnostics and asset loading

Catalog lookup uses the selected immutable ID and includes disabled definitions for save compatibility. A missing or unsupported definition produces a visible diagnostic containing the stable ID and resolution error instead of throwing or leaving stale content.

Runtime attachment requires serialized references to:

- `BlueprintBoardPanel.uxml`;
- `BlueprintBoardPanel.uss`;
- `BlueprintDetailsPanel.uxml`;
- `BlueprintDetailsPanel.uss`;
- the `PanelSettings` asset on the `UIDocument`;
- `GameContentCatalog.asset` on the sandbox composition root.

The production and Player-test scenes serialize these dependencies. Runtime code does not use `AssetDatabase`, `UnityEditor`, project-relative `Assets/...` loading, `Resources`, reflection discovery, or editor menu commands. The test-only `BlueprintBoardPlayerTest.unity` scene is temporarily included by Unity Test Framework prebuild hooks and removed from Build Settings after the build, so it does not become a shipping startup scene.

## Verification

EditMode resolver/presenter tests cover unit and structure projections, active/bench location, immutable-ID lookup, production/combat data, neighbors, defaults and milestone labels, Neutral/race identity, missing-ID diagnostics, selection clearing, movement preservation, and removal.

Editor and standalone Player PlayMode tests select and clear cards, change selection, update location after movement, update neighbors after swapping, exercise wide/narrow layout, and verify repeated `UIDocument` disable/enable cycles do not duplicate callbacks or panels.

Manual sandbox verification should open `Assets/_Game/Scenes/BlueprintBoardSandbox.unity`, enter Play Mode, select active and benched cards, inspect every section and scrolling, move the selected card between Board and Bench, swap it, clear selection, resize the Game view, and exit/re-enter Play Mode to confirm the session starts unselected while board persistence remains intact.

## Current limitations

No current-stat modifier provider exists yet, so current values equal authored base values. Copy acquisition, socket unlocking, refinements, research effects, Ascensions, evolutions, live shop pools, combat evaluation, economy, and other later systems remain deliberately unimplemented.
