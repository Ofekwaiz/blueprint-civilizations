# Content Studio

Open `Tools > Blueprint Civilizations > Content Studio`.

The UI Toolkit window loads its structure and styling from UXML/USS. The left pane supports text search plus content-type, race, tier, and enabled-state filters. Search matches display name, stable ID, and tags. Single selection opens the asset and makes it the active Unity selection; activating a row also pings it in the Project window.

## Creating a unit

1. Select **Units** in the type menu and click **Create**.
2. Content Studio creates the asset under `Assets/_Game/Content/Assets/Units/Custom` and assigns a new immutable stable ID.
3. Fill the grouped **Identity**, **Economy and Shop**, **Production**, **Combat**, **Blueprint Progression**, and **Presentation** sections.
4. Add legal per-copy refinement options, socket milestones, and evolution references.
5. Review the selected asset’s validation panel at the bottom.
6. Run **Rebuild Catalog**, then **Validate All**.
7. Resolve every Error and Critical issue before building.

The ID is displayed read-only and is deliberately absent from the serialized editor. Display-name changes do not change the ID.

## UnitDefinition editor binding

The grouped unit editor validates its complete serialized-property schema before constructing the detail view. It creates every `PropertyField` first, then binds the completed visual tree to the selected unit's fresh `SerializedObject`. Previous bindings are explicitly removed before switching assets or rebuilding after Undo/Redo.

The six sections expose all currently authored unit data:

- **Identity:** display name, description, data version, enabled state, tags, icon, race, neutral flag, tier, and role. The immutable stable ID remains a separate read-only label.
- **Economy and Shop:** Gold cost, pool kind, shop pool size, and base shop weight.
- **Production:** spawn interval, initial spawn delay, spawn batch size, maximum population, and spawn priority.
- **Combat:** maximum HP, attack damage, attack interval, attack range, movement speed, armor, resistance, targeting priority and target compatibility, lane compatibility, movement profile, and abilities.
- **Blueprint Progression:** permitted per-copy stat upgrades, all three socket-copy milestones, both Ascension thresholds, and both evolution-reference lists.
- **Presentation:** visual prefab, animator controller, spawn/attack/death audio, and spawn/death VFX. The icon is shown in Identity.

If an expected path no longer exists, the detail pane displays `Unable to display UnitDefinition fields. See Console for missing serialized property paths.` Each missing path produces one actionable Editor error containing the selected asset type, asset path, and expected property path; repeated repaints do not continuously log the same issue.

## Safe content operations

- **Duplicate** copies the selected asset and immediately assigns a fresh stable ID.
- **Disable (Recommended)** preserves the asset and ID for save compatibility. Disabled content is excluded from normal runtime catalog queries.
- **Enable** restores a disabled definition.
- **Delete Permanently** requires explicit confirmation and warns about saved references.

Creation, serialized editing, duplication, and enable/disable operations use Unity serialization, dirty-state handling, and Undo where Unity supports the operation. Permanent asset deletion is intentionally not presented as reversible.

Filters and selection refresh after creation, duplication, property changes, enable/disable, deletion, and Undo/Redo. A deleted or missing selected asset clears the editor and shows a diagnostic instead of throwing.

## Other content types

Race, Nexus, structure, research, artifact, evolution, ability, philosophy, augment, and configuration assets use the generic serialized editor. New assets are routed into their architecture-defined content category with a `Custom` subfolder where appropriate.

## Sample generation

Run `Tools > Blueprint Civilizations > Create or Repair Prototype Sample Content` to repair the canonical Hive seed set. The generator looks up each asset by stable ID before creating it, writes to organized category folders, and rebuilds the deterministic catalog. Repeated runs update the same assets rather than creating duplicate IDs.

## Limitations

Milestone 0 provides visual authoring, not runtime gameplay. Content Studio does not preview derived combat statistics or execute modifiers/triggers. Blueprint Board runtime interaction belongs to the separate Milestone 1 sandbox.

## Manual UnitDefinition verification

1. Open `Tools > Blueprint Civilizations > Content Studio`.
2. Select **Web Spider** (`HIVE_SPIDER`) in Units.
3. Expand Identity, Economy and Shop, Production, Combat, Blueprint Progression, and Presentation; confirm every section contains editable fields.
4. Edit maximum HP, Gold cost, spawn interval, and maximum population.
5. Use Undo and Redo and confirm the displayed values update without losing Web Spider selection.
6. Save assets, restart Unity, reopen Content Studio, select Web Spider, and confirm the values persisted.
