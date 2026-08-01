# Milestone 0 Architecture

## Definition versus runtime state
`ContentDefinition` assets contain authored, reusable content. They must never be mutated during a match. `UnitBlueprintState` stores one player's copies, upgrades, evolutions, research attachments, placement, lane, and stance.

## Stable IDs
All cross-content references and save data must use immutable IDs such as `unit.hive.spider`. Display names may change freely.

## Assemblies
- Core: runtime primitives and state.
- Content: definitions, catalog, validation.
- Editor: Content Studio and authoring tools.
- Content.Tests: EditMode tests.

## Next architectural modules
Combat simulation, economy, shops, blueprint-board rules, runtime UI, and networking should be added as separate assemblies after Milestone 0 is validated.
