using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Debug Log", "Debug/Debug Log Console", true, true, false, true, false, true,
        description: "Writes a message to the Unity Console, then continues.")]
    public class DebugLogNode : NovelGraphNode
    {
        [ExposedProperty, Tooltip("Message written to the Unity Console when this node executes.")]
        public string logMessage;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            Debug.Log(logMessage);
            return base.Execute(context);
        }
    }
}
