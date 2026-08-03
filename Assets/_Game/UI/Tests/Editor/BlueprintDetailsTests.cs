using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueprintCivilizations.Blueprints;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.UI.Presenters;
using BlueprintCivilizations.UI.ViewModels;
using BlueprintCivilizations.UI.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BlueprintCivilizations.UI.Tests
{
    public sealed class BlueprintDetailsTests
    {
        private const string CatalogPath = "Assets/_Game/Content/Assets/Configuration/GameContentCatalog.asset";

        [Test]
        public void Resolver_ActiveUnitUsesImmutableIdAndAuthoredIdentity()
        {
            using var fixture = CreateFixture();
            BlueprintDetailsViewModel model = fixture.Resolve("HIVE_LARVA");

            Assert.That(model.DefinitionId, Is.EqualTo("HIVE_LARVA"));
            Assert.That(model.DisplayName, Is.EqualTo("Larva Brood"));
            Assert.That(Value(model, "Identity", "Content type"), Is.EqualTo("Unit Blueprint"));
            Assert.That(Value(model, "Identity", "Race"), Is.EqualTo("Hive"));
            Assert.That(Value(model, "Identity", "Board state"), Is.EqualTo("Active"));
        }

        [Test]
        public void Resolver_BenchedUnitReportsBenchLocation()
        {
            using var fixture = CreateFixture();
            BlueprintDetailsViewModel model = fixture.Resolve("HIVE_BEETLE");

            Assert.That(Value(model, "Identity", "Board state"), Is.EqualTo("Benched"));
            Assert.That(Value(model, "Board assignment", "Location"), Is.EqualTo("Bench"));
            Assert.That(Value(model, "Board assignment", "Active slot index"), Is.EqualTo("Bench"));
        }

        [Test]
        public void Resolver_UnitProductionValuesExposeBaseAndCurrent()
        {
            using var fixture = CreateFixture();
            BlueprintDetailsViewModel model = fixture.Resolve("HIVE_LARVA");

            AssertStat(model, "Production", "Spawn interval", "6", "6", "seconds");
            AssertStat(model, "Production", "Spawn batch size", "1", "1", "");
            AssertStat(model, "Production", "Maximum population", "6", "6", "");
            AssertStat(model, "Production", "Initial spawn delay", "0", "0", "seconds");
            AssertStat(model, "Production", "Production priority", "0", "0", "");
        }

        [Test]
        public void Resolver_UnitCombatValuesExposeCompleteAuthoredProfile()
        {
            using var fixture = CreateFixture();
            BlueprintDetailsViewModel model = fixture.Resolve("HIVE_SPIDER");

            AssertStat(model, "Combat", "Maximum health", "95", "95", "");
            AssertStat(model, "Combat", "Attack damage", "12", "12", "");
            AssertStat(model, "Combat", "Attack range", "4", "4", "tiles");
            Assert.That(Value(model, "Combat", "Targeting profile"), Does.Contain("Nearest"));
            Assert.That(Value(model, "Combat", "Movement profile"), Is.EqualTo("Ground"));
            Assert.That(Value(model, "Combat", "Lane compatibility"), Is.EqualTo("Any lane"));
        }

        [Test]
        public void Resolver_ActiveAssignmentIncludesSlotAndBothNeighbors()
        {
            using var fixture = CreateFixture();
            fixture.Service.Execute(BlueprintCommands.ActivateBlueprint("HIVE_BEETLE", 2));
            BlueprintDetailsViewModel model = fixture.Resolve("HIVE_SPIDER");

            Assert.That(Value(model, "Board assignment", "Active slot index"), Is.EqualTo("1"));
            Assert.That(Value(model, "Board assignment", "Left neighbor"), Is.EqualTo("Larva Brood"));
            Assert.That(Value(model, "Board assignment", "Right neighbor"), Is.EqualTo("Shell Beetle"));
        }

        [Test]
        public void Resolver_DefaultProgressionIsAccurateAndMilestonesRemainLocked()
        {
            using var fixture = CreateFixture();
            BlueprintDetailsViewModel model = fixture.Resolve("HIVE_LARVA");

            Assert.That(Value(model, "Progression preview", "Copies owned"), Is.EqualTo("1"));
            Assert.That(Value(model, "Progression preview", "Ascension level"), Is.EqualTo("Base (0)"));
            Assert.That(Value(model, "Progression preview", "Selected refinements"), Is.EqualTo("Not yet acquired"));
            Assert.That(Value(model, "Progression preview", "Socket count"), Is.EqualTo("0"));
            Assert.That(Value(model, "Progression preview", "Attached research"), Is.EqualTo("Not yet acquired"));
            Assert.That(Value(model, "Progression preview", "Selected evolution"), Is.EqualTo("Not yet acquired"));
            CollectionAssert.AreEquivalent(new[] { "Copy 1 milestone", "Copy 4 milestone", "Copy 5 milestone", "Copy 9 milestone", "Copy 10 milestone" },
                Section(model, "Progression preview").Values.Where(value => value.Label.Contains("milestone"))
                    .Select(value => value.Label));
        }

        [Test]
        public void Presenter_ClearingSelectionReturnsEmptyState()
        {
            using var fixture = CreateFixture();
            fixture.BoardView.RequestSelection("HIVE_LARVA");
            fixture.BoardView.RequestSelection("");

            Assert.That(fixture.DetailsView.LastModel.IsEmpty, Is.True);
            Assert.That(fixture.DetailsView.LastModel.EmptyMessage,
                Is.EqualTo(BlueprintDetailsViewModel.DefaultEmptyMessage));
        }

        [Test]
        public void Presenter_SelectedBlueprintMovementPreservesSelectionAndUpdatesLocation()
        {
            using var fixture = CreateFixture();
            fixture.BoardView.RequestSelection("HIVE_LARVA");

            Assert.That(fixture.Service.Execute(BlueprintCommands.MoveBlueprint("HIVE_LARVA", 2)).Success, Is.True);

            Assert.That(fixture.BoardPresenter.SelectedBlueprintId, Is.EqualTo("HIVE_LARVA"));
            Assert.That(Value(fixture.DetailsView.LastModel, "Board assignment", "Active slot index"), Is.EqualTo("2"));
        }

        [Test]
        public void Presenter_RemovingSelectedBlueprintClearsDetails()
        {
            using var fixture = CreateFixture();
            fixture.BoardView.RequestSelection("HIVE_LARVA");
            OwnedBlueprints(fixture.Board).Remove(fixture.Board.FindBlueprint("HIVE_LARVA"));

            fixture.BoardPresenter.Refresh();

            Assert.That(fixture.BoardPresenter.SelectedBlueprintId, Is.Empty);
            Assert.That(fixture.DetailsView.LastModel.IsEmpty, Is.True);
        }

        [Test]
        public void Resolver_NeutralIdentityDoesNotFabricateRace()
        {
            var neutral = ScriptableObject.CreateInstance<UnitDefinition>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            try
            {
                neutral.EditorInitialize("TEST_NEUTRAL", "Test Neutral");
                SetPrivate(neutral, "isNeutral", true);
                catalog.EditorSetDefinitions(new ContentDefinition[] { neutral });
                var board = new BlueprintBoardState("PLAYER", 1,
                    new[] { new UnitBlueprintState("TEST_NEUTRAL", "PLAYER") });
                var resolver = new ContentCatalogBlueprintDetailsResolver(catalog,
                    new BlueprintAdjacencyService(new ContentCatalogBlueprintDefinitionResolver(catalog)), false);

                BlueprintDetailsViewModel model = resolver.Resolve(board.FindBlueprint("TEST_NEUTRAL"), board);

                Assert.That(Value(model, "Identity", "Race"), Is.EqualTo("Neutral"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(neutral);
            }
        }

        [Test]
        public void Resolver_StructureShowsOnlyRelevantCombatAndSupportDetails()
        {
            using var fixture = CreateFixture();
            BlueprintDetailsViewModel model = fixture.Resolve("HIVE_STR_01");

            Assert.That(Value(model, "Identity", "Content type"), Is.EqualTo("Structure Blueprint"));
            Assert.That(Value(model, "Combat", "Attack behavior"), Does.Contain("Does not attack"));
            Assert.That(Value(model, "Combat", "Movement profile"), Is.EqualTo("Stationary structure"));
            Assert.That(Value(model, "Combat", "Support and adjacency"), Does.Contain("Adjacent Organic"));
            AssertStat(model, "Combat", "Maximum health", "120", "120", "");
        }

        [Test]
        public void Resolver_ShopInformationUsesAuthoredPlanningValuesOnly()
        {
            using var fixture = CreateFixture();
            BlueprintDetailsViewModel model = fixture.Resolve("HIVE_BEETLE");

            Assert.That(Value(model, "Shop information", "Gold cost"), Is.EqualTo("2"));
            Assert.That(Value(model, "Shop information", "Pool kind"), Is.EqualTo("Private race pool"));
            Assert.That(Value(model, "Shop information", "Base pool size"), Is.EqualTo("18"));
            Assert.That(Value(model, "Shop information", "Shop tier"), Is.EqualTo("Tier 1"));
            Assert.That(Value(model, "Shop information", "Live pool count"), Does.Contain("not implemented").IgnoreCase);
        }

        [Test]
        public void Resolver_MissingDefinitionProducesActionableStableIdDiagnostic()
        {
            using var fixture = CreateFixture();
            var missing = new UnitBlueprintState("MISSING_BLUEPRINT", fixture.Board.OwnerId);

            BlueprintDetailsViewModel model = fixture.Resolver.Resolve(missing, fixture.Board);

            Assert.That(model.Diagnostic, Does.Contain("MISSING_BLUEPRINT"));
            Assert.That(model.Diagnostic, Does.Contain("stable ID"));
            Assert.That(model.Diagnostic, Does.Contain("catalog"));
        }

        private static Fixture CreateFixture()
        {
            GameContentCatalog catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            const string owner = "DETAILS_TEST_PLAYER";
            var board = new BlueprintBoardState(owner, 4, new BlueprintState[]
            {
                new UnitBlueprintState("HIVE_LARVA", owner),
                new UnitBlueprintState("HIVE_SPIDER", owner),
                new UnitBlueprintState("HIVE_BEETLE", owner),
                new BlueprintState("HIVE_STR_01", owner)
            });
            var metadata = new ContentCatalogBlueprintDefinitionResolver(catalog);
            var service = new BlueprintPlacementService(board, new BlueprintValidationService(metadata));
            Assert.That(service.Execute(BlueprintCommands.ActivateBlueprint("HIVE_LARVA", 0)).Success, Is.True);
            Assert.That(service.Execute(BlueprintCommands.ActivateBlueprint("HIVE_SPIDER", 1)).Success, Is.True);
            var boardView = new FakeBoardView();
            var adjacency = new BlueprintAdjacencyService(metadata);
            var boardPresenter = new BlueprintBoardPresenter(boardView, service, adjacency,
                new ContentCatalogBlueprintBoardPresentationResolver(catalog));
            var detailsView = new FakeDetailsView();
            var resolver = new ContentCatalogBlueprintDetailsResolver(catalog, adjacency, true);
            var detailsPresenter = new BlueprintDetailsPresenter(detailsView, boardPresenter, resolver);
            return new Fixture(board, service, boardView, boardPresenter, detailsView, detailsPresenter, resolver);
        }

        private static BlueprintDetailsSectionViewModel Section(BlueprintDetailsViewModel model, string heading) =>
            model.Sections.Single(section => section.Heading == heading);

        private static string Value(BlueprintDetailsViewModel model, string section, string label) =>
            Section(model, section).Values.Single(value => value.Label == label).Value;

        private static void AssertStat(BlueprintDetailsViewModel model, string section, string label,
            string baseValue, string currentValue, string unit)
        {
            BlueprintStatViewModel stat = Section(model, section).Stats.Single(value => value.Label == label);
            Assert.That(stat.BaseValue, Is.EqualTo(baseValue));
            Assert.That(stat.CurrentValue, Is.EqualTo(currentValue));
            Assert.That(stat.Unit, Is.EqualTo(unit));
            Assert.That(stat.Modifiers, Is.Empty);
        }

        private static List<BlueprintState> OwnedBlueprints(BlueprintBoardState board) =>
            (List<BlueprintState>)typeof(BlueprintBoardState).GetField("blueprints",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(board);

        private static void SetPrivate(object target, string fieldName, object value) => target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class Fixture : IDisposable
        {
            public Fixture(BlueprintBoardState board, BlueprintPlacementService service, FakeBoardView boardView,
                BlueprintBoardPresenter boardPresenter, FakeDetailsView detailsView,
                BlueprintDetailsPresenter detailsPresenter, IBlueprintDetailsResolver resolver)
            {
                Board = board;
                Service = service;
                BoardView = boardView;
                BoardPresenter = boardPresenter;
                DetailsView = detailsView;
                DetailsPresenter = detailsPresenter;
                Resolver = resolver;
            }

            public BlueprintBoardState Board { get; }
            public BlueprintPlacementService Service { get; }
            public FakeBoardView BoardView { get; }
            public BlueprintBoardPresenter BoardPresenter { get; }
            public FakeDetailsView DetailsView { get; }
            public BlueprintDetailsPresenter DetailsPresenter { get; }
            public IBlueprintDetailsResolver Resolver { get; }

            public BlueprintDetailsViewModel Resolve(string id) => Resolver.Resolve(Board.FindBlueprint(id), Board);

            public void Dispose()
            {
                DetailsPresenter.Dispose();
                BoardPresenter.Dispose();
            }
        }

        private sealed class FakeDetailsView : IBlueprintDetailsView
        {
            public BlueprintDetailsViewModel LastModel { get; private set; }
            public int RenderCount { get; private set; }
            public void Render(BlueprintDetailsViewModel model) { LastModel = model; RenderCount++; }
            public void Dispose() { }
        }

        private sealed class FakeBoardView : IBlueprintBoardView
        {
            public event Action<string> SelectionRequested;
            public event Action<string> BenchRequested { add { } remove { } }
            public event Action<string, int> ReorderRequested { add { } remove { } }
            public event Action<BlueprintBoardDropRequest> DropPreviewRequested { add { } remove { } }
            public event Action<BlueprintBoardDropRequest> DropRequested { add { } remove { } }
            public event Action UndoRequested { add { } remove { } }
            public event Action RedoRequested { add { } remove { } }

            public void Render(BlueprintBoardViewModel model) { }
            public void SetSelectionState(string selectedBlueprintId, IEnumerable<string> adjacentBlueprintIds) { }
            public void ShowDropPreview(BlueprintBoardDropRequest request, bool isValid, string message) { }
            public void ShowStatus(string message, bool isError) { }
            public void LogInteractionDiagnostic(string message) { }
            public void Dispose() { }
            public void RequestSelection(string id) => SelectionRequested?.Invoke(id);
        }
    }
}
