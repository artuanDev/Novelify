using System;
using System.Linq;
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

        private readonly struct NovelGraphCallFrame
        {
            public NovelGraphAsset Graph { get; }
            public string ReturnNodeId { get; }

            public NovelGraphCallFrame(NovelGraphAsset graph, string returnNodeId)
            {
                Graph = graph;
                ReturnNodeId = returnNodeId ?? string.Empty;
            }
        }

        private NovelGraphAsset m_graph;
        private NovelGraphAsset m_currentGraph;
        private NovelGraphContext m_context;
        private NovelNodeResult m_pendingResult;
        private string m_currentNodeId;
        private readonly System.Collections.Generic.List<NovelGraphCallFrame> m_callStack =
            new System.Collections.Generic.List<NovelGraphCallFrame>();

        public NovelGraphRunnerStatus Status { get; private set; } = NovelGraphRunnerStatus.Idle;
        public NovelGraphState State { get; } = new NovelGraphState();
        public NovelNodeResult CurrentPresentation => m_pendingResult;
        public string CurrentNodeId => m_currentNodeId ?? string.Empty;

        public event Action<NovelNodeResult> PresentationChanged;
        public event Action<string> SignalRaised;
        public event Action<string> FunctionRequested;
        public event Action<NovelComponentFunctionCall> ComponentFunctionRequested;
        public event Action<NovelCharacterStageCommand> CharacterStageRequested;
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
            m_currentGraph = graph;
            m_callStack.Clear();
            m_graph.Init(owner);
            m_context = new NovelGraphContext(
                graph,
                State,
                owner,
                RaiseSignal,
                RequestFunction,
                RequestComponentFunction,
                RequestCharacterStage);
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
                currentGraphId = GetGraphId(m_currentGraph),
                nodeId = CurrentNodeId,
                state = State.ToList(),
                callStack = CaptureCallStack()
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
            m_callStack.Clear();

            if (data.version >= 2 && data.callStack != null)
            {
                foreach (NovelGraphCallFrameSaveData savedFrame in data.callStack)
                {
                    NovelGraphAsset caller = ResolveGraph(savedFrame.graphId);
                    if (caller == null ||
                        (!string.IsNullOrWhiteSpace(savedFrame.returnNodeId) && caller.GetNode(savedFrame.returnNodeId) == null))
                    {
                        return false;
                    }

                    caller.Init(owner);
                    m_callStack.Add(new NovelGraphCallFrame(caller, savedFrame.returnNodeId));
                }
            }

            m_currentGraph = data.version >= 2 ? ResolveGraph(data.currentGraphId) : m_graph;
            if (m_currentGraph == null)
            {
                return false;
            }

            m_currentGraph.Init(owner);
            if (m_currentGraph.GetNode(data.nodeId) == null)
            {
                return false;
            }

            State.Load(data.state);
            m_context = new NovelGraphContext(
                m_currentGraph,
                State,
                owner,
                RaiseSignal,
                RequestFunction,
                RequestComponentFunction,
                RequestCharacterStage);
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
                    if (ReturnFromPageOrFinish())
                    {
                        continue;
                    }

                    return;
                }

                NovelGraphNode node = m_currentGraph.GetNode(m_currentNodeId);
                if (node == null)
                {
                    Fail($"Graph '{m_currentGraph.name}' cannot find node '{m_currentNodeId}'.");
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
                    case NovelNodeResultType.NovelPage:
                        if (!EnterPage(result.Page, result.NextNodeId))
                        {
                            return;
                        }

                        break;
                    case NovelNodeResultType.Complete:
                        if (ReturnFromPageOrFinish())
                        {
                            break;
                        }

                        return;
                    default:
                        Fail($"Node '{node.id}' returned an unsupported result.");
                        return;
                }
            }

            Fail($"Graph '{m_currentGraph.name}' exceeded {MaxAutomaticSteps} automatic steps. Check for an unbroken loop or recursive page call.");
        }

        private void RaiseSignal(string signal)
        {
            if (!string.IsNullOrWhiteSpace(signal))
            {
                SignalRaised?.Invoke(signal);
            }
        }

        private void RequestFunction(string functionId)
        {
            if (!string.IsNullOrWhiteSpace(functionId))
            {
                FunctionRequested?.Invoke(functionId);
            }
        }

        private void RequestComponentFunction(NovelComponentFunctionCall functionCall)
        {
            ComponentFunctionRequested?.Invoke(functionCall);
        }

        private void RequestCharacterStage(NovelCharacterStageCommand command)
        {
            CharacterStageRequested?.Invoke(command);
        }

        private void Finish()
        {
            Status = NovelGraphRunnerStatus.Completed;
            m_pendingResult = NovelNodeResult.Complete();
            Completed?.Invoke();
        }

        private bool EnterPage(NovelPageAsset page, string returnNodeId)
        {
            if (page == null)
            {
                Fail("Cannot call a null Novel Page.");
                return false;
            }

            page.Init(m_context.Owner);
            NovelGraphNode start = page.GetStartNode();
            if (start == null)
            {
                Fail($"Novel Page '{page.name}' does not have a Start node.");
                return false;
            }

            m_callStack.Add(new NovelGraphCallFrame(m_currentGraph, returnNodeId));
            m_currentGraph = page;
            m_context.SetGraph(page);
            m_currentNodeId = start.id;
            return true;
        }

        private bool ReturnFromPageOrFinish()
        {
            if (m_callStack.Count == 0)
            {
                Finish();
                return false;
            }

            int lastIndex = m_callStack.Count - 1;
            NovelGraphCallFrame frame = m_callStack[lastIndex];
            m_callStack.RemoveAt(lastIndex);
            m_currentGraph = frame.Graph;
            m_context.SetGraph(m_currentGraph);
            m_currentNodeId = frame.ReturnNodeId;
            return true;
        }

        private System.Collections.Generic.List<NovelGraphCallFrameSaveData> CaptureCallStack()
        {
            var frames = new System.Collections.Generic.List<NovelGraphCallFrameSaveData>(m_callStack.Count);
            foreach (NovelGraphCallFrame frame in m_callStack)
            {
                frames.Add(new NovelGraphCallFrameSaveData
                {
                    graphId = GetGraphId(frame.Graph),
                    returnNodeId = frame.ReturnNodeId
                });
            }

            return frames;
        }

        private string GetGraphId(NovelGraphAsset graph)
        {
            if (graph == null || ReferenceEquals(graph, m_graph))
            {
                return "$root";
            }

            return graph is NovelPageAsset page ? page.PageId : graph.name;
        }

        private NovelGraphAsset ResolveGraph(string graphId)
        {
            if (string.IsNullOrWhiteSpace(graphId) || string.Equals(graphId, "$root", StringComparison.Ordinal))
            {
                return m_graph;
            }

            var visited = new System.Collections.Generic.HashSet<NovelGraphAsset>();
            var pending = new System.Collections.Generic.Stack<NovelGraphAsset>();
            pending.Push(m_graph);
            while (pending.Count > 0)
            {
                NovelGraphAsset graph = pending.Pop();
                if (graph == null || !visited.Add(graph))
                {
                    continue;
                }

                foreach (NovelPageNode pageNode in graph.Nodes.OfType<NovelPageNode>())
                {
                    NovelPageAsset page = pageNode.page;
                    if (page == null)
                    {
                        continue;
                    }

                    if (string.Equals(page.PageId, graphId, StringComparison.Ordinal) ||
                        string.Equals(page.name, graphId, StringComparison.Ordinal))
                    {
                        return page;
                    }

                    pending.Push(page);
                }
            }

            return null;
        }

        private void Fail(string message)
        {
            Status = NovelGraphRunnerStatus.Faulted;
            Debug.LogError(message);
            Faulted?.Invoke(message);
        }
    }
}
