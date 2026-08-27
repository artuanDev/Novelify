namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Set Integer", "State/Set Integer", true, true, false, false, false, true)]
    public class SetIntNode : NovelGraphNode
    {
        [ExposedProperty]
        public string key;

        [ExposedProperty]
        public int value;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.State.SetInt(key, value);
            return base.Execute(context);
        }
    }
}
