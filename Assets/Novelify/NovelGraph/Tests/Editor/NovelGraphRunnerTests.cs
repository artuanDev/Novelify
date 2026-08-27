using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NovelGraph.Tests
{
    public class NovelGraphRunnerTests
    {
        private NovelGraphAsset m_graph;

        [SetUp]
        public void SetUp()
        {
            m_graph = ScriptableObject.CreateInstance<NovelGraphAsset>();
            m_graph.name = "Runner Test Graph";
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(m_graph);
        }

        [Test]
        public void RunnerPausesForDialogueAndCompletesAfterAdvance()
        {
            StartNode start = Add(new StartNode());
            DialogueNode dialogue = Add(new DialogueNode { speaker = "Mira", dialogue = "Ready?" });
            EndNode end = Add(new EndNode());
            m_graph.Connect(start, 0, dialogue);
            m_graph.Connect(dialogue, 0, end);

            NovelGraphRunner runner = new NovelGraphRunner();
            runner.Start(m_graph);

            Assert.That(runner.Status, Is.EqualTo(NovelGraphRunnerStatus.WaitingForAdvance));
            Assert.That(runner.CurrentPresentation.Speaker, Is.EqualTo("Mira"));
            Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Ready?"));

            Assert.That(runner.Advance(), Is.True);
            Assert.That(runner.Status, Is.EqualTo(NovelGraphRunnerStatus.Completed));
        }

        [Test]
        public void ChoiceStoresStateRaisesSignalAndUsesSelectedBranch()
        {
            StartNode start = Add(new StartNode());
            TwoChoiceNode choice = Add(new TwoChoiceNode
            {
                prompt = "Choose",
                firstChoice = "Left",
                firstSignal = "went_left",
                firstValue = 10,
                secondChoice = "Right",
                secondSignal = "went_right",
                secondValue = 20,
                stateKey = "direction"
            });
            DialogueNode left = Add(new DialogueNode { dialogue = "Left branch" });
            DialogueNode right = Add(new DialogueNode { dialogue = "Right branch" });
            m_graph.Connect(start, 0, choice);
            m_graph.Connect(choice, 0, left);
            m_graph.Connect(choice, 1, right);

            string raisedSignal = null;
            NovelGraphRunner runner = new NovelGraphRunner();
            runner.SignalRaised += signal => raisedSignal = signal;
            runner.Start(m_graph);

            Assert.That(runner.Status, Is.EqualTo(NovelGraphRunnerStatus.WaitingForChoice));
            Assert.That(runner.Choose(1), Is.True);
            Assert.That(runner.State.GetInt("direction"), Is.EqualTo(20));
            Assert.That(raisedSignal, Is.EqualTo("went_right"));
            Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Right branch"));
        }

        [Test]
        public void IntegerConditionSelectsMatchingOutput()
        {
            StartNode start = Add(new StartNode());
            SetIntNode set = Add(new SetIntNode { key = "trust", value = 1 });
            IntConditionNode condition = Add(new IntConditionNode { key = "trust", expectedValue = 1 });
            DialogueNode equal = Add(new DialogueNode { dialogue = "Equal" });
            DialogueNode notEqual = Add(new DialogueNode { dialogue = "Not equal" });
            m_graph.Connect(start, 0, set);
            m_graph.Connect(set, 0, condition);
            m_graph.Connect(condition, 0, equal);
            m_graph.Connect(condition, 1, notEqual);

            NovelGraphRunner runner = new NovelGraphRunner();
            runner.Start(m_graph);

            Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Equal"));
        }

        [Test]
        public void SaveDataRestoresTheCurrentPresentationAndState()
        {
            StartNode start = Add(new StartNode());
            SetIntNode set = Add(new SetIntNode { key = "chapter", value = 3 });
            DialogueNode dialogue = Add(new DialogueNode { dialogue = "A saved line" });
            m_graph.Connect(start, 0, set);
            m_graph.Connect(set, 0, dialogue);

            NovelGraphRunner original = new NovelGraphRunner();
            original.Start(m_graph);
            NovelGraphSaveData save = original.CaptureSaveData();

            NovelGraphRunner restored = new NovelGraphRunner();
            Assert.That(restored.Restore(m_graph, save), Is.True);
            Assert.That(restored.State.GetInt("chapter"), Is.EqualTo(3));
            Assert.That(restored.CurrentPresentation.Text, Is.EqualTo("A saved line"));
        }

        [Test]
        public void CharacterDialogueCarriesVoicePresentationSettings()
        {
            NovelCharacter character = ScriptableObject.CreateInstance<NovelCharacter>();
            character.Configure("Mira Vale", 520f, Color.cyan);
            try
            {
                StartNode start = Add(new StartNode());
                DialogueNode dialogue = Add(new DialogueNode
                {
                    speakerMode = NovelSpeakerMode.Character,
                    character = character,
                    dialogue = "A voiced line",
                    playLetterSounds = true,
                    charactersPerSecond = 24f
                });
                m_graph.Connect(start, 0, dialogue);

                NovelGraphRunner runner = new NovelGraphRunner();
                runner.Start(m_graph);

                Assert.That(runner.CurrentPresentation.Speaker, Is.EqualTo("Mira Vale"));
                Assert.That(runner.CurrentPresentation.Character, Is.SameAs(character));
                Assert.That(runner.CurrentPresentation.PlayLetterSounds, Is.True);
                Assert.That(runner.CurrentPresentation.CharactersPerSecond, Is.EqualTo(24f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void CallFunctionRequestsBindingAndContinues()
        {
            StartNode start = Add(new StartNode());
            CallFunctionNode call = Add(new CallFunctionNode { functionId = "open_harbor_gates" });
            EndNode end = Add(new EndNode());
            m_graph.Connect(start, 0, call);
            m_graph.Connect(call, 0, end);

            string requestedFunction = null;
            NovelGraphRunner runner = new NovelGraphRunner();
            runner.FunctionRequested += functionId => requestedFunction = functionId;
            runner.Start(m_graph);

            Assert.That(requestedFunction, Is.EqualTo("open_harbor_gates"));
            Assert.That(runner.Status, Is.EqualTo(NovelGraphRunnerStatus.Completed));
        }

        [Test]
        public void EveryNodeAndExposedFieldHasTooltipText()
        {
            Type[] nodeTypes = typeof(NovelGraphNode).Assembly.GetTypes()
                .Where(type => type != typeof(NovelGraphNode) &&
                               !type.IsAbstract &&
                               typeof(NovelGraphNode).IsAssignableFrom(type))
                .ToArray();

            foreach (Type nodeType in nodeTypes)
            {
                NodeInfoAttribute nodeInfo = nodeType.GetCustomAttribute<NodeInfoAttribute>();
                Assert.That(nodeInfo, Is.Not.Null, $"{nodeType.Name} is missing NodeInfo.");
                Assert.That(nodeInfo.description, Is.Not.Empty, $"{nodeType.Name} is missing a node tooltip.");

                FieldInfo[] exposedFields = nodeType.GetFields()
                    .Where(field => field.GetCustomAttribute<ExposedPropertyAttribute>() != null)
                    .ToArray();
                foreach (FieldInfo field in exposedFields)
                {
                    TooltipAttribute tooltip = field.GetCustomAttribute<TooltipAttribute>();
                    Assert.That(tooltip, Is.Not.Null, $"{nodeType.Name}.{field.Name} is missing Tooltip.");
                    Assert.That(tooltip.tooltip, Is.Not.Empty, $"{nodeType.Name}.{field.Name} has an empty Tooltip.");
                }
            }
        }

        private T Add<T>(T node) where T : NovelGraphNode
        {
            m_graph.AddNode(node);
            return node;
        }
    }
}
