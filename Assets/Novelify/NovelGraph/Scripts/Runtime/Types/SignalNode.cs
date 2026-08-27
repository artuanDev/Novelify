namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Raise Signal", "Events/Raise Signal", true, true, false, false, false, true)]
    public class SignalNode : NovelGraphNode
    {
        [ExposedProperty]
        public string signal;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.RaiseSignal(signal);
            return base.Execute(context);
        }
    }
}
