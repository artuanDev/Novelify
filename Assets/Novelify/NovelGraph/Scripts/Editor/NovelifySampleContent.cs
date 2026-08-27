using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NovelGraph.Editor
{
    public static class NovelifySampleContent
    {
        private const string SampleRoot = "Assets/Novelify/Samples";
        private const string GraphPath = SampleRoot + "/Stories/TheLastBeacon.asset";
        private const string ScenePath = SampleRoot + "/Scenes/DecisionEventsSample.unity";
        private const string MiraPath = SampleRoot + "/Characters/Mira.asset";
        private const string EliasPath = SampleRoot + "/Characters/Elias.asset";

        [InitializeOnLoadMethod]
        private static void CreateMissingSampleAfterImport()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorApplication.delayCall += CreateSampleContent;
            }
        }

        [MenuItem("Tools/Novelify/Create or Refresh Sample Story")]
        public static void CreateSampleContent()
        {
            EnsureFolders();
            NovelGraphAsset graph = AssetDatabase.LoadAssetAtPath<NovelGraphAsset>(GraphPath);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<NovelGraphAsset>();
                AssetDatabase.CreateAsset(graph, GraphPath);
            }

            NovelCharacter mira = GetOrCreateCharacter(
                MiraPath, "Mira Vale", 520f, new Color(0.12f, 0.62f, 0.58f, 1f));
            NovelCharacter elias = GetOrCreateCharacter(
                EliasPath, "Elias Rook", 285f, new Color(0.88f, 0.56f, 0.2f, 1f));

            BuildGraph(graph, mira, elias);
            CreateScene(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Novelify sample created: {ScenePath}");
        }

        public static void CreateSampleContentBatch()
        {
            CreateSampleContent();
        }

        private static void BuildGraph(NovelGraphAsset graph, NovelCharacter mira, NovelCharacter elias)
        {
            graph.Nodes.Clear();
            graph.Connections.Clear();

            StartNode start = Add(graph, new StartNode(), 40f, 340f);
            DialogueNode opening = Narration(graph,
                "At the edge of a drowned city, the last beacon wakes for one final night.",
                280f, 340f);
            DialogueNode miraIntroduction = CharacterLine(graph, mira,
                "The lower road is gone. Give me the battery and I can still reach the tower before the storm.",
                true, 36f, 560f, 340f);
            DialogueNode eliasWarning = CharacterLine(graph, elias,
                "Mira, the harbor sensors just failed. Whatever we choose, we choose it now.",
                true, 30f, 840f, 340f);
            TwoChoiceNode trustChoice = Add(graph, new TwoChoiceNode
            {
                prompt = "Mira holds out her hand. What do you do?",
                firstChoice = "Trust Mira with the battery",
                firstSignal = "mira_trusted",
                firstValue = 1,
                secondChoice = "Keep the battery and turn back",
                secondSignal = "mira_refused",
                secondValue = 0,
                stateKey = "trusted_mira"
            }, 1120f, 340f);

            DialogueNode trusted = CharacterLine(graph, mira,
                "Good. Then this is our light, not mine. Stay close and keep above the flood marks.",
                true, 38f, 1440f, 60f);
            CallFunctionNode intensifyStorm = Add(graph, new CallFunctionNode
            {
                functionId = "intensify_storm"
            }, 1720f, 60f);
            MultiChoiceNode routeChoice = Add(graph, new MultiChoiceNode
            {
                prompt = "Lightning splits the old tower stairs. Choose the plan.",
                choiceA = "Climb the exposed tower stairs",
                choiceB = "Cross the flooded seawall",
                choiceC = "Repair the emergency radio first",
                choiceD = "Wait below until the storm passes",
                stateKey = "tower_route"
            }, 2000f, 60f);

            DialogueNode towerApproach = CharacterLine(graph, elias,
                "I will hold the cable. Mira, take the battery up and do not look down.",
                true, 32f, 2320f, -180f);
            CallFunctionNode openGatesA = Add(graph, new CallFunctionNode
            {
                functionId = "open_harbor_gates"
            }, 2600f, -180f);
            DialogueNode towerEnding = CharacterLine(graph, mira,
                "The beacon is alive. Look at the water, Elias. The ships can see us.",
                true, 40f, 2880f, -180f);
            EndNode towerEnd = Add(graph, new EndNode(), 3160f, -180f);

            SignalNode seawallSignal = Add(graph, new SignalNode
            {
                signal = "seawall_chosen"
            }, 2320f, 40f);
            DialogueNode seawallEnding = Narration(graph,
                "They lash themselves together and cross the seawall. At dawn, a rescue lamp answers from the eastern pier.",
                2600f, 40f);
            EndNode seawallEnd = Add(graph, new EndNode(), 2880f, 40f);

            SetIntNode repairRadio = Add(graph, new SetIntNode
            {
                key = "radio_repaired",
                value = 1
            }, 2320f, 260f);
            IntConditionNode radioCondition = Add(graph, new IntConditionNode
            {
                key = "radio_repaired",
                expectedValue = 1
            }, 2600f, 260f);
            DialogueNode radioSuccess = CharacterLine(graph, elias,
                "Signal acquired. Three ships are holding beyond the reef and waiting for the gates.",
                true, 34f, 2880f, 200f);
            CallFunctionNode openGatesC = Add(graph, new CallFunctionNode
            {
                functionId = "open_harbor_gates"
            }, 3160f, 200f);
            DialogueNode radioEnding = Narration(graph,
                "The harbor gates groan open as the beacon sweeps across the channel.",
                3440f, 200f);
            EndNode radioEnd = Add(graph, new EndNode(), 3720f, 200f);
            DialogueNode radioFailure = Narration(graph,
                "The radio remains silent. Without a bearing, the ships turn away from the reef.",
                2880f, 380f);
            EndNode radioFailureEnd = Add(graph, new EndNode(), 3160f, 380f);

            CallFunctionNode waitPowerCut = Add(graph, new CallFunctionNode
            {
                functionId = "cut_tower_power"
            }, 2320f, 500f);
            DialogueNode waitEnding = Narration(graph,
                "They wait below. The storm passes, but so do the ships that needed the light.",
                2600f, 500f);
            EndNode waitEnd = Add(graph, new EndNode(), 2880f, 500f);

            DialogueNode refused = Narration(graph,
                "Mira lowers her hand. The battery is still heavy in your coat when the tower power fails.",
                1440f, 720f);
            CallFunctionNode refusedPowerCut = Add(graph, new CallFunctionNode
            {
                functionId = "cut_tower_power"
            }, 1720f, 720f);
            DialogueNode mutedElias = CharacterLine(graph, elias,
                "No beacon, no harbor. We should leave before the lower district floods.",
                false, 32f, 2000f, 720f);
            EndNode refusedEnd = Add(graph, new EndNode(), 2280f, 720f);

            graph.Connect(start, 0, opening);
            graph.Connect(opening, 0, miraIntroduction);
            graph.Connect(miraIntroduction, 0, eliasWarning);
            graph.Connect(eliasWarning, 0, trustChoice);
            graph.Connect(trustChoice, 0, trusted);
            graph.Connect(trustChoice, 1, refused);
            graph.Connect(trusted, 0, intensifyStorm);
            graph.Connect(intensifyStorm, 0, routeChoice);
            graph.Connect(routeChoice, 0, towerApproach);
            graph.Connect(towerApproach, 0, openGatesA);
            graph.Connect(openGatesA, 0, towerEnding);
            graph.Connect(towerEnding, 0, towerEnd);
            graph.Connect(routeChoice, 1, seawallSignal);
            graph.Connect(seawallSignal, 0, seawallEnding);
            graph.Connect(seawallEnding, 0, seawallEnd);
            graph.Connect(routeChoice, 2, repairRadio);
            graph.Connect(repairRadio, 0, radioCondition);
            graph.Connect(radioCondition, 0, radioSuccess);
            graph.Connect(radioSuccess, 0, openGatesC);
            graph.Connect(openGatesC, 0, radioEnding);
            graph.Connect(radioEnding, 0, radioEnd);
            graph.Connect(radioCondition, 1, radioFailure);
            graph.Connect(radioFailure, 0, radioFailureEnd);
            graph.Connect(routeChoice, 3, waitPowerCut);
            graph.Connect(waitPowerCut, 0, waitEnding);
            graph.Connect(waitEnding, 0, waitEnd);
            graph.Connect(refused, 0, refusedPowerCut);
            graph.Connect(refusedPowerCut, 0, mutedElias);
            graph.Connect(mutedElias, 0, refusedEnd);

            EditorUtility.SetDirty(graph);
        }

        private static DialogueNode Narration(NovelGraphAsset graph, string text, float x, float y)
        {
            return Add(graph, new DialogueNode
            {
                speakerMode = NovelSpeakerMode.Narrator,
                dialogue = text,
                playLetterSounds = false,
                charactersPerSecond = 42f
            }, x, y);
        }

        private static DialogueNode CharacterLine(
            NovelGraphAsset graph,
            NovelCharacter character,
            string text,
            bool playLetterSounds,
            float charactersPerSecond,
            float x,
            float y)
        {
            return Add(graph, new DialogueNode
            {
                speakerMode = NovelSpeakerMode.Character,
                character = character,
                dialogue = text,
                playLetterSounds = playLetterSounds,
                charactersPerSecond = charactersPerSecond
            }, x, y);
        }

        private static NovelCharacter GetOrCreateCharacter(
            string path,
            string displayName,
            float frequency,
            Color nameColor)
        {
            NovelCharacter character = AssetDatabase.LoadAssetAtPath<NovelCharacter>(path);
            if (character == null)
            {
                character = ScriptableObject.CreateInstance<NovelCharacter>();
                AssetDatabase.CreateAsset(character, path);
            }

            character.Configure(displayName, frequency, nameColor);
            EditorUtility.SetDirty(character);
            return character;
        }

        private static T Add<T>(NovelGraphAsset graph, T node, float x, float y) where T : NovelGraphNode
        {
            node.SetPosition(new Rect(x, y, 240f, 160f));
            graph.AddNode(node);
            return node;
        }

        private static void CreateScene(NovelGraphAsset graph)
        {
            Scene previousScene = SceneManager.GetActiveScene();
            bool replaceEmptyScene = previousScene.IsValid() &&
                                     string.IsNullOrEmpty(previousScene.path) &&
                                     previousScene.rootCount == 0 &&
                                     !previousScene.isDirty;
            NewSceneMode mode = replaceEmptyScene ? NewSceneMode.Single : NewSceneMode.Additive;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            SceneManager.SetActiveScene(scene);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.09f, 1f);

            GameObject storyObject = new GameObject("Novelify Story Player");
            NovelGraphPlayer player = storyObject.AddComponent<NovelGraphPlayer>();
            player.SetGraph(graph);
            NovelGraphSampleEventReceiver receiver = storyObject.AddComponent<NovelGraphSampleEventReceiver>();
            BindFunction(player, "intensify_storm", receiver.IntensifyStorm);
            BindFunction(player, "open_harbor_gates", receiver.OpenHarborGates);
            BindFunction(player, "cut_tower_power", receiver.CutTowerPower);

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!replaceEmptyScene)
            {
                if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
            const string showcaseScene = SampleRoot + "/Scenes/EverythingShowcase.unity";
            EditorBuildSettings.scenes = AssetDatabase.LoadAssetAtPath<SceneAsset>(showcaseScene) != null
                ? new[]
                {
                    new EditorBuildSettingsScene(ScenePath, true),
                    new EditorBuildSettingsScene(showcaseScene, true)
                }
                : new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void BindFunction(NovelGraphPlayer player, string id, UnityAction callback)
        {
            NovelFunctionBinding binding = new NovelFunctionBinding(id);
            UnityEventTools.AddPersistentListener(binding.Callback, callback);
            player.FunctionBindings.Add(binding);
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Novelify/Samples/Stories"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Novelify/Samples/Scenes"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Novelify/Samples/Characters"));
        }
    }
}
