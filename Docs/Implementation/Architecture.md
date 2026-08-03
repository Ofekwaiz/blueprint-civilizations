# Project Architecture

## Assembly boundaries

| Assembly | Responsibility | References |
| --- | --- | --- |
| `BlueprintCivilizations.Core` | Plain C# primitives and broadly reusable authored-data enums | None; Unity engine references are disabled |
| `BlueprintCivilizations.Content` | ScriptableObject definitions, catalog, structured authoring data, and validation | Core |
| `BlueprintCivilizations.Blueprints` | ID-based Blueprint ownership, linear board/bench state, placement commands, validation, adjacency, serialization, and persistence adapters | Core, Content |
| `BlueprintCivilizations.UI` | UI Toolkit views, view models, presenters, and runtime composition for the Blueprint Board | Core, Content, Blueprints |
| `BlueprintCivilizations.Editor` | Content Studio, sample generation, catalog rebuilding, and build validation | Core, Content; Editor only |
| `BlueprintCivilizations.Tests` | EditMode content behavior and asset tests | Core, Content, Blueprints, Editor; Editor only |
| `BlueprintCivilizations.Blueprints.Tests` | EditMode board rules, serialization, validation, undo/redo, and UI asset tests | Core, Content, Blueprints, UI; Editor only |

Runtime assemblies contain no `UnityEditor` dependency. Dependency direction is one-way, and the test assembly is not auto-referenced by production code.

## Definitions and runtime state

`ContentDefinition` contains reusable authored data: immutable ID, display name, description, data version, enabled state, tags, and icon. Serialized fields are private, runtime properties are read-only, and collections expose read-only wrappers. Definitions must never be modified by gameplay.

`UnitDefinition` composes focused value objects:

- `UnitCombatStats` for HP, damage, attack interval, range, movement, armor, and resistance.
- `UnitProductionStats` for spawn interval, initial delay, batch size, population cap, and priority.
- `UnitTargetingProfile` for deterministic target preferences.
- `PerCopyStatUpgradeOption` for legal refinement choices.
- `SocketMilestoneConfiguration` for copy thresholds 1, 4, and 9.
- `UnitPresentationReferences` for optional prefab, animator, audio, and VFX assets.

`BlueprintState` and its semantic `UnitBlueprintState` subtype are plain serializable runtime objects owned by the Blueprints subsystem. They store definition/owner IDs, player selections, and synchronized placement metadata, but never a `ContentDefinition` reference or authored statistics. `BlueprintBoardState` is the aggregate root for the fixed-capacity active slot line, unlimited Milestone 1 bench, owned state registry, save version, and monotonically increasing revision.

All board mutation passes through `BlueprintPlacementService`. Commands are prevalidated, applied transactionally, postvalidated, revisioned, evented, and recorded as JSON snapshots for runtime undo/redo. Expected player-action failures return `BlueprintCommandResult`; they do not throw and do not partially mutate state. The UI issues commands through a presenter and never writes board lists.

The runtime planning panel is composed by `BlueprintBoardPanelFactory` and owned by `BlueprintBoardPanelController`. The controller disposes the Board and Details presenters as one unit. `BlueprintBoardPresenter` remains the only owner of the stable selected Blueprint ID. `BlueprintDetailsPresenter` observes selection and placement events, while `ContentCatalogBlueprintDetailsResolver` joins ID-based runtime state with immutable catalog definitions into a read-only `BlueprintDetailsViewModel`. `BlueprintDetailsView` only renders that model; it does not inspect ScriptableObjects, calculate statistics, or mutate board state.

Details statistics explicitly carry authored base value, calculated current value, display unit, tooltip text, and an optional modifier breakdown. Milestone 1 supplies current equal to base and an empty breakdown. Future progression, adjacency, philosophy, artifact, Nexus, or other modifier services may populate this projection without moving their rules into UI code.

`BlueprintAdjacencyService` operates only over active slot order. Authored tag/race/tier metadata is read through an injected resolver. It calculates relationships but does not apply modifiers or gameplay bonuses. See `BlueprintBoard.md` for the complete subsystem contract.

## Stable IDs

Bible-exported seed IDs such as `HIVE_SPIDER` are preserved exactly. IDs are hidden from the ordinary inspector and assigned through controlled Content Studio creation/duplication. Runtime catalog lookup uses IDs only; display names may change without affecting identity. Duplicate IDs are blocking validation errors. Disabling content is preferred to deletion so saves can still resolve historical IDs.

## Structured authored rules

Reusable `ModifierSpec`, `TriggerSpec`, compatibility, ability, movement, targeting, and production data describe authored behavior. Free-form descriptions are presentation text and are not executable behavior. Later services will evaluate these records deterministically; Milestone 0 does not implement combat or progression resolution.

## Canonical Hive sample and placeholders

The checked-in sample maps the requested shorthand to the canonical Bible definitions:

- Worker Larva → `HIVE_LARVA`, display name “Larva Brood”.
- Spider → `HIVE_SPIDER`, display name “Web Spider”.
- Armored Beetle → `HIVE_BEETLE`, display name “Shell Beetle”.

Tier, Gold cost, HP, damage, attacks-per-second source value, population, roles, abilities, IDs, and Tier 1 pool size come from the Design Bible/JSON export. Serialized attack interval is calculated as `1 / attacksPerSecond`.

The following are prototype placeholders because the current Bible does not specify exact per-unit values: spawn interval `6s` (within the Volume 13 Tier 1 target), initial delay `0s`, attack range (`1` melee, `4` ranged), movement speed `2`, base armor/resistance `0`, default targeting/lane settings, shop weight `1`, and empty presentation references. They remain editable data and must be replaced by approved balance/presentation values rather than treated as final rules.

## Adding a definition type

1. Add one ScriptableObject class derived from `ContentDefinition` under `Assets/_Game/Content/Runtime/Definitions`.
2. Keep authored fields private and expose read-only runtime properties.
3. Add the type to the Content Studio type menu and organized-folder mapping.
4. Add specialized checks to `ContentValidator`.
5. Add catalog/validation tests and a representative asset if the milestone needs one.
6. Rebuild the default catalog and resolve every Error/Critical issue.

## Current limitations

Milestone 1 does not contain combat, runtime match flow, economy execution, finite bench economy rules, shop generation, multiplayer, matchmaking, Blueprint progression, or derived-stat calculation. The complete Volume 16 roster has not been converted; the Hive set remains a foundation sample only.

The Details Panel therefore reports authored production/combat/shop-planning data and current stored Blueprint runtime fields only. It does not acquire copies, unlock milestones, calculate research/evolution/refinement effects, or expose live shop pool counts. See `BlueprintDetailsPanel.md` for its presentation and Player-build contract.
