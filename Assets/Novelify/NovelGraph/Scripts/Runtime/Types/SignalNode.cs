namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Raise Signal", "Events/Raise Signal", true, true, false, false, false, true,
        description: "Broadcasts a named signal with its string value to gameplay listeners.")]
    public class SignalNode : NovelGraphNode
    {
        [ExposedProperty, UnityEngine.Tooltip("Signal name sent through Novel Graph Player > On Signal.")]
        public string signal;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.RaiseSignal(signal);
            return base.Execute(context);
        }
    }
}
