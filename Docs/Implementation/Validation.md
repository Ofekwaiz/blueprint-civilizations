# Content Validation

## Validation issue model

Every issue records:

- severity (`Info`, `Warning`, `Error`, or `Critical`),
- definition ID,
- exact Unity asset path,
- serialized field name,
- actionable message,
- suggested fix.

Errors and Critical issues block player builds through `ContentBuildValidator`. Warnings identify incomplete or risky authoring, such as missing presentation prefabs or references to disabled content, without blocking the Milestone 0 sample.

## Running validation

- In Content Studio, select an asset to see its issues in the bottom panel.
- Run `Tools > Blueprint Civilizations > Validate All Content` for the entire authored database.
- Run `Tools > Blueprint Civilizations > Rebuild Default Content Catalog` after adding, moving, disabling, or deleting assets.
- Open Unity Test Runner and run all EditMode tests in both `BlueprintCivilizations.Tests` and `BlueprintCivilizations.Blueprints.Tests`.

The default catalog lives at `Assets/_Game/Content/Assets/Configuration/GameContentCatalog.asset`.

## Milestone 1 warning baseline

The verified 13-asset prototype catalog has five non-blocking warnings and no content-validation errors. These warnings remain visible intentionally; none is an actual data defect or a reason to suppress validation.

| Definition ID | Asset path | Field | Message | Suggested fix | Classification |
| --- | --- | --- | --- | --- | --- |
| `HIVE_LARVA` | `Assets/_Game/Content/Assets/Units/Hive/Unit_Hive_Larva.asset` | `ascensionOneOptions` | No Ascension I evolution is assigned. This is non-blocking for Milestone 1 because Blueprint progression is not implemented. | Author the legal evolution paths before the Blueprint progression milestone. | Design data incomplete; expected future progression authoring. |
| `HIVE_LARVA` | `Assets/_Game/Content/Assets/Units/Hive/Unit_Hive_Larva.asset` | `visualPrefab` | Visual prefab is not assigned. This optional presentation asset is non-blocking for Milestone 1 board planning. | Assign a presentation prefab before runtime entity presentation work. | Optional presentation asset missing. |
| `HIVE_SPIDER` | `Assets/_Game/Content/Assets/Units/Hive/Unit_Hive_Spider.asset` | `visualPrefab` | Visual prefab is not assigned. This optional presentation asset is non-blocking for Milestone 1 board planning. | Assign a presentation prefab before runtime entity presentation work. | Optional presentation asset missing. |
| `HIVE_BEETLE` | `Assets/_Game/Content/Assets/Units/Hive/Unit_Hive_Beetle.asset` | `ascensionOneOptions` | No Ascension I evolution is assigned. This is non-blocking for Milestone 1 because Blueprint progression is not implemented. | Author the legal evolution paths before the Blueprint progression milestone. | Design data incomplete; expected future progression authoring. |
| `HIVE_BEETLE` | `Assets/_Game/Content/Assets/Units/Hive/Unit_Hive_Beetle.asset` | `visualPrefab` | Visual prefab is not assigned. This optional presentation asset is non-blocking for Milestone 1 board planning. | Assign a presentation prefab before runtime entity presentation work. | Optional presentation asset missing. |

The three prefab warnings are expected presentation placeholders. The two Ascension warnings identify real but deliberately deferred design authoring; progression is outside Milestone 1. Icons, audio, animation, and VFX remain optional and are not currently warning-producing fields.

## Current checks

Validation covers common identity/version/tag rules, duplicate IDs, missing references, disabled dependencies, unit race/tier/economy/production/combat/progression rules, socket milestones, per-copy upgrades, structure data, research/artifact effects, evolution source IDs, trigger/modifier integrity, Nexus values, and five-level shop-odds configuration.

Catalog reconstruction separately rejects missing entries, empty IDs, and duplicate IDs. Runtime lookup provides an actionable error for empty, missing, wrong-type, or disabled IDs.

## Extending validation

1. Add the rule to the appropriate type-specific method in `ContentValidator`.
2. Use the serialized field path used by Unity so Content Studio points at the exact authoring location.
3. Choose the lowest severity that still protects runtime correctness.
4. Include a concrete suggested fix.
5. Add one valid and one invalid EditMode test where practical.

## Test execution

From Unity, open **Window > General > Test Runner**, choose **EditMode**, and run all tests. The suite covers identity, deterministic catalog construction, lookup failures, disabled/race/tier/tag filtering, unit validation, evolution references, runtime-state separation, sample-generator idempotency, real sample assets, catalog completeness, and UXML/USS loading.

The second Milestone 1 verification inventory contains 54 tests: 20 in `BlueprintCivilizations.Tests` and 34 in `BlueprintCivilizations.Blueprints.Tests`. The additional Editor coverage creates and reloads temporary Panel Settings assets, verifies Panel Settings reuse, rejects a missing UXML before generating dependent assets, repairs missing `UIDocument` and Panel Settings references, and creates the sandbox scene repeatedly without duplicate roots or components. Tests delete their temporary asset folders and scenes after execution.

The preceding Unity result file, `Errors/TestResults_20260802_163145.xml`, discovered 48 tests and recorded 47 passed and one failed. The failed test belonged to `BlueprintCivilizations.Blueprints.Tests.dll`; its use of runtime `SceneManager.CreateScene` from EditMode caused Unity 6 to throw before any assertion. The regression now uses `EditorSceneManager.NewScene`, matching actual Editor lifecycle rules rather than weakening the test expectation.

Command-line execution may be performed with Unity's `-runTests -testPlatform EditMode` arguments. A valid Unity license session is required before import or tests begin.

### Local Unity setup note

`External Code Editor application path does not exist` is a local Unity Preferences warning, not a project gameplay, assembly, or content-validation defect. Select an installed editor under **Edit > Preferences > External Tools > External Script Editor**, or reinstall the configured editor. Project architecture and gameplay code must not be changed to suppress this machine-specific warning.

## Limitations

Validation confirms authored structure and references; it cannot prove balance, combat behavior, runtime UI, or future save migration. Modifier statistic and selector names are currently validated for presence, while semantic registration against future simulation stat/target registries belongs to the milestone that introduces those registries.
