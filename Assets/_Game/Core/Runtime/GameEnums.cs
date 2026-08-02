namespace BlueprintCivilizations.Core
{
    /// <summary>Authored content tier used by shop and progression data.</summary>
    public enum ContentTier { Tier1 = 1, Tier2 = 2, Tier3 = 3, Tier4 = 4, Legendary = 5 }

    /// <summary>Authoring-time lane restrictions for a produced entity.</summary>
    public enum LaneCompatibility { Any, LeftOnly, RightOnly, Split }

    /// <summary>Authored evolution milestone.</summary>
    public enum AscensionLevel { Base, AscensionOne, AscensionTwo }

    /// <summary>Primary deterministic target-selection override.</summary>
    public enum TargetPriority { Nearest, LowestHealth, HighestHealth, UnitsFirst, StructuresFirst, NexusFirst }
}
