using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Debug Log", "Debug/Debug Log Console",true, true,false, true, false, true)]
    public class DebugLogNode : NovelGraphNode
    {
        [ExposedProperty()]
        public string logMessage;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            Debug.Log(logMessage);
            return base.Execute(context);
        }
    }
}
