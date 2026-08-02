using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Race", fileName = "Race_")]
    public sealed class RaceDefinition : ContentDefinition
    {
        [SerializeField] private string uniqueResourceName = "";
        [SerializeField] private Color identityColor = Color.white;
        [SerializeField] private NexusDefinition nexus;
        [SerializeField] private UnitDefinition startingUnit;
        [SerializeField] private List<UnitDefinition> permittedUnits = new();
        [SerializeField] private List<StructureDefinition> permittedStructures = new();
        [SerializeField] private List<ResearchDefinition> permittedResearch = new();
        [SerializeField] private List<ArtifactDefinition> permittedArtifacts = new();
        [SerializeField] private List<AbilityDefinition> ruleModules = new();

        public string UniqueResourceName => uniqueResourceName;
        public Color IdentityColor => identityColor;
        public NexusDefinition Nexus => nexus;
        public UnitDefinition StartingUnit => startingUnit;
        public IReadOnlyList<UnitDefinition> PermittedUnits => permittedUnits.AsReadOnly();
        public IReadOnlyList<StructureDefinition> PermittedStructures => permittedStructures.AsReadOnly();
        public IReadOnlyList<ResearchDefinition> PermittedResearch => permittedResearch.AsReadOnly();
        public IReadOnlyList<ArtifactDefinition> PermittedArtifacts => permittedArtifacts.AsReadOnly();
        public IReadOnlyList<AbilityDefinition> RuleModules => ruleModules.AsReadOnly();
    }
}
