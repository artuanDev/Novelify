using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Start Node", "Process/Start", false, true)]
    public class StartNode : NovelGraphNode
    {
        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            return NovelNodeResult.Continue(context.Graph.GetNodeIdFromOutput(id, 0));
        }
    }
}
