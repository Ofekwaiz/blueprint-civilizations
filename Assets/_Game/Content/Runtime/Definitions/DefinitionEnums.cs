namespace BlueprintCivilizations.Content.Definitions
{
    /// <summary>Source pool used when generating or returning shop copies.</summary>
    public enum ContentPoolKind { PrivateRace, SharedNeutral, NotInArmyShop }

    /// <summary>Research, artifact, and augment rarity.</summary>
    public enum ContentRarity { Common, Rare, Epic, Race, Legendary }

    /// <summary>How repeated instances of an effect combine.</summary>
    public enum EffectStackRule { Unique, Stackable, ReplaceExisting, HighestValue }

    /// <summary>How a modifier changes its target statistic.</summary>
    public enum ModifierOperation { FlatAdd, PercentAdd, PercentMultiply, Override, Minimum, Maximum }

    /// <summary>Lifetime of an authored modifier.</summary>
    public enum ModifierDurationScope { PlanningSnapshot, CombatDynamic, PermanentRun }

    /// <summary>Simulation event that activates an authored trigger.</summary>
    public enum TriggerEventType { OnSpawn, OnDeath, OnAttack, OnDamaged, OnRoundStart, OnCombatStart, OnCombatEnd }

    /// <summary>Authored movement model; it is separate from lane assignment.</summary>
    public enum MovementProfileKind { Ground, Flying, Stationary, Burrowing }
}
