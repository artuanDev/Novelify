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
        private Dictionary<string, NovelGraphNode> m_OutputDictionary;
        private Dictionary<string, NamedRerouteOutNode> m_NamedRerouteIdDictionary;
        private Dictionary<string, NamedRerouteOutNode> m_LegacyNamedRerouteDictionary;

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
            m_OutputDictionary = new Dictionary<string, NovelGraphNode>();
            m_NamedRerouteIdDictionary = new Dictionary<string, NamedRerouteOutNode>(StringComparer.Ordinal);
            m_LegacyNamedRerouteDictionary = new Dictionary<string, NamedRerouteOutNode>(StringComparer.Ordinal);
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

                if (node is NamedRerouteOutNode namedReroute)
                {
                    if (!m_NamedRerouteIdDictionary.TryAdd(namedReroute.DeclarationId, namedReroute))
                    {
                        Debug.LogError($"Graph '{name}' contains duplicate Named Reroute Declaration IDs. Recreate one of the declarations.");
                    }

                    if (!string.IsNullOrWhiteSpace(namedReroute.routeName))
                    {
                        string routeName = namedReroute.routeName.Trim();
                        if (!m_LegacyNamedRerouteDictionary.TryAdd(routeName, namedReroute))
                        {
                            Debug.LogWarning($"Graph '{name}' contains multiple Named Reroute Declarations called '{routeName}'. Stable dropdown references remain unambiguous.");
                        }
                    }
                }
            }

            foreach (NovelGraphConnection connection in m_connections)
            {
                if (!m_NodeDictionary.TryGetValue(connection.inputPort.nodeId, out NovelGraphNode inputNode))
                {
                    continue;
                }

                string key = GetOutputKey(connection.outputPort.nodeId, connection.outputPort.portIndex);
                if (!m_OutputDictionary.TryAdd(key, inputNode))
                {
                    Debug.LogWarning($"Graph '{name}' has multiple connections from the same flow output '{key}'. The first connection will be used.");
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
            m_OutputDictionary.TryGetValue(GetOutputKey(outputNodeId, index), out NovelGraphNode node);
            return node;
        }

        public NamedRerouteOutNode GetNamedRerouteOut(string declarationId, string legacyRouteName = "")
        {
            EnsureInitialized();
            NamedRerouteOutNode declaration = null;
            if (!string.IsNullOrWhiteSpace(declarationId) &&
                m_NamedRerouteIdDictionary.TryGetValue(declarationId.Trim(), out declaration))
            {
                return declaration;
            }

            string fallbackName = string.IsNullOrWhiteSpace(legacyRouteName) ? declarationId : legacyRouteName;
            if (!string.IsNullOrWhiteSpace(fallbackName))
            {
                m_LegacyNamedRerouteDictionary.TryGetValue(fallbackName.Trim(), out declaration);
            }

            return declaration;
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
            m_OutputDictionary = null;
            m_NamedRerouteIdDictionary = null;
            m_LegacyNamedRerouteDictionary = null;
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
            if (m_NodeDictionary == null || m_OutputDictionary == null ||
                m_NamedRerouteIdDictionary == null || m_LegacyNamedRerouteDictionary == null)
            {
                Init(gameObject);
            }
        }

        private static string GetOutputKey(string nodeId, int portIndex) => $"{nodeId}:{portIndex}";
    }
}
