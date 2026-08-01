using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.Content.Validation;
using UnityEditor;
using UnityEngine;

namespace BlueprintCivilizations.Editor.ContentStudio
{
    public static class ContentTools
    {
        [MenuItem("Tools/Blueprint Civilizations/Validate All Content")]
        public static void ValidateAll()
        {
            var all = AssetDatabase.FindAssets("t:ContentDefinition").Select(g => AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(x => x != null).ToList();
            int errors = 0;
            foreach (var definition in all)
            foreach (var issue in ContentValidator.Validate(definition, all))
            {
                if (issue.Severity == ValidationSeverity.Error) errors++;
                Debug.Log($"{definition.Id}: {issue}", definition);
            }
            EditorUtility.DisplayDialog("Content Validation", $"Validated {all.Count} assets. Errors: {errors}. See Console for details.", "OK");
        }

        [MenuItem("Tools/Blueprint Civilizations/Create Prototype Sample Content")]
        public static void CreateSamples()
        {
            const string folder = "Assets/_Game/Content/Definitions/Prototype";
            EnsureFolder(folder);
            var hive = Create<RaceDefinition>($"{folder}/Race_Hive.asset", "race.hive", "Hive");
            Create<UnitDefinition>($"{folder}/Unit_WorkerLarva.asset", "unit.hive.worker_larva", "Worker Larva");
            Create<UnitDefinition>($"{folder}/Unit_Spider.asset", "unit.hive.spider", "Spider");
            Create<UnitDefinition>($"{folder}/Unit_ArmoredBeetle.asset", "unit.hive.armored_beetle", "Armored Beetle");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("Prototype sample assets created. Assign the Hive race and tune values in Content Studio.", hive);
        }
        private static T Create<T>(string path, string id, string displayName) where T : ContentDefinition
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path); if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>(); asset.EditorInitialize(id, displayName); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static void EnsureFolder(string path)
        {
            string current="Assets"; foreach(string part in path.Split('/').Skip(1)){string next=current+"/"+part;if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,part);current=next;}
        }
    }
}
