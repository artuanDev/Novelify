using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Linq;
using UnityEditor.UIElements;

namespace NovelGraph.Editor
{
    public class NovelGraphView : GraphView
    {
        private NovelGraphAsset m_novelGraph;
        private SerializedObject m_SerializedObject;
        private NovelGraphEditorWindow m_window;

        public NovelGraphEditorWindow window => m_window;

        public List<NovelGraphEditorNode> m_graphNodes;
        public Dictionary<string, NovelGraphEditorNode> m_nodeDictionary;
        public Dictionary<Edge, NovelGraphConnection> m_connectionDictionary;

        private NovelGraphWindowSearchProvider m_searchProvider;

        private StyleSheet m_backgroundStyle;

        public StyleSheet backgroundStyle => m_backgroundStyle;

        public NovelGraphView(SerializedObject serializedObject, NovelGraphEditorWindow window)
        {
            m_SerializedObject = serializedObject;
            m_novelGraph = (NovelGraphAsset)serializedObject.targetObject;
            m_window = window;

            m_graphNodes = new List<NovelGraphEditorNode>();
            m_nodeDictionary = new Dictionary<string, NovelGraphEditorNode>(); 
            m_connectionDictionary = new Dictionary<Edge, NovelGraphConnection>();

            m_searchProvider = ScriptableObject.CreateInstance<NovelGraphWindowSearchProvider>();
            m_searchProvider.graph = this;
            this.nodeCreationRequest = ShowSearchWindow;

            m_backgroundStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Novelify/NovelGraph/Scripts/Editor/USS/NovelGraphEditor.uss");

            styleSheets.Add(m_backgroundStyle);

            GridBackground background = new GridBackground();
            background.name = "Grid";
            Add(background);
            background.SendToBack();

            ContentZoomer zoomer = new ContentZoomer();
            zoomer.maxScale = 2;

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());
            this.AddManipulator(zoomer);

            DrawNodes();
            DrawConnections();

            graphViewChanged += OnGraphViewChangedEvent;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> allPorts = new List<Port>();
            List<Port> ports = new List<Port>();

            foreach (var node in m_graphNodes)
            {
                allPorts.AddRange(node.Ports);
            }

            foreach (Port p in allPorts)
            {
                if(p == startPort) { continue; }
                if(p.node == startPort.node) { continue; }
                if(p.direction == startPort.direction) { continue; }
                if(p.portType == startPort.portType)
                {
                    ports.Add(p);
                }
            }

            return ports;
        }

        private GraphViewChange OnGraphViewChangedEvent(GraphViewChange graphViewChange)
        {
            if(graphViewChange.movedElements != null)
            {
                Undo.RecordObject(m_SerializedObject.targetObject, "Moved Elements");
                foreach (NovelGraphEditorNode editorNode in graphViewChange.movedElements.OfType<NovelGraphEditorNode>())
                {
                    editorNode.SavePosition();
                }
            }

            if(graphViewChange.elementsToRemove != null)
            {
                Undo.RecordObject(m_SerializedObject.targetObject, "Removed Stuff From Graph");

                List<NovelGraphEditorNode> nodes = graphViewChange.elementsToRemove.OfType<NovelGraphEditorNode>().ToList();
                if(nodes.Count > 0)
                {
                    for (int i = nodes.Count - 1; i >= 0; i--)
                    {
                        RemoveNode(nodes[i]);
                    }
                }

                foreach(Edge e in graphViewChange.elementsToRemove.OfType<Edge>())
                {
                    RemoveConnection(e);
                }
            }

            if(graphViewChange.edgesToCreate != null)
            {
                Undo.RecordObject(m_SerializedObject.targetObject, "Added Connections");
                foreach (Edge edge in graphViewChange.edgesToCreate)
                {
                    CreateEdge(edge);
                }
            }

            return graphViewChange;
        }

        private void RemoveConnection(Edge e)
        {
            if(m_connectionDictionary.TryGetValue(e, out NovelGraphConnection connection))
            {
                m_novelGraph.Connections.Remove(connection);
                m_connectionDictionary.Remove(e);
            }
        }

        private void CreateEdge(Edge edge)
        {
            NovelGraphEditorNode inputNode = (NovelGraphEditorNode)edge.input.node;
            int inputIndex = inputNode.Ports.IndexOf(edge.input);

            NovelGraphEditorNode outputNode = (NovelGraphEditorNode)edge.output.node;
            int outputIndex = outputNode.Ports.IndexOf(edge.output);

            NovelGraphConnection connection = new NovelGraphConnection(inputNode.Node.id, inputIndex, outputNode.Node.id, outputIndex);
            m_novelGraph.Connections.Add(connection);
        }

        private void RemoveNode(NovelGraphEditorNode editorNode)
        {
            m_novelGraph.Nodes.Remove(editorNode.Node);
            m_nodeDictionary.Remove(editorNode.Node.id);
            m_graphNodes.Remove(editorNode);
            m_SerializedObject.Update();
        }

        private void DrawNodes()
        {
            foreach (NovelGraphNode node in m_novelGraph.Nodes)
            {
                AddNodeToGraph(node);
            }

            Bind();
        }

        private void DrawConnections()
        {
            if(m_novelGraph.Connections == null) {  return; }

            foreach (NovelGraphConnection connection in m_novelGraph.Connections)
            {
                DrawConnection(connection);
            }
        }

        private void DrawConnection(NovelGraphConnection connection)
        {
            NovelGraphEditorNode inputNode = GetNode(connection.inputPort.nodeId);
            NovelGraphEditorNode outputNode = GetNode(connection.outputPort.nodeId);

            if(inputNode == null) { return; }
            if(outputNode == null) { return; }

            Port inPort = inputNode.Ports.FirstOrDefault(port => port.direction == Direction.Input && port.portType == typeof(PortTypes.FlowPort));
            Port outPort = outputNode.Ports.Where(port => port.direction == Direction.Output && port.portType == typeof(PortTypes.FlowPort)).ElementAtOrDefault(connection.outputPort.portIndex);

            if (inPort == null || outPort == null) { return; }

            Edge edge = inPort.ConnectTo(outPort);
            AddElement(edge);

            m_connectionDictionary.Add(edge, connection);
        }

        private NovelGraphEditorNode GetNode(string nodeId)
        {
            NovelGraphEditorNode node = null;
            m_nodeDictionary.TryGetValue(nodeId, out node);
            return node;
        }

        private void ShowSearchWindow(NodeCreationContext obj)
        {
            m_searchProvider.target = (VisualElement)focusController.focusedElement;
            SearchWindow.Open(new SearchWindowContext(obj.screenMousePosition), m_searchProvider);
        }

        public void Add(NovelGraphNode node)
        {
            Undo.RecordObject(m_SerializedObject.targetObject, "Added Node");
            m_novelGraph.Nodes.Add(node);
            m_SerializedObject.Update();

            AddNodeToGraph(node);

            Bind();
        }

        private void AddNodeToGraph(NovelGraphNode node)
        {
            node.typeName = node.GetType().AssemblyQualifiedName;

            NovelGraphEditorNode editorNode = new NovelGraphEditorNode(node, m_SerializedObject);
            editorNode.styleSheets.Add(m_backgroundStyle);
            editorNode.SetPosition(node.position);
            m_graphNodes.Add(editorNode);
            m_nodeDictionary.Add(node.id, editorNode);

            AddElement(editorNode);
        }

        private void Bind()
        {
            m_SerializedObject.Update();
            this.Bind(m_SerializedObject);
        }
    }
}
