# AGENTS.md — Blueprint Civilizations

## Source of truth
The documents under `Docs/DesignBible/` are authoritative gameplay specifications. Do not invent, remove, or materially change gameplay rules without identifying the conflict.

## Architecture
- Unity 6, C#.
- Authored content is data-driven through ScriptableObjects.
- Definition assets are immutable at runtime.
- Player-specific progression lives in runtime state objects.
- Every definition has a stable string ID.
- Gameplay and presentation are separated.
- UI Toolkit is used for editor tools and modular runtime UI.
- Core simulation must eventually run without scene objects.
- Use explicit deterministic RNG seeds.
- Add tests for content validation and business rules.

## Content authoring
A visual editor exists at `Tools > Blueprint Civilizations > Content Studio`.
Designers should not need to edit JSON or C# to create ordinary content.
Prefer disabling content over permanent deletion.

## UI
Use UXML/USS for structure and styling. Views must not implement gameplay logic.

## Workflow
Read the relevant bible volumes, inspect current code, state assumptions, implement the smallest complete milestone, run tests, and report changes.
