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
            Object.DestroyImmediate(m_graph);
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

        private T Add<T>(T node) where T : NovelGraphNode
        {
            m_graph.AddNode(node);
            return node;
        }
    }
}
