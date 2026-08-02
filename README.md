# Blueprint Civilizations — Unity Milestone 0 Foundation

This repository is the data-driven foundation for the game described in `Docs/DesignBible`.

## What is included
- Unity 6-compatible source layout.
- ScriptableObject definitions for races, Nexus data, units, structures, research, artifacts, evolutions, abilities, philosophies, augments, and balance configuration.
- Read-only runtime access to authored definition data.
- Structured modifier, trigger, compatibility, targeting, movement, and production authoring data.
- Stable-ID content catalog plus actionable validation and pre-build validation.
- UXML/USS-based UI Toolkit Content Studio editor window.
- Canonical Hive vertical-slice sample content and a repair/regeneration menu.
- EditMode foundation and real-asset validation tests.
- Embedded Development Bible and implementation documentation.

## Important limitation
Milestone 0 establishes authoring and validation contracts only. It does not implement blueprint runtime state, economy, shops, combat, match flow, runtime UI, saves, or networking. The complete Volume 16 prototype roster has not yet been converted to assets; the checked-in Hive set is the approved vertical-slice sample.

## First run
1. Install Unity Hub and a Unity 6 editor with Windows Build Support.
2. Add this folder as a project in Unity Hub.
3. Open the project and resolve any package-version prompt.
4. Open `Tools > Blueprint Civilizations > Content Studio`.
5. Run `Tools > Blueprint Civilizations > Rebuild Default Content Catalog` after adding content.
6. Run `Tools > Blueprint Civilizations > Validate All Content`.
7. Open Test Runner and run EditMode tests.

`Create or Repair Prototype Sample Content` is available when the canonical sample assets need to be regenerated.

## Recommended next milestone
Proceed to Milestone 1: runtime Blueprint State, active linear board, bench, Blueprint Capacity, movement commands, adjacency, and tests. Do not start economy, combat, runtime UI, or networking early.
