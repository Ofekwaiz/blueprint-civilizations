# Blueprint Civilizations — Unity Milestone 0 Scaffold

This repository is the data-driven foundation for the game described in `Docs/DesignBible`.

## What is included
- Unity 6-compatible source layout.
- ScriptableObject definitions for races, units, structures, research, artifacts, and evolutions.
- Runtime `UnitBlueprintState` separated from authored definitions.
- Content catalog and validation services.
- UI Toolkit Content Studio editor window.
- Prototype-content generator menu.
- EditMode tests.
- Embedded Development Bible and implementation documentation.

## Important limitation
This package was generated outside the Unity Editor. It has not been compiled by Unity in this environment. Open it with a Unity 6 installation and allow Unity to regenerate `.meta`, Library, solution, and package-lock files. If your installed Unity 6 version requests package upgrades, accept compatible upgrades and commit the resulting changes.

## First run
1. Install Unity Hub and a Unity 6 editor with Windows Build Support.
2. Add this folder as a project in Unity Hub.
3. Open the project and resolve any package-version prompt.
4. Run `Tools > Blueprint Civilizations > Create Prototype Sample Content`.
5. Open `Tools > Blueprint Civilizations > Content Studio`.
6. Run `Tools > Blueprint Civilizations > Validate All Content`.
7. Open Test Runner and run EditMode tests.

## Recommended next milestone
Do not start networking. First verify content authoring, stable IDs, validation, Undo/Redo, and persistence. Then build the deterministic headless combat simulation.
