using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BlueprintCivilizations.Blueprints;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.Core;
using BlueprintCivilizations.UI.ViewModels;

namespace BlueprintCivilizations.UI.Presenters
{
    /// <summary>Resolves immutable authored data and current runtime state into a presentation-only model.</summary>
    public interface IBlueprintDetailsResolver
    {
        BlueprintDetailsViewModel Resolve(BlueprintState blueprint, BlueprintBoardState board);
    }

    /// <summary>Catalog-backed Milestone 1 resolver. Current statistics equal base until modifier services exist.</summary>
    public sealed class ContentCatalogBlueprintDetailsResolver : IBlueprintDetailsResolver
    {
        private readonly GameContentCatalog catalog;
        private readonly BlueprintAdjacencyService adjacency;
        private readonly bool showDeveloperId;

        public ContentCatalogBlueprintDetailsResolver(GameContentCatalog catalog, BlueprintAdjacencyService adjacency,
            bool showDeveloperId)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.adjacency = adjacency ?? throw new ArgumentNullException(nameof(adjacency));
            this.showDeveloperId = showDeveloperId;
        }

        public BlueprintDetailsViewModel Resolve(BlueprintState blueprint, BlueprintBoardState board)
        {
            if (blueprint == null) return BlueprintDetailsViewModel.Empty();
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (!catalog.TryGet<ContentDefinition>(blueprint.DefinitionId, out var definition, out string error,
                    includeDisabled: true))
            {
                return BlueprintDetailsViewModel.Error(blueprint.DefinitionId,
                    $"Blueprint details could not resolve stable ID '{blueprint.DefinitionId}'. {error}", showDeveloperId);
            }

            return definition switch
            {
                UnitDefinition unit => ResolveUnit(blueprint, board, unit),
                StructureDefinition structure => ResolveStructure(blueprint, board, structure),
                _ => BlueprintDetailsViewModel.Error(blueprint.DefinitionId,
                    $"Content ID '{blueprint.DefinitionId}' resolves to unsupported Blueprint type {definition.GetType().Name}. " +
                    "Only UnitDefinition and StructureDefinition are valid Milestone 1 Blueprint details sources.", showDeveloperId)
            };
        }

        private BlueprintDetailsViewModel ResolveUnit(BlueprintState state, BlueprintBoardState board, UnitDefinition unit)
        {
            var production = new BlueprintDetailsSectionViewModel("Production",
                Array.Empty<BlueprintDetailsValueViewModel>(), new[]
                {
                    Stat("Spawn interval", unit.ProductionStats.SpawnInterval, "seconds"),
                    Stat("Spawn batch size", unit.ProductionStats.SpawnBatchSize),
                    Stat("Maximum population", unit.ProductionStats.MaximumPopulation),
                    Stat("Initial spawn delay", unit.ProductionStats.InitialSpawnDelay, "seconds"),
                    Stat("Production priority", unit.ProductionStats.SpawnPriority)
                });

            string targetProfile = $"{Friendly(unit.Targeting.Priority)}; Ground targets: {YesNo(unit.Targeting.CanTargetGround)}; " +
                                   $"Flying targets: {YesNo(unit.Targeting.CanTargetFlying)}";
            var combat = new BlueprintDetailsSectionViewModel("Combat", new[]
                {
                    Value("Targeting profile", targetProfile),
                    Value("Movement profile", Friendly(unit.MovementProfile)),
                    Value("Lane compatibility", Friendly(unit.LaneCompatibility))
                }, new[]
                {
                    Stat("Maximum health", unit.CombatStats.MaxHealth),
                    Stat("Attack damage", unit.CombatStats.AttackDamage),
                    Stat("Attack interval", unit.CombatStats.AttackIntervalSeconds, "seconds"),
                    Stat("Attack range", unit.CombatStats.AttackRange, "tiles"),
                    Stat("Movement speed", unit.CombatStats.MovementSpeed, "tiles/second"),
                    Stat("Armor", unit.CombatStats.Armor),
                    Stat("Resistance", unit.CombatStats.Resistance)
                });

            return Build(state, board, unit, unit.Race, unit.IsNeutral, unit.Tier, unit.Role,
                "Unit Blueprint", unit.GoldCost, unit.PoolKind, unit.ShopPoolSize,
                production, combat);
        }

        private BlueprintDetailsViewModel ResolveStructure(BlueprintState state, BlueprintBoardState board,
            StructureDefinition structure)
        {
            var production = new BlueprintDetailsSectionViewModel("Production", new[]
                {
                    Value("Battlefield placement", structure.SpawnsOnBattlefield ? "Placed at combat start" : "Does not spawn on the battlefield"),
                    Value("Production priority", "Not applicable to this structure")
                }, new[]
                {
                    Stat("Maximum population", structure.MaximumPopulation),
                    Stat("Reconstruction interval", structure.ReconstructionInterval, "seconds",
                        structure.ReconstructionInterval <= 0f ? "No automatic reconstruction is authored." : "")
                });

            string support = string.IsNullOrWhiteSpace(structure.RulesSummary)
                ? "No authored structure support or adjacency summary."
                : structure.RulesSummary;
            var combat = new BlueprintDetailsSectionViewModel("Combat", new[]
                {
                    Value("Attack behavior", structure.Abilities.Count == 0
                        ? "Does not attack; no attack ability is authored."
                        : $"Uses {structure.Abilities.Count} authored ability or abilities."),
                    Value("Movement profile", "Stationary structure"),
                    Value("Lane compatibility", Friendly(structure.LaneCompatibility)),
                    Value("Support and adjacency", support)
                }, new[]
                {
                    Stat("Maximum health", structure.BaseHealth),
                    Stat("Armor", structure.Armor),
                    Stat("Resistance", structure.Resistance)
                });

            return Build(state, board, structure, structure.Race, structure.IsNeutral, structure.Tier, "Structure",
                "Structure Blueprint", structure.GoldCost, structure.PoolKind, structure.ShopPoolSize,
                production, combat);
        }

        private BlueprintDetailsViewModel Build(BlueprintState state, BlueprintBoardState board,
            ContentDefinition definition, RaceDefinition race, bool isNeutral, ContentTier tier, string role,
            string contentType, int goldCost, ContentPoolKind poolKind, int poolSize,
            BlueprintDetailsSectionViewModel production, BlueprintDetailsSectionViewModel combat)
        {
            string raceName = isNeutral ? "Neutral" : race != null
                ? (string.IsNullOrWhiteSpace(race.DisplayName) ? race.Id : race.DisplayName)
                : "Missing race reference";
            var identity = new BlueprintDetailsSectionViewModel("Identity", new[]
            {
                Value("Race", raceName),
                Value("Content type", contentType),
                Value("Tier", Friendly(tier)),
                Value("Role", string.IsNullOrWhiteSpace(role) ? "Unspecified" : role),
                Value("Description", string.IsNullOrWhiteSpace(definition.Description) ? "No description authored." : definition.Description),
                Value("Board state", state.Location == BlueprintLocationState.Active ? "Active" : "Benched")
            });

            BlueprintAdjacentPair pair = adjacency.GetAdjacentPair(board, state.DefinitionId);
            string left = DefinitionName(pair.Left?.DefinitionId);
            string right = DefinitionName(pair.Right?.DefinitionId);
            var assignment = new BlueprintDetailsSectionViewModel("Board assignment", new[]
            {
                Value("Location", state.Location == BlueprintLocationState.Active ? "Active Board" : "Bench"),
                Value("Active slot index", state.Location == BlueprintLocationState.Active
                    ? state.BlueprintBoardIndex.ToString(CultureInfo.InvariantCulture)
                    : "Bench"),
                Value("Assigned lane", Friendly(state.AssignedLane)),
                Value("Assigned stance", Friendly(state.AssignedStance)),
                Value("Left neighbor", left),
                Value("Right neighbor", right),
                Value("Adjacency relationships", $"Left: {left}; Right: {right}")
            });

            var progression = new BlueprintDetailsSectionViewModel("Progression preview", new[]
            {
                Value("Copies owned", state.CopiesPurchased.ToString(CultureInfo.InvariantCulture)),
                Value("Ascension level", Friendly(state.AscensionLevel)),
                Value("Selected refinements", JoinOrUnavailable(state.SelectedPerCopyStatUpgradeIds)),
                Value("Socket count", state.AttachedResearchIds.Count.ToString(CultureInfo.InvariantCulture),
                    "Milestone 1 reports occupied runtime socket entries only; socket progression is not implemented."),
                Value("Attached research", JoinOrUnavailable(state.AttachedResearchIds)),
                Value("Selected evolution", JoinOrUnavailable(state.ChosenEvolutionIds)),
                Milestone(1, "Socket 1", state.CopiesPurchased),
                Milestone(4, "Socket 2", state.CopiesPurchased),
                Milestone(5, "Ascension I", state.CopiesPurchased),
                Milestone(9, "Socket 3", state.CopiesPurchased),
                Milestone(10, "Ascension II", state.CopiesPurchased)
            });

            var shop = new BlueprintDetailsSectionViewModel("Shop information", new[]
            {
                Value("Gold cost", goldCost.ToString(CultureInfo.InvariantCulture)),
                Value("Pool kind", Friendly(poolKind)),
                Value("Base pool size", poolSize.ToString(CultureInfo.InvariantCulture)),
                Value("Shop tier", Friendly(tier)),
                Value("Live pool count", "Not available; Shops are not implemented.")
            });

            return new BlueprintDetailsViewModel(false, BlueprintDetailsViewModel.DefaultEmptyMessage, definition.Icon,
                definition.DisplayName, definition.Id, showDeveloperId, "",
                new[] { identity, production, combat, assignment, progression, shop });
        }

        private string DefinitionName(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "None";
            return catalog.TryGet<ContentDefinition>(id, out var definition, includeDisabled: true)
                ? definition.DisplayName
                : $"Missing definition ({id})";
        }

        private static BlueprintDetailsValueViewModel Milestone(int copies, string reward, int copiesOwned) =>
            Value($"Copy {copies} milestone", copiesOwned >= copies
                ? $"Threshold reached; {reward} is Locked until progression is implemented."
                : $"{reward} — Locked until {copies} copies.");

        private static BlueprintDetailsValueViewModel Value(string label, string value, string tooltip = "") =>
            new(label, value, tooltip);

        private static BlueprintStatViewModel Stat(string label, float value, string unit = "", string tooltip = "")
        {
            string formatted = value.ToString("0.##", CultureInfo.InvariantCulture);
            return new BlueprintStatViewModel(label, formatted, formatted, unit,
                Array.Empty<BlueprintStatModifierViewModel>(), tooltip);
        }

        private static BlueprintStatViewModel Stat(string label, int value, string unit = "", string tooltip = "") =>
            Stat(label, (float)value, unit, tooltip);

        private static string JoinOrUnavailable(IEnumerable<string> values)
        {
            string[] present = (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            return present.Length == 0 ? "Not yet acquired" : string.Join(", ", present);
        }

        private static string YesNo(bool value) => value ? "Yes" : "No";

        private static string Friendly(Enum value) => value switch
        {
            AscensionLevel.Base => "Base (0)",
            AscensionLevel.AscensionOne => "Ascension I",
            AscensionLevel.AscensionTwo => "Ascension II",
            BlueprintLane.Unassigned => "Unassigned",
            BlueprintLane.Left => "Left lane",
            BlueprintLane.Right => "Right lane",
            BlueprintLane.Split => "Split lanes",
            BlueprintStance.Unassigned => "Unassigned",
            BlueprintStance.Assault => "Assault",
            BlueprintStance.Defense => "Defense",
            ContentPoolKind.PrivateRace => "Private race pool",
            ContentPoolKind.SharedNeutral => "Shared neutral pool",
            ContentPoolKind.NotInArmyShop => "Not in Army Shop",
            ContentTier tier => $"Tier {(int)tier}",
            LaneCompatibility.Any => "Any lane",
            LaneCompatibility.LeftOnly => "Left lane only",
            LaneCompatibility.RightOnly => "Right lane only",
            LaneCompatibility.Split => "Split lanes",
            MovementProfileKind.Ground => "Ground",
            MovementProfileKind.Flying => "Flying",
            MovementProfileKind.Stationary => "Stationary",
            MovementProfileKind.Burrowing => "Burrowing",
            _ => value.ToString()
        };
    }
}
