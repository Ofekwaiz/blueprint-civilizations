# Blueprint Civilizations

## Software Architecture Bible

**Document version:** 1.0
**Project engine:** Unity 6
**Primary language:** C#
**Status:** Authoritative implementation specification

---

# 1. Document purpose

This document defines how Blueprint Civilizations must be implemented.

The Game Design Bible defines what the game should do.

This document defines how the software should be structured so the game remains:

* maintainable,
* data-driven,
* testable,
* deterministic,
* modular,
* extensible,
* designer-friendly,
* suitable for AI-assisted development.

All developers and coding agents must follow this document unless a later approved revision explicitly replaces a rule.

When this document conflicts with undocumented implementation convenience, this document takes priority.

When this document conflicts with the Game Design Bible regarding gameplay behavior, the Game Design Bible takes priority.

---

# 2. Architectural goals

The architecture must support the following long-term goals:

1. Designers can create, remove, disable, duplicate, and rebalance content without editing source code.
2. Runtime systems do not contain hardcoded races, units, structures, research, artifacts, evolutions, or shop pools.
3. Static authored content is separate from player-specific runtime state.
4. Combat can run without visual GameObjects.
5. Combat outcomes can be reproduced from the same inputs and random seed.
6. Runtime UI can be replaced or redesigned without rewriting gameplay systems.
7. Editor tools can be expanded without changing runtime architecture.
8. New races can be added primarily through data and isolated race-specific behavior modules.
9. Multiplayer can be added after the local prototype without replacing the core simulation.
10. Automated tests can validate game rules without loading complete scenes.
11. Save data remains compatible when content display names change.
12. Content can be deprecated safely without immediately breaking saves.

---

# 3. Core architectural principles

## ARCH-001 — Data-driven content

All content that a designer may reasonably change must be represented as editable data.

Examples include:

* units,
* structures,
* races,
* research,
* artifacts,
* philosophies,
* evolutions,
* abilities,
* targeting profiles,
* shop probabilities,
* pool sizes,
* economy values,
* civilization levels,
* combat timing,
* Nexus statistics,
* ascension thresholds,
* upgrade magnitudes.

These values must not be hardcoded inside gameplay classes.

Authoring data should primarily use Unity `ScriptableObject` assets.

JSON may be used for:

* exports,
* imports,
* debugging,
* external tooling,
* snapshots,
* generated reports.

JSON must not be the only designer-authoring workflow.

---

## ARCH-002 — Definitions are immutable during gameplay

A definition asset represents authored content.

Examples:

* `UnitDefinition`
* `StructureDefinition`
* `ResearchDefinition`
* `ArtifactDefinition`
* `RaceDefinition`

Definition assets must never be modified during a match.

Player-specific changes must be stored in runtime state objects.

Example:

```text
UnitDefinition
    Base HP: 30
    Base Damage: 5
    Base Population: 4

UnitBlueprintState
    Copies: 6
    Ascension: 1
    HP upgrades: 2
    Population upgrades: 1
    Attached research: Acid Blood
```

Runtime calculations may read a definition but must not write to it.

---

## ARCH-003 — Stable immutable IDs

Every content definition must have an immutable unique string ID.

Examples:

```text
race.hive
race.humans
race.cultists

unit.hive.spider
unit.human.pikeman
unit.cultist.acolyte

structure.hive.creep_tumor
research.common.rapid_gestation
artifact.human.crown_of_valor
```

Display names are not identifiers.

Renaming a display name must not affect:

* saves,
* references,
* shop pools,
* evolutions,
* research attachments,
* replays,
* multiplayer payloads.

Every definition must contain at least:

```csharp
string Id;
string DisplayName;
string Description;
int DataVersion;
bool IsEnabled;
```

Duplicate IDs are validation errors.

Once content is released, its ID should never be changed.

Content should normally be disabled rather than deleted.

---

## ARCH-004 — Composition over inheritance

Prefer small composable data and behavior objects over deep inheritance trees.

Avoid structures such as:

```text
Unit
 └── OrganicUnit
      └── HiveUnit
           └── FlyingHiveUnit
                └── PoisonFlyingHiveUnit
```

Prefer:

```text
UnitDefinition
+ Race reference
+ Tags
+ Movement profile
+ Targeting profile
+ Ability definitions
+ Evolution definitions
+ Stat block
```

Inheritance may be used for genuinely shared framework behavior, but content variations should normally use composition.

---

## ARCH-005 — Gameplay and presentation separation

Gameplay logic must not depend on:

* GameObjects,
* animation controllers,
* particles,
* audio sources,
* UI elements,
* scene hierarchy.

Presentation systems observe game state or simulation events and display them.

The architecture should conceptually follow:

```text
Authored Definitions
        ↓
Runtime Match State
        ↓
Gameplay Services / Simulation
        ↓
Events and State Changes
        ↓
Presentation
        ↓
UI, Models, Animation, VFX, Audio
```

Destroying or replacing a visual unit should not destroy the authoritative runtime unit state unless the simulation says the unit died.

---

## ARCH-006 — Deterministic simulation

Core game calculations should produce the same result when given:

* the same definitions,
* the same match state,
* the same actions,
* the same random seed,
* the same simulation version.

Randomness must use explicit seeded random streams.

Do not use uncontrolled calls to:

```csharp
UnityEngine.Random
System.Random
```

inside authoritative systems.

Instead, provide an injected deterministic random service.

Recommended interface:

```csharp
public interface IRandomSource
{
    int NextInt(int minimumInclusive, int maximumExclusive);
    float NextFloat();
    bool Roll(float probability);
}
```

Different systems may use independent derived streams for:

* shops,
* opponent assignment,
* combat,
* augment offers,
* neutral offers,
* cosmetic presentation.

Cosmetic randomness must never affect authoritative results.

---

# 4. Project folder structure

Use the following root structure:

```text
Assets/_Game/
├── Core/
│   ├── Runtime/
│   ├── Tests/
│   └── BlueprintCivilizations.Core.asmdef
│
├── Content/
│   ├── Runtime/
│   │   ├── Definitions/
│   │   ├── Catalogs/
│   │   ├── Validation/
│   │   └── Serialization/
│   ├── Assets/
│   │   ├── Races/
│   │   ├── Units/
│   │   ├── Structures/
│   │   ├── Research/
│   │   ├── Artifacts/
│   │   ├── Evolutions/
│   │   ├── Philosophies/
│   │   ├── Abilities/
│   │   └── Configuration/
│   ├── Tests/
│   └── BlueprintCivilizations.Content.asmdef
│
├── Match/
│   ├── Runtime/
│   ├── Tests/
│   └── BlueprintCivilizations.Match.asmdef
│
├── Economy/
│   ├── Runtime/
│   ├── Tests/
│   └── BlueprintCivilizations.Economy.asmdef
│
├── Blueprints/
│   ├── Runtime/
│   ├── Tests/
│   └── BlueprintCivilizations.Blueprints.asmdef
│
├── Shops/
│   ├── Runtime/
│   ├── Tests/
│   └── BlueprintCivilizations.Shops.asmdef
│
├── Combat/
│   ├── Runtime/
│   │   ├── Simulation/
│   │   ├── Commands/
│   │   ├── Events/
│   │   ├── Targeting/
│   │   └── Results/
│   ├── Presentation/
│   ├── Tests/
│   └── BlueprintCivilizations.Combat.asmdef
│
├── UI/
│   ├── Runtime/
│   │   ├── Views/
│   │   ├── Presenters/
│   │   ├── ViewModels/
│   │   └── Navigation/
│   ├── UXML/
│   ├── Styles/
│   ├── Themes/
│   ├── Tests/
│   └── BlueprintCivilizations.UI.asmdef
│
├── Editor/
│   ├── ContentStudio/
│   ├── Validation/
│   ├── ImportExport/
│   ├── Tools/
│   └── BlueprintCivilizations.Editor.asmdef
│
├── Networking/
│   ├── Runtime/
│   ├── Tests/
│   └── BlueprintCivilizations.Networking.asmdef
│
├── Art/
├── Audio/
├── Prefabs/
├── Scenes/
└── Tests/
```

Editor code must never be referenced by runtime assemblies.

---

# 5. Assembly boundaries

Use assembly definition files.

Recommended assemblies:

```text
BlueprintCivilizations.Core
BlueprintCivilizations.Content
BlueprintCivilizations.Match
BlueprintCivilizations.Economy
BlueprintCivilizations.Blueprints
BlueprintCivilizations.Shops
BlueprintCivilizations.Combat
BlueprintCivilizations.UI
BlueprintCivilizations.Editor
BlueprintCivilizations.Networking
BlueprintCivilizations.Tests
```

Dependency direction should generally follow:

```text
Core
 ↑
Content
 ↑
Match / Economy / Blueprints / Shops / Combat
 ↑
UI / Presentation / Networking
 ↑
Editor tools
```

Avoid circular assembly dependencies.

`Core` should contain only broadly reusable primitives and infrastructure.

Do not place all systems inside `Core`.

---

# 6. Content definition architecture

## 6.1 Base content definition

All definitions should inherit from or contain a common base representation.

Recommended fields:

```csharp
public abstract class ContentDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private string description;
    [SerializeField] private int dataVersion = 1;
    [SerializeField] private bool isEnabled = true;
    [SerializeField] private List<string> tags;
    [SerializeField] private Sprite icon;
}
```

The ID should not be casually editable after asset creation.

The Content Studio should provide controlled ID creation.

---

## 6.2 Race definition

`RaceDefinition` should contain references to:

* race ID,
* display information,
* race resource definition,
* Nexus definition,
* permitted race blueprints,
* permitted race research,
* permitted race artifacts,
* race shop configuration,
* race tags,
* race-specific presentation assets,
* race-specific rule modules.

Race-specific mechanics should be implemented through modular rule components, not large race checks throughout the code.

Avoid:

```csharp
if (race == Hive) { ... }
else if (race == Humans) { ... }
else if (race == Cultists) { ... }
```

Prefer behavior modules or rule services referenced by the race definition.

---

## 6.3 Unit definition

`UnitDefinition` should contain authored base data only.

Recommended categories:

### Identity

* ID
* display name
* description
* race
* tier
* tags
* enabled state

### Economy

* Gold cost
* private or neutral pool
* pool size
* base shop weight
* sell value rules

### Production

* spawn interval
* maximum simultaneous population
* initial spawn delay
* spawn batch size
* spawn priority

### Combat

* maximum HP
* damage
* attack interval
* attack range
* movement speed
* armor
* resistance values
* targeting profile
* movement profile
* abilities

### Blueprint progression

* permitted per-copy stat upgrades
* socket milestone configuration
* Ascension I threshold
* Ascension I evolution options
* Ascension II threshold
* Ascension II evolution options

### Presentation

* icon
* prefab
* animator configuration
* audio references
* VFX references

Do not place player-owned copies or selected upgrades inside `UnitDefinition`.

---

## 6.4 Structure definition

Structures are also Blueprint-produced entities.

`StructureDefinition` should specify:

* whether it spawns on the battlefield,
* placement restrictions,
* lane,
* maximum population,
* production behavior,
* attack or support behavior,
* Blueprint Board adjacency effects,
* economic effects,
* defensive effects,
* shop tier,
* pool size,
* compatible research,
* evolution or upgrade options.

Do not create a completely separate progression system unless the design bible explicitly requires it.

Use shared blueprint progression systems where appropriate.

---

## 6.5 Research definition

Research is socketable content.

Each research definition should specify:

* compatible blueprint categories,
* compatible races,
* compatible tags,
* rarity,
* cost in race resource,
* effect definitions,
* stacking behavior,
* whether duplicates are allowed,
* whether it can be moved,
* any reassignment cost,
* tooltip data.

Research effects should use modular effect definitions rather than hardcoded research IDs.

---

## 6.6 Artifact definition

Artifacts are normally civilization-wide modifiers.

Each artifact should specify:

* race restrictions,
* philosophy interactions,
* rarity,
* unique or stackable status,
* acquisition rules,
* effect modules,
* tooltip behavior,
* incompatibilities.

Artifacts should not be represented by arbitrary custom code unless their behavior cannot reasonably be represented by reusable effect modules.

---

# 7. Runtime state architecture

Runtime state classes must be plain serializable C# objects where practical.

They should not inherit from `MonoBehaviour`.

Recommended state hierarchy:

```text
GameSessionState
 ├── LobbyState
 ├── MatchState
 │    ├── RoundState
 │    ├── PlayerState[]
 │    ├── PairingState
 │    └── MatchResult
 └── SessionMetadata
```

Player state:

```text
PlayerState
 ├── Player ID
 ├── Race ID
 ├── Match HP
 ├── Gold
 ├── Race resource
 ├── Civilization level
 ├── Interest status
 ├── Nexus upgrade state
 ├── Active Blueprint Board
 ├── Blueprint Bench
 ├── Research Bench
 ├── Artifact state
 └── Shop state
```

Blueprint state:

```text
BlueprintState
 ├── Definition ID
 ├── Owner ID
 ├── Copies purchased
 ├── Ascension level
 ├── Evolution selections
 ├── Per-copy stat upgrades
 ├── Attached research IDs
 ├── Active or benched
 ├── Blueprint Board index
 ├── Assigned lane
 ├── Assigned stance
 └── Derived-stat cache or revision
```

Runtime state must reference definitions by stable ID.

Direct Unity asset references should not be required inside serialized match saves.

---

# 8. Derived statistics

Final runtime statistics should be calculated from layered modifiers.

Recommended order:

```text
Base definition
→ Copy-selected stat upgrades
→ Ascension base modifiers
→ Evolution modifiers
→ Attached research
→ Blueprint adjacency effects
→ Philosophy effects
→ Artifact effects
→ Nexus or civilization effects
→ Temporary round modifiers
```

Modifier order must be documented and deterministic.

Use a reusable stat modifier model.

Recommended modifier types:

```text
FlatAdd
PercentAdd
PercentMultiply
Override
Minimum
Maximum
```

Avoid modifying raw values repeatedly.

Prefer recalculating or caching derived values from source modifiers.

The source of each modifier should be inspectable for debugging and UI tooltips.

Example:

```text
Spider Spawn Interval: 4.2 seconds

Base: 6.0
Copy upgrades: -0.6
Queen adjacency: -0.5
Swarm philosophy: ×0.85
```

---

# 9. Blueprint Board architecture

The active Nexus Blueprint Board is a single ordered line.

It is not a matrix.

Each active blueprint has:

* one board index,
* an optional left neighbor,
* an optional right neighbor.

Blueprint Capacity determines how many active blueprint slots a player may use.

Benched blueprints do not count toward Blueprint Capacity.

Adjacency effects may target:

* left neighbor,
* right neighbor,
* both adjacent neighbors,
* self based on neighbors,
* all blueprints to the left,
* all blueprints to the right,
* blueprints with matching tags.

Blueprint placement rules must be implemented by the Blueprint subsystem, not the UI.

The UI should submit movement commands such as:

```text
MoveBlueprint
SwapBlueprints
BenchBlueprint
ActivateBlueprint
```

The subsystem validates the operation and returns a result.

The UI must never directly mutate the board list.

---

# 10. Shop architecture

Shop generation must be separated from shop presentation.

Recommended components:

```text
ShopService
ShopOfferGenerator
ShopProbabilityTable
PrivatePoolState
SharedNeutralPoolState
ShopState
ShopOffer
ShopTransactionService
```

The shop system must support:

* private race pools,
* shared neutral pools,
* tier odds,
* Civilization Level influence,
* rerolls,
* locked offers if retained,
* disabled content filtering,
* pool depletion,
* duplicate offers where permitted,
* deterministic seeded generation.

Purchasing an offer must be transactional.

A purchase should either fully succeed or leave state unchanged.

Recommended result model:

```text
PurchaseResult
 ├── Success
 ├── Failure reason
 ├── Gold spent
 ├── Pool changes
 ├── Blueprint changes
 └── Triggered milestones
```

---

# 11. Combat architecture

Combat must be divided into simulation and presentation.

## 11.1 Simulation

The simulation owns:

* spawning,
* lane movement,
* targeting,
* attacks,
* damage,
* healing,
* status effects,
* deaths,
* structures,
* Nexus damage,
* combat timers,
* Sudden Death,
* winner determination.

Simulation objects should not require scene objects.

Recommended simulation model:

```text
CombatSimulation
 ├── CombatState
 ├── CombatEntityState[]
 ├── SpawnScheduler
 ├── TargetingSystem
 ├── MovementSystem
 ├── AbilitySystem
 ├── DamageSystem
 ├── StatusEffectSystem
 ├── NexusSystem
 └── CombatEventLog
```

The initial simulation may use fixed ticks.

A recommended prototype tick rate is configurable, such as:

```text
10 simulation ticks per second
```

Do not assume Unity frame rate equals simulation rate.

---

## 11.2 Combat presentation

The presentation layer converts simulation events into:

* models,
* animations,
* projectiles,
* VFX,
* sounds,
* health bars,
* combat text,
* camera behavior.

Presentation may interpolate between simulation states.

Presentation must not decide who was hit or killed.

---

## 11.3 Combat event log

The simulation should emit ordered events.

Examples:

```text
UnitSpawned
UnitMoved
AttackStarted
DamageApplied
UnitDied
AbilityActivated
StatusApplied
NexusDamaged
SuddenDeathStarted
CombatEnded
```

Events should include:

* simulation tick,
* entity IDs,
* source ID,
* target ID,
* relevant values,
* optional presentation hints.

This supports:

* replay,
* debugging,
* spectator mode,
* multiplayer synchronization,
* automated balance reports.

---

# 12. Match flow architecture

Use an explicit state machine.

Recommended states:

```text
Lobby
RaceSelection
MatchInitialization
Planning
CombatPreparation
Combat
RoundResolution
EliminationCheck
NextRound
MatchComplete
```

Transitions must be controlled by the Match subsystem.

UI screens observe the current state and display appropriate controls.

Avoid scene-specific scripts controlling core match progression.

The match system must support configurable formats:

```text
Duel
Rotating multiplayer lobby
Future team modes
Future PvE modes
```

Player count must not be hardcoded inside blueprint, economy, shop, or combat systems.

---

# 13. Multiplayer preparation

Do not implement multiplayer before the local deterministic game loop works.

However, architecture should prepare for it.

Authoritative state changes should be represented by commands.

Examples:

```text
BuyShopOfferCommand
RerollShopCommand
LevelCivilizationCommand
MoveBlueprintCommand
AttachResearchCommand
SelectEvolutionCommand
SetLaneCommand
SetStanceCommand
EndPlanningCommand
```

Commands should contain:

* player ID,
* match ID,
* command type,
* payload,
* sequence number,
* expected state revision where appropriate.

The authoritative host or server validates commands.

The client UI requests actions but does not declare them valid.

---

# 14. UI architecture

Use Unity UI Toolkit for runtime UI and Editor tools unless a specific technical limitation requires otherwise.

Runtime UI should use:

```text
UXML — structure
USS — visual styling
C# View — element references and presentation
Presenter/ViewModel — behavior and binding
Service — gameplay operation
```

Example:

```text
ArmyShopView.uxml
ArmyShopView.uss
ArmyShopView.cs
ArmyShopPresenter.cs
ShopService.cs
```

Views must not contain economy or shop rules.

Presenters must not implement shop probability algorithms.

Services must not know visual hierarchy.

---

## 14.1 Modular UI components

Recommended components:

```text
PlanningScreen
CombatHUD
ArmyShopPanel
ResearchShopPanel
ArtifactShopPanel
BlueprintBoardPanel
BlueprintCard
BlueprintBenchPanel
ResearchBenchPanel
ResourceBar
CivilizationLevelPanel
NexusUpgradePanel
OpponentPreviewPanel
RoundResultPanel
TooltipPanel
ConfirmationDialog
ContentCard
```

Each component should expose a small clear interface.

UI components should be reusable between screens where practical.

---

## 14.2 Theme system

Use a theme asset or centralized USS variable files.

Theme values should include:

* colors,
* spacing,
* font references,
* border sizes,
* corner radius,
* icon sizes,
* animation duration,
* panel transparency,
* rarity presentation.

Do not hardcode visual colors throughout C#.

Race-specific themes should override base theme values without duplicating complete layouts.

---

# 15. Editor tooling architecture

The project must include a visual Content Studio.

Menu location:

```text
Tools > Blueprint Civilizations > Content Studio
```

The Content Studio should eventually support:

* races,
* units,
* structures,
* research,
* artifacts,
* evolutions,
* philosophies,
* abilities,
* configuration assets,
* shop tables.

The editor must provide:

* search,
* filtering,
* create,
* duplicate,
* disable,
* delete with confirmation,
* validation,
* Undo/Redo,
* dirty-state support,
* direct asset selection,
* missing-reference display,
* ID collision detection.

Designers should not need to manually edit JSON.

Custom editors must use Unity serialization APIs where practical.

Do not write directly to asset fields in ways that bypass Undo or dirty-state tracking.

---

# 16. Validation architecture

All definitions must support validation.

Recommended interface:

```csharp
public interface IValidatableContent
{
    IEnumerable<ValidationIssue> Validate(ValidationContext context);
}
```

Validation issue fields:

```text
Severity
Definition ID
Asset path
Field name
Message
Suggested fix
```

Severity levels:

```text
Info
Warning
Error
Critical
```

Validation should run:

* inside Content Studio,
* when requested from a menu command,
* before builds,
* in automated tests,
* optionally during CI.

Example validation rules:

* missing ID,
* duplicate ID,
* missing display name,
* invalid tier,
* negative Gold cost,
* zero spawn interval,
* zero population,
* invalid evolution references,
* incompatible research,
* missing prefab,
* disabled referenced content,
* empty shop pool,
* impossible ascension thresholds.

---

# 17. Save-data architecture

Save files should serialize stable runtime data, not Unity scene references.

Recommended save content:

```text
Save version
Game version
Content database version
Player profile
Meta progression
Settings
Optional suspended match
```

A suspended match should store:

* definition IDs,
* player runtime state,
* shop pool state,
* random stream state or seeds,
* round number,
* pairing history,
* command history where necessary.

Use explicit save versions and migration functions.

Never assume old save data exactly matches the newest class structure.

---

# 18. Meta progression architecture

Meta progression must unlock variety rather than direct competitive power.

Store unlocks as stable content IDs.

Examples:

```text
Unlocked research IDs
Unlocked artifact IDs
Unlocked evolution IDs
Unlocked cosmetic IDs
Unlocked race variants
```

Matchmaking and competitive rules should ensure that unlock breadth does not create an unfair numerical advantage.

Meta progression must be separate from in-match state.

---

# 19. Testing strategy

Testing is mandatory for core systems.

## EditMode tests

Use for:

* content validation,
* stable IDs,
* catalogs,
* economy calculations,
* shop odds,
* pool depletion,
* blueprint placement,
* adjacency,
* copy milestones,
* ascension,
* socket compatibility,
* stat calculations,
* match state transitions,
* deterministic combat,
* save migration.

## PlayMode tests

Use for:

* UI Toolkit integration,
* scene composition,
* presentation bindings,
* drag-and-drop,
* runtime object lifecycle,
* combat visual synchronization.

## Simulation tests

Support running many combats without rendering.

Balance simulations should produce reports such as:

* faction win rate,
* unit usage,
* unit survival,
* Nexus damage,
* round duration,
* Gold efficiency,
* spawn efficiency,
* philosophy performance.

Tests should not depend on arbitrary timing when deterministic alternatives exist.

---

# 20. Dependency management

Avoid service-location patterns that hide dependencies.

Do not use a global singleton for every system.

Use an explicit composition root when entering the game.

Example responsibilities:

```text
GameCompositionRoot
 ├── builds services
 ├── loads content catalog
 ├── creates random sources
 ├── creates match state
 ├── creates presenters
 └── connects event streams
```

Constructor injection is preferred for plain C# classes.

Unity component references may be configured through serialized fields when they are presentation-only dependencies.

Interfaces should be used at meaningful subsystem boundaries.

Do not create interfaces for every trivial class.

---

# 21. Error handling and diagnostics

Errors must be actionable.

Bad:

```text
Invalid unit.
```

Good:

```text
UnitDefinition 'unit.hive.spider' has Spawn Interval 0.
Spawn Interval must be greater than 0.
Asset: Assets/_Game/Content/Assets/Units/Hive/Spider.asset
```

Gameplay command failures should return structured results instead of throwing exceptions for expected invalid player actions.

Unexpected programmer errors may throw exceptions during development.

Add structured logging for:

* match state transitions,
* shop generation,
* purchases,
* blueprint movement,
* evolution selection,
* combat initialization,
* deterministic seed selection,
* save migration.

---

# 22. Coding standards

Use clear descriptive naming.

Avoid unexplained abbreviations.

Public APIs should have XML documentation.

Use one primary type per file unless tightly coupled private helper types justify otherwise.

Avoid classes with excessive responsibilities.

Avoid methods with long parameter lists; use request or context objects where appropriate.

Avoid magic numbers.

Store balancing values in data assets or configuration objects.

Use readonly fields where practical.

Prefer immutable result objects for operations.

Use nullable reference type analysis if compatible with project configuration.

Treat warnings as issues to investigate.

Do not commit commented-out code.

Do not claim placeholder code is complete.

Use TODO comments only when accompanied by a clear reason or tracked task.

---

# 23. Naming conventions

Use PascalCase for:

* classes,
* methods,
* properties,
* enums,
* public fields where unavoidable.

Use camelCase for:

* local variables,
* parameters,
* private fields.

Serialized private fields may use:

```csharp
[SerializeField] private string displayName;
```

Interfaces use the `I` prefix:

```text
IRandomSource
IContentCatalog
IShopService
```

Definition classes use the `Definition` suffix.

Runtime state classes use the `State` suffix.

Commands use the `Command` suffix.

Results use the `Result` suffix.

Events use past-tense event names where appropriate:

```text
BlueprintPurchased
UnitSpawned
CombatEnded
```

---

# 24. Performance principles

Correctness and maintainability come before premature optimization.

However:

* avoid repeated asset lookups by name,
* cache catalog lookups by stable ID,
* avoid per-frame allocations in combat,
* pool visual combat objects,
* avoid excessive LINQ inside frequent simulation ticks,
* keep simulation independent of scene hierarchy searches,
* do not use `FindObjectOfType` as regular architecture,
* do not load individual assets repeatedly during a match.

Performance-sensitive code should be profiled before major optimization.

---

# 25. AI coding-agent rules

Before implementing a task, the coding agent must:

1. Read `AGENTS.md`.
2. Read this document.
3. Read the relevant Game Design Bible volumes.
4. Inspect existing implementation.
5. Identify assumptions.
6. Limit work to the requested milestone.
7. Add or update tests.
8. Run compilation and tests.
9. Report all changed files.
10. Report remaining limitations honestly.

The coding agent must not:

* invent new gameplay systems,
* silently remove documented mechanics,
* hardcode content,
* mix UI and gameplay logic,
* mutate ScriptableObjects at runtime,
* skip tests while claiming completion,
* start future milestones without being asked,
* rewrite working architecture without explaining the need.

---

# 26. Definition of done

A task is complete only when:

* the project compiles,
* relevant tests pass,
* no known critical validation errors remain,
* implementation follows assembly boundaries,
* authored content remains data-driven,
* public behavior is documented,
* changed files are listed,
* assumptions are reported,
* limitations are reported,
* no unrelated milestone was started.

---

# 27. Initial implementation sequence

The preferred order is:

## Milestone 0 — Foundation

* project assemblies,
* content definitions,
* stable IDs,
* content catalog,
* validation,
* visual Content Studio,
* sample content,
* EditMode tests.

## Milestone 1 — Blueprint planning

* runtime Blueprint State,
* active linear board,
* bench,
* Blueprint Capacity,
* movement commands,
* adjacency,
* tests.

## Milestone 2 — Economy and shops

* Gold,
* interest,
* race resources,
* Civilization Level,
* private race pools,
* shared neutral pool,
* shop offers,
* purchasing,
* rerolling.

## Milestone 3 — Headless combat

* two lanes,
* Nexus spawning,
* population caps,
* targeting,
* movement,
* damage,
* death,
* Nexus HP,
* combat timing,
* Sudden Death,
* deterministic results.

## Milestone 4 — Blueprint progression

* copy acquisition,
* per-copy stat upgrades,
* sockets,
* research attachment,
* Ascension I,
* evolution selection,
* Ascension II.

## Milestone 5 — Runtime presentation and UI

* planning UI,
* combat presentation,
* HUD,
* modular panels,
* themes.

## Milestone 6 — Complete local duel

* race selection,
* full phase loop,
* rewards,
* Match HP,
* victory and defeat.

## Milestone 7 — Multiplayer lobby

* player lobby,
* opponent pairing,
* ghost combat,
* elimination,
* authoritative networking,
* disconnect handling.

---

# 28. Revision policy

Changes to this document must include:

* version number,
* date,
* changed section,
* reason,
* compatibility impact.

Do not silently change architectural contracts after implementation has begun.

---

# 29. Revision history

## Version 1.0

Initial architecture specification.

Defines:

* data-driven content,
* ScriptableObject authoring,
* runtime-state separation,
* deterministic combat,
* modular UI,
* visual Content Studio,
* save compatibility,
* subsystem boundaries,
* AI coding-agent requirements.
