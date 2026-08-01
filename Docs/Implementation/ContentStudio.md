# Content Studio

Open `Tools > Blueprint Civilizations > Content Studio`.

The initial tool supports searching and selecting content, editing serialized fields, creating assets, duplicating assets, disabling content, permanent deletion with warning, and validation display.

## Designer workflow
1. Create or select a unit.
2. Set its immutable ID once.
3. Assign race, tier, Gold cost, combat stats, spawn interval, maximum population, targeting, ability text, evolutions, and visuals.
4. Run Validate All Content.
5. Prefer Disable over permanent deletion.

## Extending the tool
Add a new ScriptableObject derived from `ContentDefinition`, then add its type to the Content Studio toolbar. Put specialized validation in `ContentValidator` and keep runtime mutation out of definition assets.
