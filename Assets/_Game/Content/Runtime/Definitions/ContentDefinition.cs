using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    public abstract class ContentDefinition : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [TextArea(2, 8)] [SerializeField] private string description = "";
        [SerializeField] private int dataVersion = 1;
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private List<string> tags = new();
        [SerializeField] private Sprite icon;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int DataVersion => dataVersion;
        public bool IsEnabled => isEnabled;
        public IReadOnlyList<string> Tags => tags;
        public Sprite Icon => icon;

#if UNITY_EDITOR
        public void EditorInitialize(string newId, string newDisplayName)
        {
            id = newId;
            displayName = newDisplayName;
        }
#endif
    }
}
