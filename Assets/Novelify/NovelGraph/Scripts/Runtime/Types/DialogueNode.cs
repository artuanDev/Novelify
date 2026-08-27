using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Dialogue", "Story/Dialogue", true, true, false, false, false, true)]
    public class DialogueNode : NovelGraphNode
    {
        [ExposedProperty]
        public string speaker;

        [ExposedProperty, TextArea(3, 9)]
        public string dialogue;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            return NovelNodeResult.Dialogue(speaker, dialogue, context.Graph.GetNodeIdFromOutput(id, 0));
        }
    }
}
