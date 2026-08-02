# Milestone 0 Architecture

## Definition versus runtime state
`ContentDefinition` assets contain authored, reusable content. Serialized fields remain private, runtime APIs are read-only, and collection properties return read-only wrappers. Player-owned Blueprint state intentionally begins in Milestone 1 and must live in the Blueprints subsystem rather than `Core`.

## Stable IDs
All cross-content references and save data use immutable IDs. Bible-exported seed IDs such as `HIVE_SPIDER` are preserved exactly. IDs are hidden from the ordinary inspector, assigned through controlled Content Studio operations, and regenerated when an asset is duplicated. Display names may change freely.

## Assemblies
- Core: broadly reusable primitives with Unity engine references disabled.
- Content: immutable authored definitions, structured rule data, catalog, and validation.
- Editor: Content Studio and authoring tools.
- Content.Tests: EditMode tests.

`Core` contains only broadly reusable authored-content primitives during Milestone 0. Future Blueprint state must be introduced in its own assembly.

## Structured authored rules
Gameplay behavior is described by reusable `ModifierSpec`, `TriggerSpec`, compatibility, ability, movement, targeting, and production data. Free-form descriptions are presentation text and are not authoritative executable behavior.

## Catalog and validation
`GameContentCatalog.asset` includes enabled and disabled definitions so older IDs remain resolvable when explicitly requested. Validation produces severity, definition ID, asset path, field name, message, and suggested fix. Blocking validation issues prevent player builds.

## Next architectural modules
Combat simulation, economy, shops, blueprint-board rules, runtime UI, and networking should be added as separate assemblies after Milestone 0 is validated.
