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

Milestone 0 provides visual authoring, not runtime gameplay. It does not preview derived combat statistics, execute modifiers/triggers, or implement drag-and-drop Blueprint Board behavior.
