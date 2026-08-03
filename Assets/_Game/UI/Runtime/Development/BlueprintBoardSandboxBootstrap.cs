using System;
using System.Linq;
using BlueprintCivilizations.Blueprints;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.UI.Presenters;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.UI.Development
{
    /// <summary>
    /// Development-only composition root for visually exercising Milestone 1. It owns no board rules;
    /// all mutations still flow through BlueprintPlacementService and BlueprintBoardPresenter.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class BlueprintBoardSandboxBootstrap : MonoBehaviour
    {
        public const string DefaultStorageKey = "development.blueprint-board-sandbox.v1";

        [SerializeField] private GameContentCatalog catalog = null;
        [SerializeField] private VisualTreeAsset boardLayout = null;
        [SerializeField] private StyleSheet boardStyle = null;
        [SerializeField] private VisualTreeAsset detailsLayout = null;
        [SerializeField] private StyleSheet detailsStyle = null;
        [Min(4)] [SerializeField] private int capacity = 4;
        [SerializeField] private string storageKey = DefaultStorageKey;
        [SerializeField] private bool logInteractionDiagnostics;

        private BlueprintBoardPanelController panel;
        private IDisposable autoSave;
        private VisualElement sandboxHost;

        public BlueprintBoardState State => panel?.Placement.State;

        public static BlueprintBoardState CreateInitialBoardState(int configuredCapacity = 4)
        {
            if (configuredCapacity < 4) throw new ArgumentOutOfRangeException(nameof(configuredCapacity), "Sandbox capacity must be at least four.");
            const string owner = "DEVELOPMENT_SANDBOX_PLAYER";
            var board = new BlueprintBoardState(owner, configuredCapacity, new BlueprintState[]
            {
                new UnitBlueprintState("HIVE_LARVA", owner),
                new UnitBlueprintState("HIVE_SPIDER", owner),
                new UnitBlueprintState("HIVE_BEETLE", owner),
                new BlueprintState("HIVE_STR_01", owner)
            });
            var placement = new BlueprintPlacementService(board);
            if (!placement.Execute(BlueprintCommands.ActivateBlueprint("HIVE_LARVA", 0)).Success ||
                !placement.Execute(BlueprintCommands.ActivateBlueprint("HIVE_SPIDER", 1)).Success)
                throw new InvalidOperationException("Could not create the Blueprint Board sandbox's initial active line.");
            return board;
        }

        [ContextMenu("Reset Blueprint Board Sandbox Save")]
        public void ResetSavedBoard()
        {
            if (!string.IsNullOrWhiteSpace(storageKey)) PlayerPrefs.DeleteKey(storageKey);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (panel != null) return;
            if (catalog == null || boardLayout == null || boardStyle == null || detailsLayout == null || detailsStyle == null)
            {
                Debug.LogError("Blueprint Board sandbox is missing its catalog, Board/Details UXML, or Board/Details USS reference. " +
                               "Re-run the sandbox scene creation command.", this);
                enabled = false;
                return;
            }

            var definitionResolver = new ContentCatalogBlueprintDefinitionResolver(catalog);
            var validation = new BlueprintValidationService(definitionResolver);
            var persistence = new BlueprintBoardPersistenceService(new PlayerPrefsBlueprintBoardStorage());
            var loaded = persistence.Load(storageKey);
            BlueprintBoardState state = loaded.Success && validation.IsValid(loaded.Board)
                ? loaded.Board
                : CreateInitialBoardState(capacity);
            if (loaded.Success && state != loaded.Board)
                Debug.LogWarning("The saved Blueprint Board sandbox state was invalid and has been replaced with the development default.", this);

            sandboxHost = new VisualElement { name = "blueprint-board-sandbox-host" };
            sandboxHost.style.flexGrow = 1;
            GetComponent<UIDocument>().rootVisualElement.Add(sandboxHost);
            panel = BlueprintBoardPanelFactory.Attach(sandboxHost, boardLayout, boardStyle, detailsLayout, detailsStyle,
                state, catalog, logInteractionDiagnostics);
            autoSave = persistence.BindAutoSave(storageKey, panel.Placement);
        }

        private void OnDisable()
        {
            autoSave?.Dispose();
            autoSave = null;
            panel?.Dispose();
            panel = null;
            sandboxHost?.RemoveFromHierarchy();
            sandboxHost = null;
        }
    }
}
