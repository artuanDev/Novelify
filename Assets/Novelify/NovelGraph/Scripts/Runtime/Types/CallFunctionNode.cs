namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Call Function", "Events/Call Function", true, true, false, false, false, true,
        description: "Invokes the matching UnityEvent binding on the active Novel Graph Player.")]
    public class CallFunctionNode : NovelGraphNode
    {
        [ExposedProperty, UnityEngine.Tooltip("Binding ID configured in Novel Graph Player > Function Bindings.")]
        public string functionId;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.CallFunction(functionId);
            return base.Execute(context);
        }
    }
}
