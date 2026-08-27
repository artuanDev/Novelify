using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Debug Log Value", "Debug/Debug Log Value", true, true, true, false)]
    public class DebugValue : NovelGraphNode
    {
        [ExposedProperty]
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
