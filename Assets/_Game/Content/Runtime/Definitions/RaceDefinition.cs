using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Race", fileName = "Race_")]
    public sealed class RaceDefinition : ContentDefinition
    {
        [SerializeField] private string uniqueResourceName = "";
        [SerializeField] private Color identityColor = Color.white;
        public string UniqueResourceName => uniqueResourceName;
        public Color IdentityColor => identityColor;
    }
}
