using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    public abstract class ContentDefinition : ScriptableObject
    {
        [HideInInspector] [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [TextArea(2, 8)] [SerializeField] private string description = "";
        [SerializeField] private int dataVersion = 1;
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private List<string> tags = new();
        [SerializeField] private Sprite icon;

        /// <summary>Immutable identifier used by saves, catalogs, and cross-content references.</summary>
        public string Id => id;
        /// <summary>Player-facing content name.</summary>
        public string DisplayName => displayName;
        /// <summary>Player-facing content description.</summary>
        public string Description => description;
        /// <summary>Schema version for migration and compatibility checks.</summary>
        public int DataVersion => dataVersion;
        /// <summary>Whether this content may be offered or used in new matches.</summary>
        public bool IsEnabled => isEnabled;
        /// <summary>Authored tags used by compatibility and rule selectors.</summary>
        public IReadOnlyList<string> Tags => tags.AsReadOnly();
        /// <summary>Optional presentation icon.</summary>
        public Sprite Icon => icon;

#if UNITY_EDITOR
        /// <summary>Assigns identity to a newly created definition exactly once.</summary>
        public void EditorInitialize(string newId, string newDisplayName)
        {
            if (!string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException($"Content ID '{id}' has already been assigned.");
            AssignIdentity(newId, newDisplayName);
        }

        /// <summary>Assigns a fresh identity to a newly duplicated asset.</summary>
        public void EditorAssignDuplicateIdentity(string newId, string newDisplayName)
        {
            AssignIdentity(newId, newDisplayName);
        }

        /// <summary>Changes enabled state through controlled editor tooling.</summary>
        public void EditorSetEnabled(bool value) => isEnabled = value;

        private void AssignIdentity(string newId, string newDisplayName)
        {
            if (string.IsNullOrWhiteSpace(newId)) throw new ArgumentException("Content ID is required.", nameof(newId));
            if (string.IsNullOrWhiteSpace(newDisplayName)) throw new ArgumentException("Display name is required.", nameof(newDisplayName));
            id = newId.Trim();
            displayName = newDisplayName.Trim();
        }
#endif
    }
}
