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
- Open Unity Test Runner and run all EditMode tests in `BlueprintCivilizations.Tests`.

The default catalog lives at `Assets/_Game/Content/Assets/Configuration/GameContentCatalog.asset`.

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

Command-line execution may be performed with Unity’s `-runTests -testPlatform EditMode` arguments. A valid Unity license session is required before import or tests begin.

## Limitations

Validation confirms authored structure and references; it cannot prove balance, combat behavior, runtime UI, or future save migration. Modifier statistic and selector names are currently validated for presence, while semantic registration against future simulation stat/target registries belongs to the milestone that introduces those registries.
