using System;
using System.Linq;
using BlueprintCivilizations.Blueprints;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.UI.Development;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.Editor.BlueprintBoard
{
    public static class BlueprintBoardSandboxSceneTools
    {
        public const string ScenePath = "Assets/_Game/Scenes/BlueprintBoardSandbox.unity";
        public const string PanelSettingsPath = "Assets/_Game/UI/Development/BlueprintBoardSandboxPanelSettings.asset";
        public const string LayoutPath = "Assets/_Game/UI/UXML/BlueprintBoardPanel.uxml";
        public const string StylePath = "Assets/_Game/UI/Styles/BlueprintBoardPanel.uss";
        public const string DetailsLayoutPath = "Assets/_Game/UI/UXML/BlueprintDetailsPanel.uxml";
        public const string DetailsStylePath = "Assets/_Game/UI/Styles/BlueprintDetailsPanel.uss";
        public const string CatalogPath = "Assets/_Game/Content/Assets/Configuration/GameContentCatalog.asset";
        public const string PlayerTestScenePath = "Assets/_Game/UI/Tests/PlayMode/BlueprintBoardPlayerTest.unity";
        public const string PlayerTestStorageKey = "tests.blueprint-board.player-runtime.v1";
        public const string RootName = "PrototypeBootstrap";

        private static readonly string[] RequiredDefinitionIds =
        {
            "HIVE_LARVA",
            "HIVE_SPIDER",
            "HIVE_BEETLE",
            "HIVE_STR_01"
        };

        public static SandboxAssetPaths ProductionPaths { get; } = new(
            ScenePath, PanelSettingsPath, LayoutPath, StylePath, DetailsLayoutPath, DetailsStylePath, CatalogPath,
            BlueprintBoardSandboxBootstrap.DefaultStorageKey);

        public static SandboxAssetPaths PlayerTestPaths { get; } = new(
            PlayerTestScenePath, PanelSettingsPath, LayoutPath, StylePath, DetailsLayoutPath, DetailsStylePath,
            CatalogPath, PlayerTestStorageKey);

        [MenuItem("Tools/Blueprint Civilizations/Create Blueprint Board Sandbox Scene")]
        public static void CreateBlueprintBoardSandboxScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Blueprint Board Sandbox creation cancelled; open scene changes were not saved.");
                return;
            }

            if (!TryLoadSandboxAssets(ProductionPaths, out var assets, out string error))
            {
                LogCreationFailure(error);
                return;
            }

            if (!TryCreateOrRepairScene(ProductionPaths.ScenePath, assets, ProductionPaths.StorageKey,
                    out var root, out error))
            {
                LogCreationFailure(error);
                return;
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            Debug.Log($"Blueprint Board development sandbox is ready at '{ProductionPaths.ScenePath}'.", root);
        }

        /// <summary>Creates the serialized runtime scene loaded by Editor and standalone Player tests.</summary>
        public static void CreateBlueprintBoardPlayerTestScene()
        {
            if (!TryLoadSandboxAssets(PlayerTestPaths, out var assets, out string error) ||
                !TryCreateOrRepairScene(PlayerTestPaths.ScenePath, assets, PlayerTestPaths.StorageKey,
                    out _, out error))
                throw new InvalidOperationException($"Blueprint Board Player test scene creation failed:\n{error}");
            Debug.Log($"Blueprint Board Player test scene is ready at '{PlayerTestPaths.ScenePath}'.");
        }

        /// <summary>Batch-safe repair entry point for the serialized development and Player-test scenes.</summary>
        public static void CreateSerializedRuntimeScenes()
        {
            CreateOrThrow(ProductionPaths, "development sandbox");
            CreateOrThrow(PlayerTestPaths, "Player test sandbox");
            AssetDatabase.SaveAssets();
        }

        private static void CreateOrThrow(SandboxAssetPaths paths, string purpose)
        {
            if (!TryLoadSandboxAssets(paths, out var assets, out string error) ||
                !TryCreateOrRepairScene(paths.ScenePath, assets, paths.StorageKey, out _, out error))
                throw new InvalidOperationException($"Could not create the Blueprint Board {purpose}:\n{error}");
        }

        public static bool TryLoadSandboxAssets(SandboxAssetPaths paths, out SandboxAssets assets, out string error)
        {
            assets = null;
            error = "";
            if (paths == null)
            {
                error = DependencyError("SandboxAssetPaths", "<not supplied>", "read sandbox asset paths",
                    "Pass a complete Blueprint Board sandbox path configuration.");
                return false;
            }

            // Load non-generated dependencies first so a missing source asset cannot leave a newly-created
            // PanelSettings asset behind as the apparent result of a failed scene operation.
            if (!TryLoadSourceDependencies(paths, out var layout, out var style, out var detailsLayout,
                    out var detailsStyle, out var catalog, out error)) return false;

            if (!TryLoadOrCreatePanelSettings(paths.PanelSettingsPath, out var panelSettings, out error)) return false;

            // PanelSettings creation performs a synchronous refresh. Reload every source dependency afterward
            // so scene composition never receives a stale Unity object from before that import boundary.
            if (!TryLoadSourceDependencies(paths, out layout, out style, out detailsLayout,
                    out detailsStyle, out catalog, out error)) return false;
            assets = new SandboxAssets(panelSettings, layout, style, detailsLayout, detailsStyle, catalog);
            return true;
        }

        public static bool TryLoadOrCreatePanelSettings(string path, out PanelSettings settings, out string error)
        {
            settings = null;
            error = "";
            if (!IsProjectAssetPath(path, ".asset"))
            {
                error = DependencyError(nameof(PanelSettings), path, "validate the configured asset path",
                    "Use an Assets/... path ending in .asset.");
                return false;
            }

            try
            {
                settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
                if (settings != null) return true;

                // An existing but not-yet-imported file must be synchronously reimported before deciding
                // whether it is missing or has the wrong main-object type.
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)) || AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
                    if (settings != null) return true;

                    string actualType = AssetDatabase.LoadMainAssetAtPath(path)?.GetType().FullName ?? "an unloadable asset";
                    error = DependencyError(nameof(PanelSettings), path,
                        $"reimport the existing asset, which resolved as {actualType}",
                        "Delete or move the invalid asset in Unity, then run the sandbox command again so a valid PanelSettings asset can be created.");
                    return false;
                }

                string folder = path.Substring(0, path.LastIndexOf('/'));
                if (!TryEnsureFolder(folder, out error)) return false;

                var created = ScriptableObject.CreateInstance<PanelSettings>();
                created.name = "BlueprintBoardSandboxPanelSettings";
                created.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                created.referenceResolution = new Vector2Int(1920, 1080);
                created.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                created.match = 0.5f;
                AssetDatabase.CreateAsset(created, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                // Never return the transient instance passed to CreateAsset. Unity can invalidate it while
                // importing; the persistent, reloaded main object is the only authoritative reference.
                settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
                if (settings != null) return true;

                error = DependencyError(nameof(PanelSettings), path,
                    "create, save, synchronously import, and reload the asset",
                    "Use Assets > Create > UI Toolkit > Panel Settings Asset at this exact path, then run the sandbox command again.");
                return false;
            }
            catch (Exception exception)
            {
                settings = null;
                error = DependencyError(nameof(PanelSettings), path,
                    $"load or create the asset ({exception.GetType().Name}: {exception.Message})",
                    "Confirm the path is writable, remove any wrong-type asset at that path, and retry from Unity.");
                return false;
            }
        }

        public static bool TryCreateOrRepairScene(string scenePath, SandboxAssets assets, string storageKey,
            out GameObject root, out string error)
        {
            root = null;
            error = "";
            if (!IsProjectAssetPath(scenePath, ".unity"))
            {
                error = DependencyError("SceneAsset", scenePath, "validate the configured sandbox scene path",
                    "Use an Assets/... path ending in .unity.");
                return false;
            }
            if (assets == null)
            {
                error = DependencyError("sandbox dependency set", scenePath, "validate scene prerequisites",
                    "Load PanelSettings, UXML, USS, GameContentCatalog, and prototype definitions before composing the scene.");
                return false;
            }

            // Opening a scene in Single mode can unload otherwise unreferenced assets. Capture stable paths
            // before the scene switch, then reload every dependency from AssetDatabase afterward.
            var persistentPaths = new SandboxAssetPaths(scenePath,
                assets.PanelSettingsPath, assets.LayoutPath, assets.StylePath, assets.DetailsLayoutPath,
                assets.DetailsStylePath, assets.CatalogPath, storageKey);
            if (!IsProjectAssetPath(persistentPaths.PanelSettingsPath, ".asset") ||
                !IsProjectAssetPath(persistentPaths.LayoutPath, ".uxml") ||
                !IsProjectAssetPath(persistentPaths.StylePath, ".uss") ||
                !IsProjectAssetPath(persistentPaths.DetailsLayoutPath, ".uxml") ||
                !IsProjectAssetPath(persistentPaths.DetailsStylePath, ".uss") ||
                !IsProjectAssetPath(persistentPaths.CatalogPath, ".asset"))
            {
                error = DependencyError("persistent sandbox dependencies", scenePath,
                    "capture AssetDatabase paths before opening the sandbox scene",
                    "Use imported project assets rather than transient ScriptableObjects when creating or saving the sandbox scene.");
                return false;
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                string folder = scenePath.Substring(0, scenePath.LastIndexOf('/'));
                if (!TryEnsureFolder(folder, out error)) return false;

                Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null
                    ? EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single)
                    : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                if (!TryLoadSandboxAssets(persistentPaths, out var reloadedAssets, out string reloadError))
                    throw new InvalidOperationException($"Sandbox dependencies could not be reloaded after opening the scene. {reloadError}");

                root = EnsureSceneComposition(scene, reloadedAssets.PanelSettings, reloadedAssets.Layout,
                    reloadedAssets.Style, reloadedAssets.DetailsLayout, reloadedAssets.DetailsStyle,
                    reloadedAssets.Catalog, storageKey);
                if (!TryValidateSceneComposition(scene, reloadedAssets, out error))
                    throw new InvalidOperationException(error);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                    throw new InvalidOperationException($"Unity returned false while saving SceneAsset '{scenePath}'.");

                AssetDatabase.SaveAssets();
                return true;
            }
            catch (Exception exception)
            {
                root = null;
                error = DependencyError("SceneAsset", scenePath,
                    $"compose, validate, and save the sandbox scene ({exception.GetType().Name}: {exception.Message})",
                    "Fix the reported dependency or filesystem problem, then run the scene command again. The scene is saved only after validation succeeds.");
                try { EditorSceneManager.RestoreSceneManagerSetup(previousSetup); }
                catch (Exception restoreException) { error += $"\nScene setup restore also failed: {restoreException.Message}"; }
                return false;
            }
        }

        public static GameObject EnsureSceneComposition(Scene scene, PanelSettings panelSettings,
            VisualTreeAsset layout, StyleSheet style, VisualTreeAsset detailsLayout, StyleSheet detailsStyle,
            GameContentCatalog catalog, string storageKey = BlueprintBoardSandboxBootstrap.DefaultStorageKey)
        {
            if (!scene.IsValid()) throw new ArgumentException("A valid scene is required.", nameof(scene));
            if (panelSettings == null) throw new ArgumentNullException(nameof(panelSettings));
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (style == null) throw new ArgumentNullException(nameof(style));
            if (detailsLayout == null) throw new ArgumentNullException(nameof(detailsLayout));
            if (detailsStyle == null) throw new ArgumentNullException(nameof(detailsStyle));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("A non-empty runtime storage key is required.", nameof(storageKey));

            GameObject[] namedRoots = scene.GetRootGameObjects().Where(candidate => candidate.name == RootName).ToArray();
            GameObject root = namedRoots.FirstOrDefault();
            if (root == null)
            {
                root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
            }
            foreach (GameObject duplicateRoot in namedRoots.Skip(1)) UnityEngine.Object.DestroyImmediate(duplicateRoot);

            UIDocument document = root.GetComponent<UIDocument>() ?? root.AddComponent<UIDocument>();
            BlueprintBoardSandboxBootstrap bootstrap = root.GetComponent<BlueprintBoardSandboxBootstrap>() ??
                                                         root.AddComponent<BlueprintBoardSandboxBootstrap>();

            foreach (UIDocument duplicate in SceneComponents<UIDocument>(scene).Where(candidate => candidate != document).ToArray())
                UnityEngine.Object.DestroyImmediate(duplicate);
            foreach (BlueprintBoardSandboxBootstrap duplicate in SceneComponents<BlueprintBoardSandboxBootstrap>(scene)
                         .Where(candidate => candidate != bootstrap).ToArray())
                UnityEngine.Object.DestroyImmediate(duplicate);

            document.panelSettings = panelSettings;
            document.visualTreeAsset = null; // The runtime factory owns cloning; a source asset here would render twice.
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.FindProperty("boardLayout").objectReferenceValue = layout;
            serialized.FindProperty("boardStyle").objectReferenceValue = style;
            serialized.FindProperty("detailsLayout").objectReferenceValue = detailsLayout;
            serialized.FindProperty("detailsStyle").objectReferenceValue = detailsStyle;
            serialized.FindProperty("capacity").intValue = 4;
            serialized.FindProperty("storageKey").stringValue = storageKey;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(document);
            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            return root;
        }

        private static bool TryValidateSceneComposition(Scene scene, SandboxAssets assets, out string error)
        {
            error = "";
            GameObject[] roots = scene.GetRootGameObjects().Where(candidate => candidate.name == RootName).ToArray();
            UIDocument[] documents = SceneComponents<UIDocument>(scene);
            BlueprintBoardSandboxBootstrap[] bootstraps = SceneComponents<BlueprintBoardSandboxBootstrap>(scene);
            if (roots.Length != 1 || documents.Length != 1 || bootstraps.Length != 1)
            {
                error = $"Scene validation expected exactly one {RootName}, UIDocument, and BlueprintBoardSandboxBootstrap " +
                        $"but found {roots.Length}, {documents.Length}, and {bootstraps.Length}.";
                return false;
            }
            if (documents[0].gameObject != roots[0] || bootstraps[0].gameObject != roots[0])
            {
                error = "The UIDocument and BlueprintBoardSandboxBootstrap must both be attached to PrototypeBootstrap.";
                return false;
            }
            if (documents[0].panelSettings != assets.PanelSettings)
            {
                error = $"UIDocument PanelSettings was not repaired to '{AssetDatabase.GetAssetPath(assets.PanelSettings)}'.";
                return false;
            }

            var serialized = new SerializedObject(bootstraps[0]);
            if (serialized.FindProperty("catalog").objectReferenceValue != assets.Catalog ||
                serialized.FindProperty("boardLayout").objectReferenceValue != assets.Layout ||
                serialized.FindProperty("boardStyle").objectReferenceValue != assets.Style ||
                serialized.FindProperty("detailsLayout").objectReferenceValue != assets.DetailsLayout ||
                serialized.FindProperty("detailsStyle").objectReferenceValue != assets.DetailsStyle)
            {
                error = "BlueprintBoardSandboxBootstrap catalog, Board/Details UXML, or Board/Details USS reference was not repaired.";
                return false;
            }
            return true;
        }

        private static T[] SceneComponents<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static bool TryLoadSourceDependencies(SandboxAssetPaths paths, out VisualTreeAsset layout,
            out StyleSheet style, out VisualTreeAsset detailsLayout, out StyleSheet detailsStyle,
            out GameContentCatalog catalog, out string error)
        {
            layout = null;
            style = null;
            detailsLayout = null;
            detailsStyle = null;
            catalog = null;
            if (!TryLoadRequiredAsset(paths.LayoutPath, "restore or reimport BlueprintBoardPanel.uxml", out layout, out error) ||
                !TryLoadRequiredAsset(paths.StylePath, "restore or reimport BlueprintBoardPanel.uss", out style, out error) ||
                !TryLoadRequiredAsset(paths.DetailsLayoutPath, "restore or reimport BlueprintDetailsPanel.uxml", out detailsLayout, out error) ||
                !TryLoadRequiredAsset(paths.DetailsStylePath, "restore or reimport BlueprintDetailsPanel.uss", out detailsStyle, out error) ||
                !TryLoadRequiredAsset(paths.CatalogPath, "rebuild the default content catalog from Content Studio", out catalog, out error))
                return false;

            var resolver = new ContentCatalogBlueprintDefinitionResolver(catalog);
            foreach (string definitionId in RequiredDefinitionIds)
            {
                if (resolver.TryResolve(definitionId, out _)) continue;
                error = DependencyError("Blueprint definition", paths.CatalogPath,
                    $"resolve required prototype definition '{definitionId}' from GameContentCatalog",
                    "Run Tools > Blueprint Civilizations > Rebuild Default Content Catalog and restore the missing unit or structure asset.");
                return false;
            }
            return true;
        }

        private static bool TryLoadRequiredAsset<T>(string path, string suggestedFix, out T asset, out string error)
            where T : UnityEngine.Object
        {
            asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                error = "";
                return true;
            }

            error = DependencyError(typeof(T).Name, path, "load the required existing asset", suggestedFix);
            return false;
        }

        private static bool TryEnsureFolder(string path, out string error)
        {
            error = "";
            if (AssetDatabase.IsValidFolder(path)) return true;
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.LastIndexOf('/') < 0)
            {
                error = DependencyError("folder", path, "create the required project folder",
                    "Use a folder path below Assets and create its parent folders in Unity.");
                return false;
            }

            string parent = path.Substring(0, path.LastIndexOf('/'));
            string name = path.Substring(path.LastIndexOf('/') + 1);
            if (!TryEnsureFolder(parent, out error)) return false;
            string guid = AssetDatabase.CreateFolder(parent, name);
            if (!string.IsNullOrWhiteSpace(guid)) return true;
            error = DependencyError("folder", path, "create the required project folder",
                "Create this folder manually in Unity and rerun the sandbox command.");
            return false;
        }

        private static bool IsProjectAssetPath(string path, string extension) =>
            !string.IsNullOrWhiteSpace(path) && path.StartsWith("Assets/", StringComparison.Ordinal) &&
            path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) && path.LastIndexOf('/') > 6;

        private static string DependencyError(string assetType, string path, string attemptedAction, string suggestedFix) =>
            $"Asset type: {assetType}\nExpected path: {path}\nAttempted action: {attemptedAction}.\nSuggested manual fix: {suggestedFix}";

        private static void LogCreationFailure(string details) =>
            Debug.LogError($"Blueprint Board Sandbox creation failed:\n{details}");

        public sealed class SandboxAssetPaths
        {
            public SandboxAssetPaths(string scenePath, string panelSettingsPath, string layoutPath,
                string stylePath, string detailsLayoutPath, string detailsStylePath, string catalogPath,
                string storageKey = BlueprintBoardSandboxBootstrap.DefaultStorageKey)
            {
                ScenePath = scenePath;
                PanelSettingsPath = panelSettingsPath;
                LayoutPath = layoutPath;
                StylePath = stylePath;
                DetailsLayoutPath = detailsLayoutPath;
                DetailsStylePath = detailsStylePath;
                CatalogPath = catalogPath;
                StorageKey = storageKey;
            }

            public string ScenePath { get; }
            public string PanelSettingsPath { get; }
            public string LayoutPath { get; }
            public string StylePath { get; }
            public string DetailsLayoutPath { get; }
            public string DetailsStylePath { get; }
            public string CatalogPath { get; }
            public string StorageKey { get; }
        }

        public sealed class SandboxAssets
        {
            public SandboxAssets(PanelSettings panelSettings, VisualTreeAsset layout, StyleSheet style,
                VisualTreeAsset detailsLayout, StyleSheet detailsStyle, GameContentCatalog catalog)
            {
                PanelSettings = panelSettings;
                Layout = layout;
                Style = style;
                DetailsLayout = detailsLayout;
                DetailsStyle = detailsStyle;
                Catalog = catalog;
                PanelSettingsPath = AssetDatabase.GetAssetPath(panelSettings);
                LayoutPath = AssetDatabase.GetAssetPath(layout);
                StylePath = AssetDatabase.GetAssetPath(style);
                DetailsLayoutPath = AssetDatabase.GetAssetPath(detailsLayout);
                DetailsStylePath = AssetDatabase.GetAssetPath(detailsStyle);
                CatalogPath = AssetDatabase.GetAssetPath(catalog);
            }

            public PanelSettings PanelSettings { get; }
            public VisualTreeAsset Layout { get; }
            public StyleSheet Style { get; }
            public VisualTreeAsset DetailsLayout { get; }
            public StyleSheet DetailsStyle { get; }
            public GameContentCatalog Catalog { get; }
            public string PanelSettingsPath { get; }
            public string LayoutPath { get; }
            public string StylePath { get; }
            public string DetailsLayoutPath { get; }
            public string DetailsStylePath { get; }
            public string CatalogPath { get; }
            public bool IsComplete => PanelSettings != null && Layout != null && Style != null &&
                                      DetailsLayout != null && DetailsStyle != null && Catalog != null;
        }
    }
}
