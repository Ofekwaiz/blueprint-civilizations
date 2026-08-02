using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    /// <summary>Tier probability row for one civilization level.</summary>
    [Serializable]
    public sealed class ShopTierOddsRow
    {
        [Min(1)] [SerializeField] private int civilizationLevel = 1;
        [SerializeField] private List<float> tierPercentages = new() { 100, 0, 0, 0, 0 };

        public int CivilizationLevel => civilizationLevel;
        public IReadOnlyList<float> TierPercentages => tierPercentages.AsReadOnly();
    }

    /// <summary>Versioned, designer-authored prototype economy, shop, and combat values.</summary>
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Game Balance Configuration", fileName = "Config_GameBalance")]
    public sealed class GameBalanceConfigurationDefinition : ContentDefinition
    {
        [Min(0)] [SerializeField] private int startingGold = 5;
        [Min(1)] [SerializeField] private int duelMatchHealth = 10;
        [Min(1)] [SerializeField] private int lobbyMatchHealth = 20;
        [Min(1)] [SerializeField] private int combatNexusHealth = 1000;
        [Min(0.01f)] [SerializeField] private float simulationTicksPerSecond = 10;
        [Min(1)] [SerializeField] private int normalCombatSeconds = 40;
        [Min(1)] [SerializeField] private int acceleratedCombatSeconds = 20;
        [Min(1)] [SerializeField] private int startingCivilizationLevel = 1;
        [Min(1)] [SerializeField] private int startingBlueprintCapacity = 3;
        [Min(0)] [SerializeField] private int rerollCost = 2;
        [Min(0)] [SerializeField] private int researchPackRaceCost = 8;
        [Min(0)] [SerializeField] private int artifactPackRaceCost = 18;
        [Min(1)] [SerializeField] private int armyShopOfferCount = 5;
        [SerializeField] private List<ShopTierOddsRow> shopTierOdds = new();

        public int StartingGold => startingGold;
        public int DuelMatchHealth => duelMatchHealth;
        public int LobbyMatchHealth => lobbyMatchHealth;
        public int CombatNexusHealth => combatNexusHealth;
        public float SimulationTicksPerSecond => simulationTicksPerSecond;
        public int NormalCombatSeconds => normalCombatSeconds;
        public int AcceleratedCombatSeconds => acceleratedCombatSeconds;
        public int StartingCivilizationLevel => startingCivilizationLevel;
        public int StartingBlueprintCapacity => startingBlueprintCapacity;
        public int RerollCost => rerollCost;
        public int ResearchPackRaceCost => researchPackRaceCost;
        public int ArtifactPackRaceCost => artifactPackRaceCost;
        public int ArmyShopOfferCount => armyShopOfferCount;
        public IReadOnlyList<ShopTierOddsRow> ShopTierOdds => shopTierOdds.AsReadOnly();
    }
}
