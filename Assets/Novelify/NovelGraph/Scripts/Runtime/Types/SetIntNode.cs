namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Set Integer", "State/Set Integer", true, true, false, false, false, true,
        description: "Stores an integer in the current story state, then continues.")]
    public class SetIntNode : NovelGraphNode
    {
        [ExposedProperty, UnityEngine.Tooltip("Case-sensitive story state key to create or replace.")]
        public string key;

        [ExposedProperty, UnityEngine.Tooltip("Integer value stored under the selected key.")]
        public int value;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.State.SetInt(key, value);
            return base.Execute(context);
        }
    }
}
