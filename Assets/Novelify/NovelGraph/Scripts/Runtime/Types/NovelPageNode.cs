using System;

namespace NovelGraph
{
    [Serializable]
    [NodeInfo("Novel Page", "Flow/Novel Page", true, true, false, false, false, true,
        description: "Runs a reusable Novel Page and returns through Next when that page reaches End.")]
    public class NovelPageNode : NovelGraphNode
    {
        [ExposedProperty, UnityEngine.Tooltip("Reusable Novel Page asset to run. Pages can be called repeatedly and can call other pages.")]
        public NovelPageAsset page;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            if (page == null)
            {
                throw new InvalidOperationException("Novel Page node has no page asset assigned.");
            }

            return NovelNodeResult.CallPage(page, context.Graph.GetNodeIdFromOutput(id, 0));
        }
    }
}
