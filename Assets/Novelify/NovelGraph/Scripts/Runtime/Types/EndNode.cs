namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("End", "Process/End", true, false,
        description: "Returns from the current Novel Page, or completes the story when used in the root graph.")]
    public class EndNode : NovelGraphNode
    {
        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            return NovelNodeResult.Complete();
        }
    }
}
