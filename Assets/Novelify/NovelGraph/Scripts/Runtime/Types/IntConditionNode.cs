namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Integer Condition", "State/Integer Condition", true, true, false, false, false, true, 2)]
    public class IntConditionNode : NovelGraphNode
    {
        [ExposedProperty]
        public string key;

        [ExposedProperty]
        public int expectedValue = 1;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            int output = context.State.GetInt(key) == expectedValue ? 0 : 1;
            return NovelNodeResult.Continue(context.Graph.GetNodeIdFromOutput(id, output));
        }

        public override string GetOutputPortName(int index) => index == 0 ? "Equal" : "Not Equal";
    }
}
