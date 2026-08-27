using System;
using UnityEngine;

namespace NovelGraph
{
    public enum NovelGraphRunnerStatus
    {
        Idle,
        Running,
        WaitingForAdvance,
        WaitingForChoice,
        Completed,
        Faulted
    }

    public sealed class NovelGraphRunner
    {
        private const int MaxAutomaticSteps = 1000;

        private NovelGraphAsset m_graph;
        private NovelGraphContext m_context;
        private NovelNodeResult m_pendingResult;
        private string m_currentNodeId;

        public NovelGraphRunnerStatus Status { get; private set; } = NovelGraphRunnerStatus.Idle;
        public NovelGraphState State { get; } = new NovelGraphState();
        public NovelNodeResult CurrentPresentation => m_pendingResult;
        public string CurrentNodeId => m_currentNodeId ?? string.Empty;

        public event Action<NovelNodeResult> PresentationChanged;
        public event Action<string> SignalRaised;
        public event Action Completed;
        public event Action<string> Faulted;

        public void Start(NovelGraphAsset graph, GameObject owner = null)
        {
            if (graph == null)
            {
                Fail("Cannot start a null Novel Graph.");
                return;
            }

            m_graph = graph;
            m_graph.Init(owner);
            m_context = new NovelGraphContext(graph, State, owner, RaiseSignal);
            State.Clear();

            NovelGraphNode start = graph.GetStartNode();
            if (start == null)
            {
                Fail($"Graph '{graph.name}' does not have a Start node.");
                return;
            }

            m_currentNodeId = start.id;
            Status = NovelGraphRunnerStatus.Running;
            Pump();
        }

        public void Restart()
        {
            if (m_graph != null)
            {
                Start(m_graph, m_context?.Owner);
            }
        }

        public bool Advance()
        {
            if (Status != NovelGraphRunnerStatus.WaitingForAdvance)
            {
                return false;
            }

            m_currentNodeId = m_pendingResult.NextNodeId;
            Status = NovelGraphRunnerStatus.Running;
            Pump();
            return true;
        }

        public bool Choose(int index)
        {
            if (Status != NovelGraphRunnerStatus.WaitingForChoice ||
                index < 0 || index >= m_pendingResult.Choices.Count)
            {
                return false;
            }

            NovelChoiceOption choice = m_pendingResult.Choices[index];
            if (!string.IsNullOrWhiteSpace(m_pendingResult.StateKey))
            {
                State.SetInt(m_pendingResult.StateKey, choice.StateValue);
            }

            RaiseSignal(choice.Signal);
            m_currentNodeId = choice.NextNodeId;
            Status = NovelGraphRunnerStatus.Running;
            Pump();
            return true;
        }

        public NovelGraphSaveData CaptureSaveData()
        {
            return new NovelGraphSaveData
            {
                graphName = m_graph != null ? m_graph.name : string.Empty,
                nodeId = CurrentNodeId,
                state = State.ToList()
            };
        }

        public bool Restore(NovelGraphAsset graph, NovelGraphSaveData data, GameObject owner = null)
        {
            if (graph == null || data == null || string.IsNullOrWhiteSpace(data.nodeId))
            {
                return false;
            }

            m_graph = graph;
            m_graph.Init(owner);
            if (m_graph.GetNode(data.nodeId) == null)
            {
                return false;
            }

            State.Load(data.state);
            m_context = new NovelGraphContext(graph, State, owner, RaiseSignal);
            m_currentNodeId = data.nodeId;
            Status = NovelGraphRunnerStatus.Running;
            Pump();
            return Status != NovelGraphRunnerStatus.Faulted;
        }

        private void Pump()
        {
            for (int step = 0; step < MaxAutomaticSteps; step++)
            {
                if (string.IsNullOrWhiteSpace(m_currentNodeId))
                {
                    Finish();
                    return;
                }

                NovelGraphNode node = m_graph.GetNode(m_currentNodeId);
                if (node == null)
                {
                    Fail($"Graph '{m_graph.name}' cannot find node '{m_currentNodeId}'.");
                    return;
                }

                NovelNodeResult result;
                try
                {
                    result = node.Execute(m_context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    Fail($"Node '{node.GetType().Name}' failed: {exception.Message}");
                    return;
                }

                switch (result.Type)
                {
                    case NovelNodeResultType.Continue:
                        m_currentNodeId = result.NextNodeId;
                        break;
                    case NovelNodeResultType.Dialogue:
                        m_pendingResult = result;
                        Status = NovelGraphRunnerStatus.WaitingForAdvance;
                        PresentationChanged?.Invoke(result);
                        return;
                    case NovelNodeResultType.Choice:
                        if (result.Choices.Count == 0)
                        {
                            Fail($"Choice node '{node.id}' has no usable choices.");
                            return;
                        }

                        m_pendingResult = result;
                        Status = NovelGraphRunnerStatus.WaitingForChoice;
                        PresentationChanged?.Invoke(result);
                        return;
                    case NovelNodeResultType.Complete:
                        Finish();
                        return;
                    default:
                        Fail($"Node '{node.id}' returned an unsupported result.");
                        return;
                }
            }

            Fail($"Graph '{m_graph.name}' exceeded {MaxAutomaticSteps} automatic steps. Check for an unbroken loop.");
        }

        private void RaiseSignal(string signal)
        {
            if (!string.IsNullOrWhiteSpace(signal))
            {
                SignalRaised?.Invoke(signal);
            }
        }

        private void Finish()
        {
            Status = NovelGraphRunnerStatus.Completed;
            m_pendingResult = NovelNodeResult.Complete();
            Completed?.Invoke();
        }

        private void Fail(string message)
        {
            Status = NovelGraphRunnerStatus.Faulted;
            Debug.LogError(message);
            Faulted?.Invoke(message);
        }
    }
}
