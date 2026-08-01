namespace BlueprintCivilizations.Core
{
    public enum ContentTier { Tier1 = 1, Tier2 = 2, Tier3 = 3, Tier4 = 4, Legendary = 5 }
    public enum LaneCompatibility { Any, LeftOnly, RightOnly, Flying }
    public enum UnitLane { Left, Right }
    public enum UnitStance { Assault, Defense }
    public enum BlueprintLocation { Active, Bench }
    public enum AscensionLevel { Base, AscensionOne, AscensionTwo }
    public enum StatUpgradeType { MaxHealth, AttackDamage, AttackSpeed, MoveSpeed, SpawnSpeed, MaxPopulation, AbilityPower }
    public enum TargetPriority { Nearest, LowestHealth, HighestHealth, UnitsFirst, StructuresFirst, NexusFirst }
}
