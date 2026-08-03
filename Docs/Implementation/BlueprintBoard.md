# Blueprint Board

## Purpose and invariants

The Blueprint Board is the player's production-planning state and UI. It is not a battlefield. The active board is exactly one horizontal ordered line: never a matrix, hex layout, or multiple rows. Each active Blueprint occupies one slot, and serialized slot count must equal Blueprint Capacity.

Benched Blueprints do not consume active capacity or participate in adjacency. Their serialized order is deterministic for presentation and save stability but has no gameplay meaning. Bench capacity is unlimited in Milestone 1 because the Design Bible assigns a finite bench limit to the excluded Economy subsystem.

An empty active slot breaks immediate left/right adjacency. “Blueprints left of” and “Blueprints right of” still return every occupied slot on the requested side in ascending board-index order.

## Runtime state

- `BlueprintBoardState` is the aggregate root. It owns the player ID, save version, capacity, fixed slot list, bench, registered Blueprint states, and state revision.
- `BlueprintSlotState` stores its zero-based board index and an optional stable Blueprint definition ID.
- `BlueprintBenchState` stores stable definition IDs. It exposes no gameplay-capacity rule.
- `BlueprintState` stores player-owned mutable data and stable IDs only. `UnitBlueprintState` is a semantic subtype; structures can use the shared base state.

Definition assets are never stored in serialized board state and are never mutated. Catalog access is isolated behind `IBlueprintDefinitionResolver` and `IBlueprintBoardPresentationResolver`.

## Commands and movement

`BlueprintPlacementService` is the only authoritative mutation surface:

- `ActivateBlueprint` moves an owned benched Blueprint into an empty slot. If an occupied insertion position is requested while capacity remains, neighboring entries shift deterministically toward the nearest available slot, preferring the right side.
- `BenchBlueprint` removes an active Blueprint and appends it to the bench.
- `MoveBlueprint` moves an active Blueprint to an empty active slot without shifting other slots.
- `SwapBlueprints` atomically exchanges two occupied active slots.
- `ReorderBlueprints` moves one active Blueprint to another board index and shifts intervening slots while preserving their relative order.
- `SetBlueprintCapacityCommand` expands the slot list or safely shrinks only when all removed slots are empty and the active count fits.

Commands optionally carry player ID, sequence number, and expected revision for future authoritative-host integration. Expected failures return a structured `BlueprintCommandResult` and leave serialized state byte-for-byte unchanged. Programmer errors such as constructing a service with null state may still throw.

## Events and undo/redo

Every successful mutation increments the board revision and emits one `BlueprintEvent` describing the operation, affected IDs, indices, and resulting revision. `BlueprintPlacementService` stores pre-command JSON snapshots for runtime Undo/Redo. Restore actions also advance revision monotonically and emit `UndoCompleted` or `RedoCompleted`.

The UI exposes buttons and Ctrl+Z/Ctrl+Y shortcuts. No runtime assembly references `UnityEditor` or the editor Undo API.

## Adjacency

`BlueprintAdjacencyService` supports:

- immediate left neighbor;
- immediate right neighbor;
- the adjacent pair;
- all occupied Blueprints left of a source;
- all occupied Blueprints right of a source;
- active matches by authored tags;
- active matches by race;
- active matches by tier.

Tag, race, and tier queries use immutable metadata from an injected resolver. Disabled definitions may resolve for save compatibility. The service calculates relationships only; modifier resolution and gameplay bonuses are outside Milestone 1.

## Validation

`BlueprintValidationService` returns structured issues with code, severity, Blueprint ID, board index, and message. It detects unsupported save versions, invalid capacity, slot/capacity mismatch, null slots or Blueprints, invalid indices, active overflow, duplicate active or bench placement, active-and-bench duplication, duplicate ownership state, owner mismatch, unresolved definitions, unregistered placement IDs, and state/slot placement mismatch.

`CapacityOverflow` counts non-empty physical active slots and means that occupied slot count is greater than configured capacity. `CapacitySlotMismatch` independently means serialized slot-list length differs from capacity. Because each slot stores at most one Blueprint ID, overflow cannot occur while slot-list length correctly equals capacity; an overflow is therefore only representable in corrupted storage and necessarily coexists with a slot-list mismatch. Valid capacity reduction is atomic: placement rejects a reduction until every removed slot is empty, then shrinks capacity and slot storage together.

Placement commands validate the complete aggregate before mutation and again afterward. A postcondition failure rolls back to the original snapshot.

## Serialization and persistence

`BlueprintBoardSerializer` uses Unity `JsonUtility` over explicit serialized fields and stable IDs. `BlueprintBoardPersistenceService` accepts an injected `IBlueprintBoardStorage`. `PlayerPrefsBlueprintBoardStorage` is the provided restart-safe local adapter and flushes writes explicitly. `BindAutoSave` persists the initial state and every successful command, Undo, or Redo until its disposable binding is released. A future versioned match-save system can replace the storage adapter without changing board rules.

## Runtime UI

`BlueprintBoardPanel.uxml` defines the responsive planning layout, header, capacity indicator, active horizontal scroll line, bench, history controls, status feedback, and the host for the modular Details Panel. `BlueprintBoardPanel.uss` owns board colors, spacing, sizing, selection, adjacency, insertion, swap, focus, error treatments, and wide/narrow flex behavior. Horizontal scrolling preserves a single active row at narrow sizes; the Details Panel moves below the board rather than changing board topology.

`BlueprintBoardView` renders view models and emits interaction intents. It provides visible empty slots, independent hover/selected/adjacent states, tooltips, click selection, pointer drag/drop, a drag ghost, valid/invalid insertion and swap previews, focusable cards/slots, arrow-key focus movement, Shift+Arrow reordering, Delete/Backspace benching, and Undo/Redo shortcuts. `BlueprintBoardPresenter` owns the stable selected Blueprint ID, translates intents into commands, computes selected adjacency through `BlueprintAdjacencyService`, and refreshes from placement events. The view never computes placement legality or adjacency.

Use `BlueprintBoardPanelFactory.Attach` to clone the Board and Details UXML into a runtime `UIDocument` host, apply both USS assets, bind a state/catalog, and receive one disposable `BlueprintBoardPanelController`. Its Board presenter owns selection and interactions; its Details presenter observes that same selection and refreshes after successful placement events. See `BlueprintDetailsPanel.md` for the projection contract.

### Input and selection model

Milestone 1 has at most one owned instance per Blueprint definition, so the immutable definition ID is also the stable runtime Blueprint instance identifier used by selection and drag payloads. No `VisualElement` reference is authoritative state.

- Pointer enter/leave updates only `blueprint-card--hovered`; hover never selects and never applies adjacency styling.
- Pointer release without crossing the drag threshold requests selection. Clicking a different card transfers selection. Clicking the currently selected card keeps it selected.
- Clicking an empty active slot or empty Bench surface clears selection.
- Selection is owned by `BlueprintBoardPresenter`, survives view rebuilds and successful movement while the Blueprint remains owned, and clears if that Blueprint no longer exists.
- Immediate adjacency is derived from the selected active Blueprint. It is empty for no selection or a benched selection and is rendered with `blueprint-card--adjacent`, visually distinct from hover and selection.

The independent state classes are:

- `blueprint-card--hovered` for the card directly under the pointer;
- `blueprint-card--selected` for the single presenter-owned selection;
- `blueprint-card--adjacent` for immediate left/right neighbors of the selection;
- `blueprint-card--dragging` for the captured source;
- `blueprint-drop-target--valid` and `blueprint-drop-target--invalid` for command-preview feedback;
- `blueprint-insertion--preview` and `blueprint-slot--swap-preview` for the valid operation shape.

Pure hover updates do not rebuild the board. Selection class updates also avoid a rebuild. Placement events rebuild the card/slot projection, after which presenter-owned selection and adjacency are reapplied by stable ID.

### Drag, capture, and drop mapping

`BlueprintBoardInteractionState` tracks one pointer, stable Blueprint ID, origin, source index, start position, and whether movement has crossed the configurable 6-pixel default threshold. Pointer down captures on the source card. Move, up, and cancel are handled on that captured card, with root trickle-down callbacks as a secondary guard so `ScrollView` gesture handling cannot consume the interaction. Crossing the threshold applies the source style and creates a picking-ignored drag ghost.

Every geometric target is converted to a `BlueprintBoardDropRequest`. The presenter maps it as follows:

- Bench to empty Active slot: `ActivateBlueprint`.
- Bench to occupied Active slot or insertion target: `ActivateBlueprint`; the placement service applies its documented deterministic insertion shift toward the nearest empty slot, preferring right on a tie.
- Active to Bench: `BenchBlueprint`.
- Active to empty Active slot: `MoveBlueprint`.
- Active to occupied Active slot: `SwapBlueprints`.
- Active to insertion target: `ReorderBlueprints`.

For hover feedback, `BlueprintPlacementService.Preview` executes the exact command against an isolated serialized clone. It cannot change authoritative state, revision, history, events, or persistence. A failed preview applies invalid styling and the structured command reason. A drop dispatches the command through the presenter; failures leave state unchanged and remain visible in status feedback.

Pointer capture is released on click, successful or failed drop, pointer cancel, capture loss, view refresh, panel detachment, and disposal. Rendering cancels any in-flight interaction before removing card elements. A completed drag never falls through to click selection. `BlueprintBoardPresenter.Dispose` unsubscribes both view intents and `BlueprintPlacementService.EventRaised`, then disposes the view callbacks, preventing repeated `UIDocument` enable/disable cycles from accumulating handlers.

Optional interaction diagnostics are supplied by `BlueprintBoardPanelFactory.Attach(enableInteractionDiagnostics: true)` or the sandbox bootstrap's serialized developer flag. The default is false. When enabled, logs include pointer down, threshold crossing, capture/cancel/release, source ID, target kind/index, command dispatch, and structured result.

## Development sandbox

Run `Tools > Blueprint Civilizations > Create Blueprint Board Sandbox Scene` to create or repair the development sandbox. Its authoritative assets are:

- scene: `Assets/_Game/Scenes/BlueprintBoardSandbox.unity`;
- Panel Settings: `Assets/_Game/UI/Development/BlueprintBoardSandboxPanelSettings.asset`;
- structure: `Assets/_Game/UI/UXML/BlueprintBoardPanel.uxml`;
- style: `Assets/_Game/UI/Styles/BlueprintBoardPanel.uss`;
- details structure: `Assets/_Game/UI/UXML/BlueprintDetailsPanel.uxml`;
- details style: `Assets/_Game/UI/Styles/BlueprintDetailsPanel.uss`;
- content catalog: `Assets/_Game/Content/Assets/Configuration/GameContentCatalog.asset`.

The Panel Settings references Unity's standard generated runtime theme at `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss`. This shared UI Toolkit dependency is authored by Unity's normal Panel Settings workflow and contains only `@import url("unity-theme://default")`; it does not contain Blueprint rules or presentation overrides.

Before opening or mutating the scene, the command loads both UXML assets, both USS assets, the catalog, and all four prototype definitions (`HIVE_LARVA`, `HIVE_SPIDER`, `HIVE_BEETLE`, and `HIVE_STR_01`). Missing required source assets abort with an error containing the asset type, exact path, attempted action, and suggested manual repair.

The Panel Settings asset is generated only after those prerequisites pass. Creation uses `ScriptableObject.CreateInstance<PanelSettings>` and `AssetDatabase.CreateAsset`, then saves, synchronously imports, refreshes, and reloads the persistent main object from its documented path. The transient instance passed to `CreateAsset` is never returned because Unity may invalidate it during import. An existing valid asset is reused; an existing wrong-type or unloadable asset produces an actionable error instead of creating a duplicate.

Scene composition is idempotent: it repairs to exactly one `PrototypeBootstrap`, one `UIDocument`, and one `BlueprintBoardSandboxBootstrap`, assigns the authoritative Panel Settings/catalog/UXML/USS references, and removes duplicate sandbox roots or components. The completed in-memory composition is validated before `SaveScene`; failures restore the prior Editor scene setup and are not saved as successful sandbox output.

The runtime sandbox starts with capacity 4, Larva and Spider active, and Beetle plus Creep Tumor benched. It resolves all four definitions through the default content catalog, attaches the normal UXML/USS through `BlueprintBoardPanelFactory`, and binds successful operations to the PlayerPrefs persistence adapter. The bootstrap is a development composition root only; it contains no placement or adjacency rules.

### Unity verification procedure

1. Run all EditMode tests. The Details Panel baseline contains 89 tests, including resolver, presentation, placement, validation, serialization, content, and sandbox-generation coverage.
2. Run all Editor PlayMode tests. The suite contains 17 UIDocument tests covering hover, selection transfer/clear, Details population and refresh, all drag mappings, responsive layout, preview styling, invalid drops, capture release, persistence, and repeated enable/disable.
3. Run the complete PlayMode assembly in a Windows Standalone Player twice. Both runs must discover 17 tests with no failure, skip, or inconclusive result.
4. Run the sandbox creation command twice. Both runs must retain one Panel Settings asset and one scene composition.
5. Open `BlueprintBoardSandbox.unity`, enter Play Mode, and verify selection, Details content, bench-to-board activation, active-to-bench movement, empty-slot movement, swapping, insertion/reordering, adjacency highlighting, keyboard navigation, Shift+Arrow, Delete-to-bench, and Undo/Redo.
6. Exit and re-enter Play Mode, then restart Unity, to verify the PlayerPrefs-backed sandbox state survives reload. Selection intentionally starts empty because it is presenter state, not persisted board state.

### Standalone Player test composition

`Assets/_Game/UI/Tests/PlayMode/BlueprintBoardPlayerTest.unity` is a checked-in test-only scene with serialized references to the catalog, both UXML assets, both USS assets, and the Panel Settings asset. `BlueprintBoardPlayModeTests` implements the Unity Test Framework prebuild/cleanup hooks: the scene is temporarily appended to `EditorBuildSettings` for the Player build and removed immediately after the build. It is never the shipping startup scene, and `GamePrototype0` is unchanged.

Player tests load `BlueprintBoardPlayerTest` by scene name through `SceneManager`. They do not use `AssetDatabase`, `UnityEditor`, `Assets/...` runtime paths, reflection, or filesystem discovery. A dedicated `tests.blueprint-board.player-runtime.v1` PlayerPrefs key isolates test persistence and is deleted during setup and teardown. The production sandbox continues to use its separate runtime storage key.

For Milestone 1 visual testing, open `Assets/_Game/Scenes/BlueprintBoardSandbox.unity`; do not use `GamePrototype0`. `GamePrototype0` intentionally contains only its camera and light as the empty future game scene.

### Manual sandbox usability check

1. Run `Tools > Blueprint Civilizations > Create Blueprint Board Sandbox Scene`.
2. Open `Assets/_Game/Scenes/BlueprintBoardSandbox.unity`.
3. Enter Play Mode and confirm active Blueprint cards, Bench cards, and two empty active slots are visible.
4. Hover Larva Brood, then Web Spider. Confirm only the pointer target has the yellow hover treatment.
5. Click Larva Brood, then Web Spider. Confirm selection transfers, the selected background is unique, and purple adjacency follows the selection. Click an empty slot and confirm selection/adjacency clear.
6. Drag Shell Beetle from the Bench to an empty active slot. Confirm the source, ghost, and valid target feedback appear and the Blueprint activates.
7. Drag one active Blueprint to the Bench and confirm it becomes benched.
8. Drag one active Blueprint onto another and confirm the documented swap occurs.
9. Drag an active Blueprint to an empty active slot, then use an insertion target to reorder. Confirm movement/insertion previews and final ordering.
10. Drag to the title or status area. Confirm invalid feedback, no state change, and that the next click/drag still works (capture was released).
11. Repeat selection and drag operations after several state refreshes. Confirm no duplicate response or progressively worsening behavior.
12. Exit and re-enter Play Mode. Confirm the PlayerPrefs-backed board restores. Restart Unity and repeat the restore check.

The sandbox-generation command may be run repeatedly. Each run repairs the same scene to one `PrototypeBootstrap`, one `UIDocument`, and one sandbox bootstrap component without creating duplicate Panel Settings or overwriting unrelated assets.

The EditMode regression recorded in `Errors/TestResults_20260802_163145.xml` was not a board-rule failure. `BlueprintBoardSandboxSceneComposition_WhenRunTwice_DoesNotDuplicateBootstrapOrDocument` called runtime `SceneManager.CreateScene`, which Unity 6 permits only in Play Mode. Editor tests now create temporary scenes through `EditorSceneManager.NewScene`; the production composition API remains unchanged by that lifecycle correction.

## Extensibility boundaries

- Economy may inject finite bench-capacity policy later; it must not move economy logic into the view.
- Match flow may enable/disable commands by phase outside this subsystem.
- Progression may extend `BlueprintState` through composed serializable records without mutating definitions.
- Modifier systems consume adjacency query results; they do not belong in adjacency calculation.
- Multiplayer may serialize commands and use expected revision/sequence fields after the local deterministic loop exists.
- Save migration must branch on `SaveVersion` before accepting older state shapes.
