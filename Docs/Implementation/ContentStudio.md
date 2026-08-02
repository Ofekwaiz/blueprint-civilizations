# Content Studio

Open `Tools > Blueprint Civilizations > Content Studio`.

The tool uses UXML/USS and supports type filtering, search by identity/tag, direct asset selection, serialized editing, safe creation, duplication with a fresh stable ID, disabling with Undo, permanent deletion with warning, catalog rebuilding, and actionable validation display.

## Designer workflow
1. Create or select a unit.
2. Content Studio assigns its immutable ID. The ordinary inspector does not expose the ID.
3. Assign race, tier, Gold cost, combat stats, spawn interval, maximum population, targeting, ability text, evolutions, and visuals.
4. Author executable effects through ability, modifier, and trigger definitions; description fields are presentation text.
5. Rebuild the default catalog and run Validate All Content.
6. Resolve every Error and Critical issue before building.
7. Prefer Disable over permanent deletion.

## Extending the tool
Add a new ScriptableObject derived from `ContentDefinition`, add its type to the Content Studio type menu, and add specialized validation to `ContentValidator`. Keep serialized fields private, expose read-only runtime APIs, and never add player-specific state to a definition asset.
