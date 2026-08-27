namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Integer Condition", "State/Integer Condition", true, true, false, false, false, true, 2,
        "Compares a stored integer and follows either Equal or Not Equal.")]
    public class IntConditionNode : NovelGraphNode
    {
        [ExposedProperty, UnityEngine.Tooltip("Case-sensitive state key to read. A missing key has value zero.")]
        public string key;

        [ExposedProperty, UnityEngine.Tooltip("Value required to follow the Equal output.")]
        public int expectedValue = 1;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            int output = context.State.GetInt(key) == expectedValue ? 0 : 1;
            return NovelNodeResult.Continue(context.Graph.GetNodeIdFromOutput(id, output));
        }

        public override string GetOutputPortName(int index) => index == 0 ? "Equal" : "Not Equal";
    }
}
