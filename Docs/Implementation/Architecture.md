# Milestone 0 Architecture

## Assembly boundaries

| Assembly | Responsibility | References |
| --- | --- | --- |
| `BlueprintCivilizations.Core` | Plain C# primitives and the minimal ID-based blueprint state contract | None; Unity engine references are disabled |
| `BlueprintCivilizations.Content` | ScriptableObject definitions, catalog, structured authoring data, and validation | Core |
| `BlueprintCivilizations.Editor` | Content Studio, sample generation, catalog rebuilding, and build validation | Core, Content; Editor only |
| `BlueprintCivilizations.Tests` | EditMode behavior and asset tests | Core, Content, Editor; Editor only |

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

The minimal `UnitBlueprintState` is a plain C# separation contract required by the approved Milestone 0 scope. It stores definition/owner IDs and player selections, but never a `UnitDefinition` reference or authored statistics. Board commands, placement rules, adjacency, derived-stat calculation, and progression services remain Milestone 1 or later.

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

Milestone 0 does not contain combat, runtime match flow, economy execution, shop generation, multiplayer, matchmaking, runtime UI, Blueprint Board services, or derived-stat calculation. The complete Volume 16 roster has not been converted; the Hive set is a foundation sample only.
