namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("End", "Process/End", true, false,
        description: "Ends graph execution and invokes the player's story-completed event.")]
    public class EndNode : NovelGraphNode
    {
        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            return NovelNodeResult.Complete();
        }
    }
}
