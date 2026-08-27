using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovelGraph.Editor
{
    public static class NovelifySampleContent
    {
        private const string SampleRoot = "Assets/Novelify/Samples";
        private const string GraphPath = SampleRoot + "/Stories/TheLastBeacon.asset";
        private const string ScenePath = SampleRoot + "/Scenes/DecisionEventsSample.unity";

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

            BuildGraph(graph);
            CreateScene(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Novelify sample created: {ScenePath}");
        }

        public static void CreateSampleContentBatch()
        {
            CreateSampleContent();
        }

        private static void BuildGraph(NovelGraphAsset graph)
        {
            graph.Nodes.Clear();
            graph.Connections.Clear();

            StartNode start = Add(graph, new StartNode(), 40f, 280f);
            DialogueNode opening = Add(graph, new DialogueNode
            {
                speaker = "Narrator",
                dialogue = "At the edge of a drowned city, the last beacon wakes for one final night."
            }, 280f, 280f);
            DialogueNode mira = Add(graph, new DialogueNode
            {
                speaker = "Mira",
                dialogue = "The tide has covered the lower road. Give me the battery and I can still reach the tower."
            }, 540f, 280f);
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
            }, 810f, 280f);

            DialogueNode trusted = Add(graph, new DialogueNode
            {
                speaker = "Mira",
                dialogue = "Good. Then this is our light, not mine. Stay close and keep above the flood marks."
            }, 1110f, 100f);
            TwoChoiceNode beaconChoice = Add(graph, new TwoChoiceNode
            {
                prompt = "At the tower, the storm is almost overhead. When should Mira ignite the beacon?",
                firstChoice = "Light it now, before the storm breaks",
                firstSignal = "beacon_lit",
                firstValue = 1,
                secondChoice = "Wait for a ship to answer the radio",
                secondSignal = "beacon_waited",
                secondValue = 0,
                stateKey = "beacon_lit_early"
            }, 1380f, 100f);
            DialogueNode earlyEnding = Add(graph, new DialogueNode
            {
                speaker = "Narrator",
                dialogue = "Amber light rolls across the black water. Far beyond the rain, three ships answer in sequence."
            }, 1690f, 20f);
            EndNode earlyEnd = Add(graph, new EndNode(), 1980f, 20f);
            DialogueNode waitedEnding = Add(graph, new DialogueNode
            {
                speaker = "Mira",
                dialogue = "A voice finally cracks through the radio. We light the beacon together, exactly when they need it."
            }, 1690f, 190f);
            EndNode waitedEnd = Add(graph, new EndNode(), 1980f, 190f);

            DialogueNode refused = Add(graph, new DialogueNode
            {
                speaker = "Narrator",
                dialogue = "Mira lowers her hand. By dawn the tower is dark, and the battery is still heavy in your coat."
            }, 1110f, 500f);
            EndNode refusedEnd = Add(graph, new EndNode(), 1400f, 500f);

            graph.Connect(start, 0, opening);
            graph.Connect(opening, 0, mira);
            graph.Connect(mira, 0, trustChoice);
            graph.Connect(trustChoice, 0, trusted);
            graph.Connect(trustChoice, 1, refused);
            graph.Connect(trusted, 0, beaconChoice);
            graph.Connect(beaconChoice, 0, earlyEnding);
            graph.Connect(beaconChoice, 1, waitedEnding);
            graph.Connect(earlyEnding, 0, earlyEnd);
            graph.Connect(waitedEnding, 0, waitedEnd);
            graph.Connect(refused, 0, refusedEnd);

            EditorUtility.SetDirty(graph);
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
            storyObject.AddComponent<NovelGraphSampleEventReceiver>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!replaceEmptyScene)
            {
                if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Novelify/Samples/Stories"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Novelify/Samples/Scenes"));
        }
    }
}
