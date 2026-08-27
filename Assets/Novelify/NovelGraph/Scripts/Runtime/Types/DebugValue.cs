using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Debug Log Value", "Debug/Debug Log Value", true, true, true, false,
        description: "Copies and logs an integer for graph debugging.")]
    public class DebugValue : NovelGraphNode
    {
        [ExposedProperty, Tooltip("Integer copied to Final Value and written to the Console.")]
        public int value;

        private int m_finalValue;
        public int finalValue => m_finalValue;
        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            Debug.Log("value was " + value);

            m_finalValue = value;

            Debug.Log("Final value is " +  m_finalValue);

            return base.Execute(context);
        }
    }
}
