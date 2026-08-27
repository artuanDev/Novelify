namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("End", "Process/End", true, false)]
    public class EndNode : NovelGraphNode
    {
        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            return NovelNodeResult.Complete();
        }
    }
}
