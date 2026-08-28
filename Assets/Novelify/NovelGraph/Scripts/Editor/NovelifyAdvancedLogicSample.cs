using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovelGraph.Editor
{
    public static class NovelifyAdvancedLogicSample
    {
        private const string SampleRoot = "Assets/Novelify/Samples/AdvancedLogic";
        private const string GraphPath = SampleRoot + "/AdvancedLogicExample.asset";
        private const string PagePath = SampleRoot + "/ReusableRockStrike.asset";
        private const string ScenePath = SampleRoot + "/AdvancedLogicExample.unity";
        private const string RockTargetId = "advanced_sample_rock";

        [InitializeOnLoadMethod]
        private static void CreateMissingSampleAfterImport()
        {
            NovelGraphAsset graph = AssetDatabase.LoadAssetAtPath<NovelGraphAsset>(GraphPath);
            bool needsDeclarationUpgrade = graph != null && graph.Nodes
                .OfType<NamedRerouteInNode>()
                .Any(usage => string.IsNullOrWhiteSpace(usage.declarationId));
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null || needsDeclarationUpgrade)
            {
                EditorApplication.delayCall += CreateAdvancedLogicSample;
            }
        }

        [MenuItem("Tools/Novelify/Create or Refresh Advanced Logic Sample")]
        public static void CreateAdvancedLogicSample()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Novelify/Samples/AdvancedLogic"));

            NovelPageAsset page = AssetDatabase.LoadAssetAtPath<NovelPageAsset>(PagePath);
            if (page == null)
            {
                page = ScriptableObject.CreateInstance<NovelPageAsset>();
                AssetDatabase.CreateAsset(page, PagePath);
            }

            NovelGraphAsset graph = AssetDatabase.LoadAssetAtPath<NovelGraphAsset>(GraphPath);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<NovelGraphAsset>();
                AssetDatabase.CreateAsset(graph, GraphPath);
            }

            BuildReusablePage(page);
            BuildMainGraph(graph, page);
            CreateScene(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Novelify advanced logic sample created: {ScenePath}");
        }

        private static void BuildReusablePage(NovelPageAsset page)
        {
            page.Nodes.Clear();
            page.Connections.Clear();

            StartNode start = Add(page, new StartNode(), 40f, 160f);
            DialogueNode strikeLine = Add(page, new DialogueNode
            {
                speakerMode = NovelSpeakerMode.Narrator,
                dialogue = "The same reusable Novel Page swings the crystal hammer.",
                playLetterSounds = false,
                charactersPerSecond = 44f
            }, 300f, 160f);
            CallFunctionNode strikeRock = Add(page, new CallFunctionNode
            {
                callMode = NovelFunctionCallMode.ComponentMethod,
                targetMode = NovelFunctionTargetMode.TargetId,
                target = RockTargetId,
                componentType = nameof(NovelGraphAdvancedSampleRock),
                methodName = nameof(NovelGraphAdvancedSampleRock.Strike),
                arguments = new List<NovelFunctionArgument>
                {
                    new NovelFunctionArgument
                    {
                        name = "playSound",
                        source = NovelFunctionArgumentSource.Constant,
                        type = NovelFunctionArgumentType.Boolean,
                        boolValue = true
                    },
                    new NovelFunctionArgument
                    {
                        name = "impactLabel",
                        source = NovelFunctionArgumentSource.Constant,
                        type = NovelFunctionArgumentType.String,
                        stringValue = "crystal hammer"
                    },
                    new NovelFunctionArgument
                    {
                        name = "damage",
                        source = NovelFunctionArgumentSource.StoryState,
                        type = NovelFunctionArgumentType.Integer,
                        stateKey = "rock_damage"
                    }
                }
            }, 580f, 160f);
            EndNode end = Add(page, new EndNode(), 880f, 160f);

            page.Connect(start, 0, strikeLine);
            page.Connect(strikeLine, 0, strikeRock);
            page.Connect(strikeRock, 0, end);
            EditorUtility.SetDirty(page);
        }

        private static void BuildMainGraph(NovelGraphAsset graph, NovelPageAsset page)
        {
            graph.Nodes.Clear();
            graph.Connections.Clear();

            StartNode start = Add(graph, new StartNode(), 40f, 260f);
            SetIntNode declareDamage = Add(graph, new SetIntNode
            {
                key = "rock_damage",
                value = 2
            }, 280f, 260f);
            RerouteNode reroute = Add(graph, new RerouteNode(), 540f, 260f);
            NamedRerouteInNode routeIn = Add(graph, new NamedRerouteInNode
            {
                routeName = "rock_encounter"
            }, 760f, 260f);

            NamedRerouteOutNode routeOut = Add(graph, new NamedRerouteOutNode
            {
                routeName = "rock_encounter"
            }, 1040f, 40f);
            routeIn.SetDeclaration(routeOut);
            NovelPageNode firstStrike = Add(graph, new NovelPageNode { page = page }, 1300f, 40f);
            DialogueNode betweenStrikes = Add(graph, new DialogueNode
            {
                speakerMode = NovelSpeakerMode.Narrator,
                dialogue = "The rock remains. Call the exact same page again—no duplicated dialogue or gameplay wiring.",
                playLetterSounds = false,
                charactersPerSecond = 44f
            }, 1580f, 40f);
            NovelPageNode secondStrike = Add(graph, new NovelPageNode { page = page }, 1880f, 40f);
            DialogueNode ending = Add(graph, new DialogueNode
            {
                speakerMode = NovelSpeakerMode.Narrator,
                dialogue = "The rock component plays its own sound, runs its own break logic, and removes itself.",
                playLetterSounds = false,
                charactersPerSecond = 44f
            }, 2160f, 40f);
            EndNode end = Add(graph, new EndNode(), 2460f, 40f);

            graph.Connect(start, 0, declareDamage);
            graph.Connect(declareDamage, 0, reroute);
            graph.Connect(reroute, 0, routeIn);
            graph.Connect(routeOut, 0, firstStrike);
            graph.Connect(firstStrike, 0, betweenStrikes);
            graph.Connect(betweenStrikes, 0, secondStrike);
            graph.Connect(secondStrike, 0, ending);
            graph.Connect(ending, 0, end);
            EditorUtility.SetDirty(graph);
        }

        private static void CreateScene(NovelGraphAsset graph)
        {
            Scene previousScene = SceneManager.GetActiveScene();
            bool replaceEmptyScene = previousScene.IsValid() && string.IsNullOrEmpty(previousScene.path) &&
                                     previousScene.rootCount == 0 && !previousScene.isDirty;
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                replaceEmptyScene ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.5f, -8f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f, 1f);

            GameObject storyObject = new GameObject("Novelify Advanced Logic Player");
            NovelGraphPlayer player = storyObject.AddComponent<NovelGraphPlayer>();
            player.SetGraph(graph);
            player.SetStoryTitle("NOVELIFY — REUSABLE ROCK ENCOUNTER");

            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Ancient Sound Rock";
            rock.transform.position = new Vector3(0f, 0.25f, 0f);
            rock.transform.localScale = new Vector3(2.4f, 2.4f, 1.2f);
            rock.AddComponent<AudioSource>().spatialBlend = 0f;
            rock.AddComponent<NovelGraphAdvancedSampleRock>();
            NovelFunctionTarget target = rock.AddComponent<NovelFunctionTarget>();
            target.SetTargetId(RockTargetId);

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!replaceEmptyScene)
            {
                if (previousScene.IsValid())
                {
                    SceneManager.SetActiveScene(previousScene);
                }

                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T Add<T>(NovelGraphAsset graph, T node, float x, float y) where T : NovelGraphNode
        {
            node.SetPosition(new Rect(x, y, 250f, 170f));
            graph.AddNode(node);
            return node;
        }
    }
}
