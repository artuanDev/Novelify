using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Start Node", "Process/Start", false, true,
        description: "Entry point of the story. Every graph needs one Start Node.")]
    public class StartNode : NovelGraphNode
    {
        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            return NovelNodeResult.Continue(context.Graph.GetNodeIdFromOutput(id, 0));
        }
    }
}
