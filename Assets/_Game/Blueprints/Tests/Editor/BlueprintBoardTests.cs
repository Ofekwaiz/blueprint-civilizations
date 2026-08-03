using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.Core;
using BlueprintCivilizations.Editor.BlueprintBoard;
using BlueprintCivilizations.UI.Development;
using BlueprintCivilizations.UI.Presenters;
using BlueprintCivilizations.UI.ViewModels;
using BlueprintCivilizations.UI.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.Blueprints.Tests
{
    public sealed class BlueprintBoardTests
    {
        private const string LayoutPath = "Assets/_Game/UI/UXML/BlueprintBoardPanel.uxml";
        private const string StylePath = "Assets/_Game/UI/Styles/BlueprintBoardPanel.uss";
        private const string DetailsLayoutPath = "Assets/_Game/UI/UXML/BlueprintDetailsPanel.uxml";
        private const string DetailsStylePath = "Assets/_Game/UI/Styles/BlueprintDetailsPanel.uss";
        private const string CatalogPath = "Assets/_Game/Content/Assets/Configuration/GameContentCatalog.asset";

        [Test]
        public void ActivateBlueprint_WhenCapacityExists_MovesFromBenchToRequestedSlot()
        {
            var (board, service) = Create(3, "A", "B");
            var result = service.Execute(BlueprintCommands.ActivateBlueprint("A", 2));

            Assert.That(result.Success, Is.True);
            Assert.That(board.Slots[2].BlueprintDefinitionId, Is.EqualTo("A"));
            Assert.That(board.Bench.BlueprintDefinitionIds, Does.Not.Contain("A"));
            Assert.That(board.FindBlueprint("A").Location, Is.EqualTo(BlueprintLocationState.Active));
            Assert.That(board.FindBlueprint("A").BlueprintBoardIndex, Is.EqualTo(2));
            Assert.That(result.Events.Single().Type, Is.EqualTo(BlueprintEventType.Activated));
        }

        [Test]
        public void ActivateBlueprint_WhenCapacityIsFull_ReturnsFailureWithoutMutation()
        {
            var (board, service) = Create(1, "A", "B");
            Assert.That(service.Execute(BlueprintCommands.ActivateBlueprint("A", 0)).Success, Is.True);
            string before = BlueprintBoardSerializer.Serialize(board);

            var result = service.Execute(BlueprintCommands.ActivateBlueprint("B", 0));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Failure, Is.EqualTo(BlueprintCommandFailure.CapacityExceeded));
            Assert.That(BlueprintBoardSerializer.Serialize(board), Is.EqualTo(before));
        }

        [Test]
        public void ActivateBlueprint_WhenInsertionSlotIsOccupied_ShiftsTowardNearestEmptySlot()
        {
            var (board, service) = Create(4, "A", "B", "C");
            service.Execute(BlueprintCommands.ActivateBlueprint("A", 1));
            service.Execute(BlueprintCommands.ActivateBlueprint("B", 2));

            var result = service.Execute(BlueprintCommands.ActivateBlueprint("C", 1));

            Assert.That(result.Success, Is.True);
            CollectionAssert.AreEqual(new[] { "A", "C", "B", "" }, SlotIds(board));
        }

        [Test]
        public void BenchBlueprint_WhenActive_DeactivatesAndAppendsToBench()
        {
            var (board, service) = CreateActive(2, "A", "B");
            var result = service.Execute(BlueprintCommands.BenchBlueprint("A"));

            Assert.That(result.Success, Is.True);
            Assert.That(board.Slots[0].IsEmpty, Is.True);
            Assert.That(board.Bench.BlueprintDefinitionIds.Last(), Is.EqualTo("A"));
            Assert.That(board.FindBlueprint("A").Location, Is.EqualTo(BlueprintLocationState.Benched));
            Assert.That(board.FindBlueprint("A").BlueprintBoardIndex, Is.EqualTo(-1));
        }

        [Test]
        public void MoveBlueprint_WhenTargetIsEmpty_MovesWithoutChangingOrderOfOthers()
        {
            var (board, service) = Create(3, "A", "B");
            service.Execute(BlueprintCommands.ActivateBlueprint("A", 0));
            service.Execute(BlueprintCommands.ActivateBlueprint("B", 1));

            var result = service.Execute(BlueprintCommands.MoveBlueprint("A", 2));

            Assert.That(result.Success, Is.True);
            CollectionAssert.AreEqual(new[] { "", "B", "A" }, SlotIds(board));
        }

        [Test]
        public void MoveBlueprint_WhenBlueprintIsMissing_ReturnsFailureWithoutMutation()
        {
            var (board, service) = Create(2, "A");
            string before = BlueprintBoardSerializer.Serialize(board);

            var result = service.Execute(BlueprintCommands.MoveBlueprint("MISSING", 1));

            Assert.That(result.Failure, Is.EqualTo(BlueprintCommandFailure.MissingBlueprint));
            Assert.That(BlueprintBoardSerializer.Serialize(board), Is.EqualTo(before));
        }

        [Test]
        public void SwapBlueprints_WhenBothSlotsOccupied_SwapsAtomically()
        {
            var (board, service) = CreateActive(2, "A", "B");
            var result = service.Execute(BlueprintCommands.SwapBlueprints(0, 1));

            Assert.That(result.Success, Is.True);
            CollectionAssert.AreEqual(new[] { "B", "A" }, SlotIds(board));
            Assert.That(board.FindBlueprint("A").BlueprintBoardIndex, Is.EqualTo(1));
            Assert.That(board.FindBlueprint("B").BlueprintBoardIndex, Is.EqualTo(0));
        }

        [Test]
        public void SwapBlueprints_WhenOneSlotIsEmpty_ReturnsStructuredInvalidSwap()
        {
            var (board, service) = Create(2, "A");
            service.Execute(BlueprintCommands.ActivateBlueprint("A", 0));
            string before = BlueprintBoardSerializer.Serialize(board);

            var result = service.Execute(BlueprintCommands.SwapBlueprints(0, 1));

            Assert.That(result.Failure, Is.EqualTo(BlueprintCommandFailure.InvalidSwap));
            Assert.That(BlueprintBoardSerializer.Serialize(board), Is.EqualTo(before));
        }

        [Test]
        public void ReorderBlueprints_WhenMovedAcrossLine_ShiftsInterveningSlotsDeterministically()
        {
            var (board, service) = CreateActive(4, "A", "B", "C", "D");
            var result = service.Execute(BlueprintCommands.ReorderBlueprints("A", 3));

            Assert.That(result.Success, Is.True);
            CollectionAssert.AreEqual(new[] { "B", "C", "D", "A" }, SlotIds(board));
        }

        [Test]
        public void Capacity_WhenExpandedAddsSlots_AndUnsafeShrinkIsRejected()
        {
            var (board, service) = CreateActive(2, "A", "B");
            Assert.That(service.Execute(new SetBlueprintCapacityCommand(4)).Success, Is.True);
            Assert.That(board.Capacity, Is.EqualTo(4));
            Assert.That(board.Slots.Count, Is.EqualTo(4));

            var rejected = service.Execute(new SetBlueprintCapacityCommand(1));
            Assert.That(rejected.Failure, Is.EqualTo(BlueprintCommandFailure.CapacityExceeded));
            Assert.That(board.Capacity, Is.EqualTo(4));

            service.Execute(BlueprintCommands.BenchBlueprint("B"));
            Assert.That(service.Execute(new SetBlueprintCapacityCommand(1)).Success, Is.True);
            Assert.That(board.Slots.Count, Is.EqualTo(1));
        }

        [Test]
        public void Adjacency_QueriesImmediateAndDirectionalRelationships_InSlotOrder()
        {
            var (board, service) = Create(5, "A", "B", "C", "D");
            service.Execute(BlueprintCommands.ActivateBlueprint("A", 0));
            service.Execute(BlueprintCommands.ActivateBlueprint("B", 1));
            service.Execute(BlueprintCommands.ActivateBlueprint("C", 2));
            service.Execute(BlueprintCommands.ActivateBlueprint("D", 4));
            var adjacency = new BlueprintAdjacencyService();

            Assert.That(adjacency.GetLeftNeighbor(board, "B").DefinitionId, Is.EqualTo("A"));
            Assert.That(adjacency.GetRightNeighbor(board, "B").DefinitionId, Is.EqualTo("C"));
            Assert.That(adjacency.GetLeftNeighbor(board, "D"), Is.Null, "An empty slot breaks immediate adjacency.");
            CollectionAssert.AreEqual(new[] { "A", "B" }, adjacency.GetBlueprintsLeftOf(board, "C").Select(value => value.DefinitionId));
            CollectionAssert.AreEqual(new[] { "D" }, adjacency.GetBlueprintsRightOf(board, "C").Select(value => value.DefinitionId));
        }

        [Test]
        public void Adjacency_MatchingTagsRaceAndTier_UsesInjectedDefinitionMetadata()
        {
            var (board, service) = CreateActive(3, "A", "B", "C");
            var resolver = new FakeResolver(
                Meta("A", "HIVE", ContentTier.Tier1, "Organic", "Swarm"),
                Meta("B", "HIVE", ContentTier.Tier2, "Organic"),
                Meta("C", "HUMAN", ContentTier.Tier1, "Holy"));
            var adjacency = new BlueprintAdjacencyService(resolver);

            CollectionAssert.AreEqual(new[] { "A", "B" }, adjacency.GetMatchingTags(board, new[] { "organic" }).Select(value => value.DefinitionId));
            CollectionAssert.AreEqual(new[] { "A", "B" }, adjacency.GetMatchingRace(board, "hive").Select(value => value.DefinitionId));
            CollectionAssert.AreEqual(new[] { "A", "C" }, adjacency.GetMatchingTier(board, ContentTier.Tier1).Select(value => value.DefinitionId));
        }

        [Test]
        public void Validation_WhenStateIsCorrupted_ReturnsStructuredIssues()
        {
            var (board, service) = CreateActive(2, "A", "B");
            var slots = Private<List<BlueprintSlotState>>(board, "slots");
            PrivateSet(slots[1], "blueprintDefinitionId", "A");
            var bench = Private<List<string>>(board.Bench, "blueprintDefinitionIds");
            bench.Add("A");
            bench.Add("A");
            Private<List<BlueprintState>>(board, "blueprints").Add(null);
            PrivateSet(slots[0], "boardIndex", 9);
            PrivateSet(board, "capacity", 1);

            var codes = new BlueprintValidationService().Validate(board).Select(issue => issue.Code).ToArray();

            Assert.That(codes, Does.Contain(BlueprintValidationCode.DuplicateActiveBlueprint));
            Assert.That(codes, Does.Contain(BlueprintValidationCode.ActiveAndBenchDuplicate));
            Assert.That(codes, Does.Contain(BlueprintValidationCode.DuplicateBenchBlueprint));
            Assert.That(codes, Does.Contain(BlueprintValidationCode.NullBlueprint));
            Assert.That(codes, Does.Contain(BlueprintValidationCode.InvalidBoardIndex));
            Assert.That(codes, Does.Contain(BlueprintValidationCode.CapacityOverflow));
            Assert.That(codes.Count(code => code == BlueprintValidationCode.ActiveAndBenchDuplicate), Is.EqualTo(1));
        }

        [Test]
        public void Validation_WhenOccupiedCountIsBelowCapacity_DoesNotReportOverflow()
        {
            var (board, service) = Create(3, "A");
            service.Execute(BlueprintCommands.ActivateBlueprint("A", 0));

            Assert.That(ValidationCodes(board).Contains(BlueprintValidationCode.CapacityOverflow), Is.False);
        }

        [Test]
        public void Validation_WhenOccupiedCountEqualsCapacity_DoesNotReportOverflow()
        {
            var (board, _) = CreateActive(2, "A", "B");

            Assert.That(board.Slots.Count, Is.EqualTo(board.Capacity),
                "With one Blueprint ID per fixed slot, overflow is not representable while slot count equals capacity.");
            Assert.That(ValidationCodes(board).Contains(BlueprintValidationCode.CapacityOverflow), Is.False);
        }

        [Test]
        public void Validation_WhenOccupiedCountExceedsCapacity_ReportsOverflow()
        {
            var (board, _) = CreateActive(2, "A", "B");
            PrivateSet(board, "capacity", 1);

            Assert.That(ValidationCodes(board), Does.Contain(BlueprintValidationCode.CapacityOverflow));
        }

        [Test]
        public void Validation_WhenSlotListLengthMismatchesWithoutOccupiedOverflow_ReportsOnlyMismatch()
        {
            var (board, _) = Create(2, "A");
            Private<List<BlueprintSlotState>>(board, "slots").Add(new BlueprintSlotState());

            var codes = ValidationCodes(board);
            Assert.That(codes, Does.Contain(BlueprintValidationCode.CapacitySlotMismatch));
            Assert.That(codes.Contains(BlueprintValidationCode.CapacityOverflow), Is.False);
        }

        [Test]
        public void Validation_WhenOverflowAndSlotMismatchCoexist_ReportsBothDistinctIssues()
        {
            var (board, _) = CreateActive(2, "A", "B");
            PrivateSet(board, "capacity", 1);

            var codes = ValidationCodes(board);
            Assert.That(codes, Does.Contain(BlueprintValidationCode.CapacitySlotMismatch));
            Assert.That(codes, Does.Contain(BlueprintValidationCode.CapacityOverflow));
        }

        [Test]
        public void Validation_WhenPlacementReferencesUnknownBlueprint_ReportsBrokenReference()
        {
            var (board, service) = CreateActive(1, "A");
            PrivateSet(Private<List<BlueprintSlotState>>(board, "slots")[0], "blueprintDefinitionId", "MISSING");

            Assert.That(new BlueprintValidationService().Validate(board).Any(issue =>
                issue.Code == BlueprintValidationCode.BrokenReference && issue.BlueprintId == "MISSING"), Is.True);
        }

        [Test]
        public void Serialization_RoundTripPreservesOrderingAndProducesStableJson()
        {
            var (board, service) = Create(3, "A", "B", "C");
            service.Execute(BlueprintCommands.ActivateBlueprint("B", 1));
            board.FindBlueprint("B").AssignedLane = BlueprintLane.Right;
            string json = BlueprintBoardSerializer.Serialize(board);

            var loaded = BlueprintBoardSerializer.TryDeserialize(json);

            Assert.That(loaded.Success, Is.True, loaded.Error);
            Assert.That(BlueprintBoardSerializer.Serialize(loaded.Board), Is.EqualTo(json));
            Assert.That(loaded.Board.FindBlueprint("B").AssignedLane, Is.EqualTo(BlueprintLane.Right));
            CollectionAssert.AreEqual(SlotIds(board), SlotIds(loaded.Board));
        }

        [Test]
        public void Persistence_SaveThenLoad_UsesStableSerializedState()
        {
            var (board, service) = CreateActive(2, "A", "B");
            var storage = new MemoryStorage();
            var persistence = new BlueprintBoardPersistenceService(storage);
            persistence.Save("player.board", board);

            var loaded = persistence.Load("player.board");

            Assert.That(storage.Flushed, Is.True);
            Assert.That(loaded.Success, Is.True);
            CollectionAssert.AreEqual(new[] { "A", "B" }, SlotIds(loaded.Board));
        }

        [Test]
        public void Persistence_AutoSaveBindingPersistsSuccessfulCommandsAndUndo()
        {
            var (board, service) = Create(1, "A");
            var storage = new MemoryStorage();
            var persistence = new BlueprintBoardPersistenceService(storage);
            using (persistence.BindAutoSave("player.auto-board", service))
            {
                service.Execute(BlueprintCommands.ActivateBlueprint("A", 0));
                Assert.That(persistence.Load("player.auto-board").Board.FindActiveIndex("A"), Is.EqualTo(0));
                service.Undo();
                Assert.That(persistence.Load("player.auto-board").Board.FindActiveIndex("A"), Is.EqualTo(-1));
            }
        }

        [Test]
        public void UndoRedo_RestoresSnapshotsAndKeepsRevisionMonotonic()
        {
            var (board, service) = Create(2, "A");
            service.Execute(BlueprintCommands.ActivateBlueprint("A", 0));
            int afterActivate = board.Revision;

            Assert.That(service.Undo().Success, Is.True);
            Assert.That(board.FindActiveIndex("A"), Is.EqualTo(-1));
            Assert.That(board.Revision, Is.GreaterThan(afterActivate));
            int afterUndo = board.Revision;

            Assert.That(service.Redo().Success, Is.True);
            Assert.That(board.FindActiveIndex("A"), Is.EqualTo(0));
            Assert.That(board.Revision, Is.GreaterThan(afterUndo));
        }

        [Test]
        public void RuntimeAssemblies_DoNotReferenceUnityEditor()
        {
            Assert.That(typeof(BlueprintPlacementService).Assembly.GetReferencedAssemblies().Any(name => name.Name.StartsWith("UnityEditor", StringComparison.Ordinal)), Is.False);
            Assert.That(typeof(BlueprintBoardView).Assembly.GetReferencedAssemblies().Any(name => name.Name.StartsWith("UnityEditor", StringComparison.Ordinal)), Is.False);
        }

        [Test]
        public void BlueprintBoardAssets_LoadAndViewRendersHorizontalCapacitySlots()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            Assert.That(layout, Is.Not.Null);
            Assert.That(style, Is.Not.Null);
            var root = layout.CloneTree();
            var view = new BlueprintBoardView(root);
            view.Render(new BlueprintBoardViewModel(3, 0,
                new[] { new BlueprintSlotViewModel(0, null), new BlueprintSlotViewModel(1, null), new BlueprintSlotViewModel(2, null) },
                Array.Empty<BlueprintCardViewModel>(), false, false));

            Assert.That(root.Q("blueprint-active-row").Query(className: "blueprint-slot").ToList().Count, Is.EqualTo(3));
            Assert.That(root.Q("blueprint-bench-row"), Is.Not.Null);
        }

        [Test]
        public void BlueprintBoardPanelFactory_WithProjectAssets_ConstructsRuntimePanel()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            var detailsLayout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DetailsLayoutPath);
            var detailsStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(DetailsStylePath);
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
            var host = new VisualElement();
            using var presenter = BlueprintBoardPanelFactory.Attach(host, layout, style, detailsLayout, detailsStyle,
                BlueprintBoardSandboxBootstrap.CreateInitialBoardState(), catalog);

            Assert.That(host.Q("blueprint-board-panel"), Is.Not.Null);
            Assert.That(presenter.Placement.State.ActiveCount, Is.EqualTo(2));
            Assert.That(presenter.Placement.State.Bench.Count, Is.EqualTo(2));
        }

        [Test]
        public void BlueprintBoardSandboxInitialState_HasTwoActiveAndTwoBenchedBlueprints()
        {
            var board = BlueprintBoardSandboxBootstrap.CreateInitialBoardState();

            Assert.That(board.Capacity, Is.EqualTo(4));
            CollectionAssert.AreEqual(new[] { "HIVE_LARVA", "HIVE_SPIDER", "", "" }, SlotIds(board));
            CollectionAssert.AreEqual(new[] { "HIVE_BEETLE", "HIVE_STR_01" }, board.Bench.BlueprintDefinitionIds);
        }

        [Test]
        public void BlueprintBoardSandboxSceneComposition_WhenRunTwice_DoesNotDuplicateBootstrapOrDocument()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            var detailsLayout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DetailsLayoutPath);
            var detailsStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(DetailsStylePath);
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject first = BlueprintBoardSandboxSceneTools.EnsureSceneComposition(scene, panelSettings, layout,
                    style, detailsLayout, detailsStyle, catalog);
                GameObject second = BlueprintBoardSandboxSceneTools.EnsureSceneComposition(scene, panelSettings, layout,
                    style, detailsLayout, detailsStyle, catalog);

                Assert.That(second, Is.SameAs(first));
                Assert.That(scene.GetRootGameObjects().Count(root => root.name == "PrototypeBootstrap"), Is.EqualTo(1));
                Assert.That(SceneComponents<UIDocument>(scene).Length, Is.EqualTo(1));
                Assert.That(SceneComponents<BlueprintBoardSandboxBootstrap>(scene).Length, Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(panelSettings);
            }
        }

        [Test]
        public void BlueprintBoardSandboxPanelSettings_WhenMissing_CreatesSavesAndReloadsPersistentAsset()
        {
            string folder = TemporarySandboxFolder();
            string path = $"{folder}/SandboxPanelSettings.asset";
            try
            {
                bool success = BlueprintBoardSandboxSceneTools.TryLoadOrCreatePanelSettings(path,
                    out var settings, out string error);

                Assert.That(success, Is.True, error);
                Assert.That(settings, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(settings), Is.EqualTo(path));
                Assert.That(AssetDatabase.LoadAssetAtPath<PanelSettings>(path), Is.Not.Null);
            }
            finally { DeleteTemporarySandboxFolder(folder); }
        }

        [Test]
        public void BlueprintBoardSandboxPanelSettings_WhenLoadedAgain_ReusesSingleAsset()
        {
            string folder = TemporarySandboxFolder();
            string path = $"{folder}/SandboxPanelSettings.asset";
            try
            {
                Assert.That(BlueprintBoardSandboxSceneTools.TryLoadOrCreatePanelSettings(path,
                    out var first, out string firstError), Is.True, firstError);
                string firstGuid = AssetDatabase.AssetPathToGUID(path);

                Assert.That(BlueprintBoardSandboxSceneTools.TryLoadOrCreatePanelSettings(path,
                    out var second, out string secondError), Is.True, secondError);

                Assert.That(second, Is.EqualTo(first));
                Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(firstGuid));
                Assert.That(AssetDatabase.FindAssets("t:PanelSettings", new[] { folder }).Length, Is.EqualTo(1));
            }
            finally { DeleteTemporarySandboxFolder(folder); }
        }

        [Test]
        public void BlueprintBoardSandboxSceneComposition_WhenUidocumentIsMissing_AddsRequiredComponent()
        {
            var assets = LoadTransientSandboxAssets();
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject(BlueprintBoardSandboxSceneTools.RootName);
                SceneManager.MoveGameObjectToScene(root, scene);

                GameObject repaired = BlueprintBoardSandboxSceneTools.EnsureSceneComposition(scene,
                    assets.PanelSettings, assets.Layout, assets.Style, assets.DetailsLayout, assets.DetailsStyle,
                    assets.Catalog);

                Assert.That(repaired, Is.SameAs(root));
                Assert.That(repaired.GetComponents<UIDocument>().Length, Is.EqualTo(1));
                Assert.That(repaired.GetComponent<UIDocument>().panelSettings, Is.EqualTo(assets.PanelSettings));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(assets.PanelSettings);
            }
        }

        [Test]
        public void BlueprintBoardSandboxSceneComposition_WhenPanelReferenceIsMissing_RepairsReference()
        {
            var assets = LoadTransientSandboxAssets();
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject(BlueprintBoardSandboxSceneTools.RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                var document = root.AddComponent<UIDocument>();
                document.panelSettings = null;

                BlueprintBoardSandboxSceneTools.EnsureSceneComposition(scene,
                    assets.PanelSettings, assets.Layout, assets.Style, assets.DetailsLayout, assets.DetailsStyle,
                    assets.Catalog);

                Assert.That(document.panelSettings, Is.EqualTo(assets.PanelSettings));
                Assert.That(SceneComponents<UIDocument>(scene).Length, Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(assets.PanelSettings);
            }
        }

        [Test]
        public void BlueprintBoardSandboxDependencies_WhenUxmlIsMissing_FailsBeforeCreatingPanelSettings()
        {
            string folder = TemporarySandboxFolder();
            var paths = TemporarySandboxPaths(folder, $"{folder}/MissingBlueprintBoardPanel.uxml");
            try
            {
                bool success = BlueprintBoardSandboxSceneTools.TryLoadSandboxAssets(paths,
                    out var assets, out string error);

                Assert.That(success, Is.False);
                Assert.That(assets, Is.Null);
                Assert.That(error, Does.Contain("Asset type: VisualTreeAsset"));
                Assert.That(error, Does.Contain(paths.LayoutPath));
                Assert.That(error, Does.Contain("Suggested manual fix:"));
                Assert.That(AssetDatabase.LoadMainAssetAtPath(paths.PanelSettingsPath), Is.Null,
                    "PanelSettings must not be created when a required source asset is missing.");
            }
            finally { DeleteTemporarySandboxFolder(folder); }
        }

        [Test]
        public void BlueprintBoardSandboxSceneCreation_WhenRunTwice_ReusesAssetsAndRepairsSingleScene()
        {
            string folder = TemporarySandboxFolder();
            var paths = TemporarySandboxPaths(folder, LayoutPath);
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Assert.That(BlueprintBoardSandboxSceneTools.TryLoadSandboxAssets(paths,
                    out var assets, out string loadError), Is.True, loadError);
                string panelGuid = AssetDatabase.AssetPathToGUID(paths.PanelSettingsPath);
                Assert.That(BlueprintBoardSandboxSceneTools.TryCreateOrRepairScene(paths.ScenePath, assets,
                    paths.StorageKey, out _, out string firstError), Is.True, firstError);

                Assert.That(BlueprintBoardSandboxSceneTools.TryCreateOrRepairScene(paths.ScenePath, assets,
                    paths.StorageKey, out var root, out string secondError), Is.True, secondError);

                Scene scene = root.scene;
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(paths.ScenePath), Is.Not.Null);
                Assert.That(AssetDatabase.AssetPathToGUID(paths.PanelSettingsPath), Is.EqualTo(panelGuid));
                Assert.That(AssetDatabase.FindAssets("t:PanelSettings", new[] { folder }).Length, Is.EqualTo(1));
                Assert.That(scene.GetRootGameObjects().Count(candidate => candidate.name == BlueprintBoardSandboxSceneTools.RootName), Is.EqualTo(1));
                Assert.That(SceneComponents<UIDocument>(scene).Length, Is.EqualTo(1));
                Assert.That(SceneComponents<BlueprintBoardSandboxBootstrap>(scene).Length, Is.EqualTo(1));
            }
            finally
            {
                try { RestoreSceneSetupOrCreateCleanScene(previousSetup); }
                finally { DeleteTemporarySandboxFolder(folder); }
            }
        }

        private static (BlueprintBoardState Board, BlueprintPlacementService Service) Create(int capacity, params string[] ids)
        {
            var board = new BlueprintBoardState("PLAYER", capacity, ids.Select(id => new UnitBlueprintState(id, "PLAYER")));
            return (board, new BlueprintPlacementService(board));
        }

        private static (BlueprintBoardState Board, BlueprintPlacementService Service) CreateActive(int capacity, params string[] ids)
        {
            var tuple = Create(capacity, ids);
            for (int index = 0; index < ids.Length; index++) Assert.That(tuple.Service.Execute(BlueprintCommands.ActivateBlueprint(ids[index], index)).Success, Is.True);
            return tuple;
        }

        private static string[] SlotIds(BlueprintBoardState board) => board.Slots.Select(slot => slot.BlueprintDefinitionId).ToArray();
        private static BlueprintValidationCode[] ValidationCodes(BlueprintBoardState board) =>
            new BlueprintValidationService().Validate(board).Select(issue => issue.Code).ToArray();
        private static BlueprintDefinitionMetadata Meta(string id, string race, ContentTier tier, params string[] tags) => new(id, race, tier, tags);

        private static BlueprintBoardSandboxSceneTools.SandboxAssets LoadTransientSandboxAssets() => new(
            ScriptableObject.CreateInstance<PanelSettings>(),
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath),
            AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath),
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DetailsLayoutPath),
            AssetDatabase.LoadAssetAtPath<StyleSheet>(DetailsStylePath),
            AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath));

        private static BlueprintBoardSandboxSceneTools.SandboxAssetPaths TemporarySandboxPaths(string folder, string layoutPath) => new(
            $"{folder}/BlueprintBoardSandbox.unity",
            $"{folder}/BlueprintBoardSandboxPanelSettings.asset",
            layoutPath,
            StylePath,
            DetailsLayoutPath,
            DetailsStylePath,
            CatalogPath,
            $"tests.blueprint-board.sandbox.{Guid.NewGuid():N}");

        private static string TemporarySandboxFolder() =>
            $"Assets/_Game/UI/Development/__BlueprintBoardSandboxTests_{Guid.NewGuid():N}";

        private static void DeleteTemporarySandboxFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void RestoreSceneSetupOrCreateCleanScene(SceneSetup[] setup)
        {
            bool canRestore = setup != null && setup.Count(entry => entry.isLoaded) > 0 &&
                              setup.Count(entry => entry.isActive) == 1 &&
                              setup.Where(entry => entry.isLoaded).All(entry =>
                                  !string.IsNullOrWhiteSpace(entry.path) &&
                                  AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.path) != null);
            if (canRestore)
            {
                try
                {
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                    return;
                }
                catch (ArgumentException)
                {
                    // Fall through to a deterministic clean scene if an external asset change invalidated setup.
                }
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static T[] SceneComponents<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static T Private<T>(object target, string fieldName) => (T)target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void PrivateSet(object target, string fieldName, object value) => target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class FakeResolver : IBlueprintDefinitionResolver
        {
            private readonly Dictionary<string, BlueprintDefinitionMetadata> values;
            public FakeResolver(params BlueprintDefinitionMetadata[] values) => this.values = values.ToDictionary(value => value.DefinitionId, StringComparer.OrdinalIgnoreCase);
            public bool TryResolve(string definitionId, out BlueprintDefinitionMetadata metadata) => values.TryGetValue(definitionId, out metadata);
        }

        private sealed class MemoryStorage : IBlueprintBoardStorage
        {
            private readonly Dictionary<string, string> values = new();
            public bool Flushed { get; private set; }
            public bool TryRead(string key, out string json) => values.TryGetValue(key, out json);
            public void Write(string key, string json) => values[key] = json;
            public void Flush() => Flushed = true;
        }
    }
}
