using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Four Choice", "Story/Choice/Four Choices", true, true, false, false, true, true, 4,
        "Shows four decisions, stores the selected zero-based index, and follows its output.")]
    public class MultiChoiceNode : NovelGraphNode
    {
        [ExposedProperty, TextArea(2, 5), Tooltip("Question or situation displayed above the four decisions.")]
        public string prompt;

        [ExposedProperty, Tooltip("Text displayed for the Choice A output.")] public string choiceA = "Choice A";
        [ExposedProperty, Tooltip("Text displayed for the Choice B output.")] public string choiceB = "Choice B";
        [ExposedProperty, Tooltip("Text displayed for the Choice C output.")] public string choiceC = "Choice C";
        [ExposedProperty, Tooltip("Text displayed for the Choice D output.")] public string choiceD = "Choice D";
        [ExposedProperty, Tooltip("State key receiving 0 for A, 1 for B, 2 for C, or 3 for D.")] public string stateKey = "last_choice";

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            NovelChoiceOption[] choices =
            {
                new NovelChoiceOption(choiceA, context.Graph.GetNodeIdFromOutput(id, 0), string.Empty, 0),
                new NovelChoiceOption(choiceB, context.Graph.GetNodeIdFromOutput(id, 1), string.Empty, 1),
                new NovelChoiceOption(choiceC, context.Graph.GetNodeIdFromOutput(id, 2), string.Empty, 2),
                new NovelChoiceOption(choiceD, context.Graph.GetNodeIdFromOutput(id, 3), string.Empty, 3)
            };
            return NovelNodeResult.Choice(prompt, stateKey, choices);
        }

        public override string GetOutputPortName(int index) => $"Choice {(char)('A' + index)}";
    }
}
