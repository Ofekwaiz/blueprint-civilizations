using System;
using System.Collections;
using System.Linq;
using BlueprintCivilizations.Blueprints;
using BlueprintCivilizations.UI.Development;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BlueprintCivilizations.UI.Tests.PlayMode
{
    public sealed class BlueprintBoardPlayModeTests : IPrebuildSetup, IPostBuildCleanup
    {
        private const string PlayerTestScenePath = "Assets/_Game/UI/Tests/PlayMode/BlueprintBoardPlayerTest.unity";
        private const string PlayerTestSceneName = "BlueprintBoardPlayerTest";
        private const string PlayerTestStorageKey = "tests.blueprint-board.player-runtime.v1";

        private Scene testScene;
        private Scene previousActiveScene;
        private GameObject rootObject;
        private UIDocument document;
        private BlueprintBoardSandboxBootstrap bootstrap;
        private string storageKey;

        public void Setup()
        {
#if UNITY_EDITOR
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayerTestScenePath) == null)
                throw new InvalidOperationException($"Blueprint Board Player test scene is missing at '{PlayerTestScenePath}'. " +
                                                    "Run the Blueprint Board Player test scene creation tool and commit the serialized scene.");
            if (EditorBuildSettings.scenes.Any(scene => scene.path == PlayerTestScenePath && scene.enabled)) return;
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.path != PlayerTestScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(PlayerTestScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(scene => scene.path != PlayerTestScenePath).ToArray();
            AssetDatabase.SaveAssets();
#endif
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousActiveScene = SceneManager.GetActiveScene();
            storageKey = PlayerTestStorageKey;
            PlayerPrefs.DeleteKey(storageKey);

            Assert.That(Application.CanStreamedLevelBeLoaded(PlayerTestSceneName), Is.True,
                $"Player test scene '{PlayerTestSceneName}' is unavailable. The IPrebuildSetup hook must include " +
                $"'{PlayerTestScenePath}' in the standalone test build.");
            AsyncOperation load = SceneManager.LoadSceneAsync(PlayerTestSceneName, LoadSceneMode.Additive);
            Assert.That(load, Is.Not.Null, $"Unity did not start loading Player test scene '{PlayerTestSceneName}'.");
            while (!load.isDone) yield return null;

            testScene = SceneManager.GetSceneByName(PlayerTestSceneName);
            Assert.That(testScene.IsValid() && testScene.isLoaded, Is.True,
                $"Player test scene '{PlayerTestSceneName}' was not loaded.");
            SceneManager.SetActiveScene(testScene);
            bootstrap = testScene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<BlueprintBoardSandboxBootstrap>(true)).SingleOrDefault();
            Assert.That(bootstrap, Is.Not.Null,
                $"Player test scene '{PlayerTestSceneName}' has no BlueprintBoardSandboxBootstrap composition root.");
            rootObject = bootstrap.gameObject;
            document = rootObject.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null,
                $"Player test scene '{PlayerTestSceneName}' has no UIDocument beside its runtime bootstrap.");
            Assert.That(document.panelSettings, Is.Not.Null,
                "The serialized Player test UIDocument has no PanelSettings reference.");

            yield return null;
            yield return null;
            Assert.That(BoardRoot(), Is.Not.Null, "The runtime Blueprint Board did not attach to its UIDocument.");
            Assert.That(BoardRoot().worldBound.width, Is.GreaterThan(0f));
            Assert.That(BoardRoot().worldBound.height, Is.GreaterThan(0f));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
            if (testScene.IsValid() && testScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(testScene);
                while (unload != null && !unload.isDone) yield return null;
            }
            PlayerPrefs.DeleteKey(storageKey);
            PlayerPrefs.Save();
            rootObject = null;
            document = null;
            bootstrap = null;
        }

        [UnityTest]
        public IEnumerator HoveringOneActiveCardAppliesHoverClassOnlyToThatCard()
        {
            VisualElement larva = Card("HIVE_LARVA");
            VisualElement spider = Card("HIVE_SPIDER");

            SendPointerEnter(larva);
            yield return null;

            Assert.That(larva.ClassListContains("blueprint-card--hovered"), Is.True);
            Assert.That(spider.ClassListContains("blueprint-card--hovered"), Is.False);
            Assert.That(spider.ClassListContains("blueprint-card--adjacent"), Is.False,
                "Hover is independent from selection-driven adjacency.");
        }

        [UnityTest]
        public IEnumerator ClickingFirstThenSecondCardTransfersSelection()
        {
            Click(Card("HIVE_LARVA"));
            yield return null;
            Assert.That(Card("HIVE_LARVA").ClassListContains("blueprint-card--selected"), Is.True);
            Assert.That(Card("HIVE_SPIDER").ClassListContains("blueprint-card--adjacent"), Is.True);

            Click(Card("HIVE_SPIDER"));
            yield return null;

            Assert.That(Card("HIVE_LARVA").ClassListContains("blueprint-card--selected"), Is.False);
            Assert.That(Card("HIVE_SPIDER").ClassListContains("blueprint-card--selected"), Is.True);
            Assert.That(Card("HIVE_LARVA").ClassListContains("blueprint-card--adjacent"), Is.True);

            Click(Element("blueprint-slot-2"));
            yield return null;
            Assert.That(Card("HIVE_LARVA").ClassListContains("blueprint-card--selected"), Is.False);
            Assert.That(Card("HIVE_SPIDER").ClassListContains("blueprint-card--selected"), Is.False);
            Assert.That(Card("HIVE_LARVA").ClassListContains("blueprint-card--adjacent"), Is.False);
        }

        [UnityTest]
        public IEnumerator SelectingBlueprintPopulatesDetailsPanel()
        {
            Assert.That(DetailsRoot().Q("blueprint-details-empty")
                .ClassListContains("blueprint-details--hidden"), Is.False);

            Click(Card("HIVE_LARVA"));
            yield return null;

            Assert.That(DetailsRoot().Q<Label>("blueprint-details-name").text, Is.EqualTo("Larva Brood"));
            Assert.That(DetailsValue("Content type"), Is.EqualTo("Unit Blueprint"));
            Assert.That(DetailsValue("Board state"), Is.EqualTo("Active"));
        }

        [UnityTest]
        public IEnumerator ChangingSelectionUpdatesDetailsAndClearingRestoresEmptyState()
        {
            Click(Card("HIVE_LARVA"));
            Click(Card("HIVE_SPIDER"));
            yield return null;

            Assert.That(DetailsRoot().Q<Label>("blueprint-details-name").text, Is.EqualTo("Web Spider"));

            Click(Element("blueprint-slot-2"));
            yield return null;

            Assert.That(DetailsRoot().Q("blueprint-details-empty")
                .ClassListContains("blueprint-details--hidden"), Is.False);
            Assert.That(DetailsRoot().Q("blueprint-details-content")
                .ClassListContains("blueprint-details--hidden"), Is.True);
        }

        [UnityTest]
        public IEnumerator BenchToEmptyActiveSlotDragActivatesBlueprint()
        {
            Drag(Card("HIVE_BEETLE"), Element("blueprint-slot-2"));
            yield return null;

            Assert.That(bootstrap.State.FindActiveIndex("HIVE_BEETLE"), Is.EqualTo(2));
            Assert.That(bootstrap.State.Bench.BlueprintDefinitionIds, Does.Not.Contain("HIVE_BEETLE"));
        }

        [UnityTest]
        public IEnumerator BenchToOccupiedActiveSlotDragUsesDocumentedInsertion()
        {
            Drag(Card("HIVE_BEETLE"), Element("blueprint-slot-0"));
            yield return null;

            Assert.That(bootstrap.State.Slots[0].BlueprintDefinitionId, Is.EqualTo("HIVE_BEETLE"));
            Assert.That(bootstrap.State.Slots[1].BlueprintDefinitionId, Is.EqualTo("HIVE_LARVA"));
            Assert.That(bootstrap.State.Slots[2].BlueprintDefinitionId, Is.EqualTo("HIVE_SPIDER"));
        }

        [UnityTest]
        public IEnumerator ActiveToBenchDragBenchesBlueprint()
        {
            Drag(Card("HIVE_LARVA"), Element("blueprint-bench-drop"));
            yield return null;

            Assert.That(bootstrap.State.FindActiveIndex("HIVE_LARVA"), Is.EqualTo(-1));
            Assert.That(bootstrap.State.Bench.BlueprintDefinitionIds, Does.Contain("HIVE_LARVA"));
        }

        [UnityTest]
        public IEnumerator ActiveToOccupiedActiveSlotDragSwapsBlueprints()
        {
            Drag(Card("HIVE_LARVA"), Element("blueprint-slot-1"));
            yield return null;

            Assert.That(bootstrap.State.Slots[0].BlueprintDefinitionId, Is.EqualTo("HIVE_SPIDER"));
            Assert.That(bootstrap.State.Slots[1].BlueprintDefinitionId, Is.EqualTo("HIVE_LARVA"));
        }

        [UnityTest]
        public IEnumerator ActiveToEmptySlotDragMovesBlueprintAndPreservesSelection()
        {
            Click(Card("HIVE_LARVA"));
            Drag(Card("HIVE_LARVA"), Element("blueprint-slot-2"));
            yield return null;

            Assert.That(bootstrap.State.FindActiveIndex("HIVE_LARVA"), Is.EqualTo(2));
            Assert.That(Card("HIVE_LARVA").ClassListContains("blueprint-card--selected"), Is.True);
        }

        [UnityTest]
        public IEnumerator MovingSelectedBlueprintUpdatesDetailsLocation()
        {
            Click(Card("HIVE_LARVA"));
            Drag(Card("HIVE_LARVA"), Element("blueprint-slot-2"));
            yield return null;

            Assert.That(DetailsRoot().Q<Label>("blueprint-details-name").text, Is.EqualTo("Larva Brood"));
            Assert.That(DetailsValue("Active slot index"), Is.EqualTo("2"));
        }

        [UnityTest]
        public IEnumerator SwappingBlueprintsUpdatesSelectedNeighborDisplay()
        {
            Drag(Card("HIVE_BEETLE"), Element("blueprint-slot-2"));
            yield return null;
            Click(Card("HIVE_SPIDER"));
            yield return null;
            Assert.That(DetailsRoot().Q<Label>("blueprint-details-name").text, Is.EqualTo("Web Spider"));
            Drag(Card("HIVE_LARVA"), Element("blueprint-slot-2"));
            yield return null;

            Assert.That(DetailsRoot().Q<Label>("blueprint-details-name").text, Is.EqualTo("Web Spider"));
            Assert.That(DetailsValue("Left neighbor"), Is.EqualTo("Shell Beetle"));
            Assert.That(DetailsValue("Right neighbor"), Is.EqualTo("Larva Brood"));
        }

        [UnityTest]
        public IEnumerator ActiveInsertionDragReordersBlueprints()
        {
            Drag(Card("HIVE_BEETLE"), Element("blueprint-slot-2"));
            yield return null;

            Drag(Card("HIVE_LARVA"), Element("blueprint-insertion-after-last"));
            yield return null;

            Assert.That(bootstrap.State.Slots[0].BlueprintDefinitionId, Is.EqualTo("HIVE_SPIDER"));
            Assert.That(bootstrap.State.Slots[1].BlueprintDefinitionId, Is.EqualTo("HIVE_BEETLE"));
            Assert.That(bootstrap.State.Slots[2].IsEmpty, Is.True);
            Assert.That(bootstrap.State.Slots[3].BlueprintDefinitionId, Is.EqualTo("HIVE_LARVA"));
        }

        [UnityTest]
        public IEnumerator DragThresholdShowsGhostSourceAndValidTargetPreviewBeforeRelease()
        {
            VisualElement source = Card("HIVE_BEETLE");
            VisualElement target = Element("blueprint-slot-2");

            BeginDrag(source, target);
            yield return null;

            Assert.That(source.ClassListContains("blueprint-card--dragging"), Is.True);
            Assert.That(BoardRoot().Q("blueprint-drag-ghost"), Is.Not.Null);
            Assert.That(target.ClassListContains("blueprint-drop-target--valid"), Is.True);

            SendPointerUp(source, target.worldBound.center);
            yield return null;
            Assert.That(bootstrap.State.FindActiveIndex("HIVE_BEETLE"), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator InvalidInsertionPreviewIsDistinctAndDropDoesNotMutateState()
        {
            VisualElement source = Card("HIVE_LARVA");
            VisualElement target = Element("blueprint-insertion-0");
            string before = BlueprintBoardSerializer.Serialize(bootstrap.State);

            BeginDrag(source, target);
            yield return null;

            Assert.That(target.ClassListContains("blueprint-drop-target--invalid"), Is.True);
            Assert.That(target.ClassListContains("blueprint-drop-target--valid"), Is.False);
            SendPointerUp(source, target.worldBound.center);
            yield return null;

            Assert.That(BlueprintBoardSerializer.Serialize(bootstrap.State), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator InvalidDropLeavesStateUnchangedAndReleasesPointerCapture()
        {
            VisualElement larva = Card("HIVE_LARVA");
            string before = BlueprintBoardSerializer.Serialize(bootstrap.State);
            int pointerId = PointerId.mousePointerId;

            Drag(larva, Element("blueprint-capacity-label"));
            yield return null;

            Assert.That(BlueprintBoardSerializer.Serialize(bootstrap.State), Is.EqualTo(before));
            Assert.That(larva.HasPointerCapture(pointerId), Is.False);
            Assert.That(Element("blueprint-status-label").ClassListContains("blueprint-status--error"), Is.True);
        }

        [UnityTest]
        public IEnumerator DetailsPanelSurvivesWideAndNarrowResponsiveLayout()
        {
            Click(Card("HIVE_LARVA"));
            VisualElement planning = document.rootVisualElement.Q("blueprint-planning-layout");
            VisualElement board = BoardRoot();
            VisualElement details = DetailsRoot();

            planning.style.width = 1400f;
            yield return null;
            yield return null;
            Assert.That(Mathf.Abs(details.worldBound.y - board.worldBound.y), Is.LessThan(8f),
                "Details should sit beside the Board when enough width is available.");

            planning.style.width = 620f;
            yield return null;
            yield return null;
            Assert.That(details.worldBound.y, Is.GreaterThan(board.worldBound.y + 20f),
                "Details should wrap below the Board at a narrow width.");
            Assert.That(DetailsRoot().Q<Label>("blueprint-details-name").text, Is.EqualTo("Larva Brood"));
        }

        [UnityTest]
        public IEnumerator DisableEnableDoesNotAccumulateCallbacksAndRestoresPersistedBoard()
        {
            int initialRevision = bootstrap.State.Revision;
            rootObject.SetActive(false);
            yield return null;
            rootObject.SetActive(true);
            yield return null;
            yield return null;

            Drag(Card("HIVE_BEETLE"), Element("blueprint-slot-2"));
            yield return null;

            Assert.That(bootstrap.State.Revision, Is.EqualTo(initialRevision + 1),
                "One drag must dispatch exactly one command after re-enable.");
            Assert.That(bootstrap.State.FindActiveIndex("HIVE_BEETLE"), Is.EqualTo(2));

            rootObject.SetActive(false);
            yield return null;
            rootObject.SetActive(true);
            yield return null;
            yield return null;

            Assert.That(bootstrap.State.FindActiveIndex("HIVE_BEETLE"), Is.EqualTo(2),
                "The isolated PlayerPrefs save must restore after a Play Mode lifecycle restart.");
            Assert.That(document.rootVisualElement.Query<VisualElement>("blueprint-details-panel").ToList().Count,
                Is.EqualTo(1), "Repeated UIDocument lifecycle changes must retain exactly one Details Panel.");
            Click(Card("HIVE_BEETLE"));
            yield return null;
            Assert.That(DetailsRoot().Q<Label>("blueprint-details-name").text, Is.EqualTo("Shell Beetle"));
        }

        private VisualElement BoardRoot() => document.rootVisualElement.Q("blueprint-board-panel");
        private VisualElement DetailsRoot() => document.rootVisualElement.Q("blueprint-details-panel");

        private string DetailsValue(string label)
        {
            foreach (VisualElement row in DetailsRoot().Query<VisualElement>(className: "blueprint-details-value-row").ToList())
            {
                Label rowLabel = row.Q<Label>(className: "blueprint-details-value-label");
                if (rowLabel?.text != label) continue;
                return row.Q<Label>(className: "blueprint-details-value")?.text;
            }
            Assert.Fail($"Blueprint Details value row '{label}' was not rendered.");
            return "";
        }

        private VisualElement Card(string id)
        {
            VisualElement card = BoardRoot()?.Q($"blueprint-card-{id}");
            Assert.That(card, Is.Not.Null, $"Blueprint card '{id}' was not rendered.");
            return card;
        }

        private VisualElement Element(string name)
        {
            VisualElement element = BoardRoot()?.Q(name);
            Assert.That(element, Is.Not.Null, $"Blueprint Board element '{name}' was not rendered.");
            return element;
        }

        private static void Click(VisualElement target)
        {
            Vector2 position = target.worldBound.center;
            SendPointerDown(target, position);
            SendPointerUp(target, position);
        }

        private static void Drag(VisualElement source, VisualElement target)
        {
            BeginDrag(source, target);
            SendPointerUp(source, target.worldBound.center);
        }

        private static void BeginDrag(VisualElement source, VisualElement target)
        {
            SendPointerDown(source, source.worldBound.center);
            SendPointerMove(source, target.worldBound.center);
        }

        private static void SendPointerDown(VisualElement target, Vector2 position)
        {
            var data = new TestPointerEvent(position, 0, 1);
            using PointerDownEvent pointerEvent = PointerDownEvent.GetPooled(data);
            target.SendEvent(pointerEvent);
        }

        private static void SendPointerMove(VisualElement target, Vector2 position, bool pressed = true)
        {
            var data = new TestPointerEvent(position, -1, pressed ? 1 : 0);
            using PointerMoveEvent pointerEvent = PointerMoveEvent.GetPooled(data);
            target.SendEvent(pointerEvent);
        }

        private static void SendPointerUp(VisualElement target, Vector2 position)
        {
            var data = new TestPointerEvent(position, 0, 0);
            using PointerUpEvent pointerEvent = PointerUpEvent.GetPooled(data);
            target.SendEvent(pointerEvent);
        }

        private static void SendPointerEnter(VisualElement target)
        {
            var data = new TestPointerEvent(target.worldBound.center, -1, 0);
            using PointerMoveEvent pointerEvent = PointerMoveEvent.GetPooled(data);
            target.SendEvent(pointerEvent);
        }

        private sealed class TestPointerEvent : IPointerEvent
        {
            public TestPointerEvent(Vector2 pointerPosition, int pointerButton, int buttons)
            {
                position = pointerPosition;
                localPosition = pointerPosition;
                button = pointerButton;
                pressedButtons = buttons;
            }

            public int pointerId => PointerId.mousePointerId;
            public string pointerType => UnityEngine.UIElements.PointerType.mouse;
            public bool isPrimary => true;
            public int button { get; }
            public int pressedButtons { get; }
            public Vector3 position { get; }
            public Vector3 localPosition { get; }
            public Vector3 deltaPosition => Vector3.zero;
            public float deltaTime => 0f;
            public int clickCount => 1;
            public float pressure => pressedButtons == 0 ? 0f : 1f;
            public float tangentialPressure => 0f;
            public float altitudeAngle => 0f;
            public float azimuthAngle => 0f;
            public float twist => 0f;
            public Vector2 tilt => Vector2.zero;
            public PenStatus penStatus => PenStatus.None;
            public Vector2 radius => Vector2.zero;
            public Vector2 radiusVariance => Vector2.zero;
            public EventModifiers modifiers => EventModifiers.None;
            public bool shiftKey => false;
            public bool ctrlKey => false;
            public bool commandKey => false;
            public bool altKey => false;
            public bool actionKey => false;
        }
    }
}
