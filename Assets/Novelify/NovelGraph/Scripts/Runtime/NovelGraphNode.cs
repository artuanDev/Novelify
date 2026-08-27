using System;
using System.Reflection;
using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    public class NovelGraphNode 
    {
        [SerializeField]
        private string m_guid;
        [SerializeField]
        private Rect m_position;

        public string typeName;

        public string id => m_guid;
        public Rect position => m_position;

        public NovelGraphNode()
        {
            NewGUID();
        }

        private void NewGUID()
        {
            m_guid = Guid.NewGuid().ToString();
        }

        public void SetPosition(Rect position)
        {
            m_position = position;
        }
        
        public virtual string OnProcess(NovelGraphAsset currentGraph)
        {
            NovelGraphNode nextNodeInFlow = currentGraph.GetNodeFromOutput(m_guid, 0);

            if (nextNodeInFlow != null)
            {
                return nextNodeInFlow.id;
            }

            return string.Empty;
        }

        public virtual NovelNodeResult Execute(NovelGraphContext context)
        {
            return NovelNodeResult.Continue(context.Graph.GetNodeIdFromOutput(id, 0));
        }

        public virtual string GetOutputPortName(int index)
        {
            return "Next";
        }

        public int GetFlowInputPortIndex()
        {
            NodeInfoAttribute info = GetType().GetCustomAttribute<NodeInfoAttribute>();
            return info != null && info.hasFlowOutput ? info.numberOfOutputs : 0;
        }
    }
}
