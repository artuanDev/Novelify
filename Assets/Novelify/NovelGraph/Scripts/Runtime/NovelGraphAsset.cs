using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace NovelGraph
{
    [CreateAssetMenu(menuName = "Novel Graph/New Novel Graph")]
    public class NovelGraphAsset : ScriptableObject
    {
        [SerializeReference]
        public List<NovelGraphNode> m_nodes;
        [SerializeField]
        private List<NovelGraphConnection> m_connections;

        public List<NovelGraphNode> Nodes => m_nodes;
        public List<NovelGraphConnection> Connections => m_connections;

        private Dictionary<string, NovelGraphNode> m_NodeDictionary;

        public GameObject gameObject;

        public NovelGraphAsset()
        {
            m_nodes = new List<NovelGraphNode>();
            m_connections = new List<NovelGraphConnection>();
        }

        public void Init(GameObject owner)
        {
            gameObject = owner;
            m_NodeDictionary = new Dictionary<string, NovelGraphNode>();
            foreach (NovelGraphNode node in Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id))
                {
                    Debug.LogWarning($"Graph '{name}' contains an invalid node and will skip it.");
                    continue;
                }

                if (!m_NodeDictionary.TryAdd(node.id, node))
                {
                    Debug.LogError($"Graph '{name}' contains the duplicate node id '{node.id}'.");
                }
            }
        }

        public NovelGraphNode GetStartNode()
        {
            StartNode[] startNodes = Nodes.OfType<StartNode>().ToArray();
            if(startNodes.Length == 0)
            {
                Debug.LogError("There is no start node in this graph");
                return null; 
            }
            return startNodes[0];
        }

        public NovelGraphNode GetNode(string nextNodeId)
        {
            EnsureInitialized();
            if (m_NodeDictionary.TryGetValue(nextNodeId, out NovelGraphNode node))
            {
                return node;
            }
            return null;
        }

        public NovelGraphNode GetNodeFromOutput(string outputNodeId, int index)
        {
            EnsureInitialized();
            foreach (NovelGraphConnection connection in m_connections)
            {
                if(connection.outputPort.nodeId == outputNodeId && connection.outputPort.portIndex == index)
                {
                    string nodeId = connection.inputPort.nodeId;
                    return GetNode(nodeId);
                }
            }

            return null;
        }

        public string GetNodeIdFromOutput(string outputNodeId, int index)
        {
            return GetNodeFromOutput(outputNodeId, index)?.id ?? string.Empty;
        }

        public void AddNode(NovelGraphNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            m_nodes.Add(node);
            m_NodeDictionary = null;
        }

        public void Connect(NovelGraphNode outputNode, int outputIndex, NovelGraphNode inputNode, int inputIndex = -1)
        {
            if (outputNode == null || inputNode == null)
            {
                throw new ArgumentNullException(outputNode == null ? nameof(outputNode) : nameof(inputNode));
            }

            int resolvedInputIndex = inputIndex >= 0 ? inputIndex : inputNode.GetFlowInputPortIndex();
            m_connections.Add(new NovelGraphConnection(inputNode.id, resolvedInputIndex, outputNode.id, outputIndex));
        }

        private void EnsureInitialized()
        {
            if (m_NodeDictionary == null)
            {
                Init(gameObject);
            }
        }
    }
}
