using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Two Choice", "Story/Choice/Two Choices", true, true,
        false, false, true, true, 2, "Shows two decisions, stores the selected value, raises its signal, and follows its output.")]
    public class TwoChoiceNode : NovelGraphNode
    {
        [ExposedProperty, TextArea(2, 5), Tooltip("Question or situation displayed above the decisions.")]
        public string prompt;

        [ExposedProperty, Tooltip("Text displayed for Choice A.")]
        public string firstChoice = "Choice A";

        [ExposedProperty, Tooltip("Signal raised immediately when Choice A is selected. Leave empty for none.")]
        public string firstSignal;

        [ExposedProperty, Tooltip("Integer stored when Choice A is selected.")]
        public int firstValue = 1;

        [ExposedProperty, Tooltip("Text displayed for Choice B.")]
        public string secondChoice = "Choice B";

        [ExposedProperty, Tooltip("Signal raised immediately when Choice B is selected. Leave empty for none.")]
        public string secondSignal;

        [ExposedProperty, Tooltip("Integer stored when Choice B is selected.")]
        public int secondValue;

        [ExposedProperty, Tooltip("State key that receives the selected choice value.")]
        public string stateKey = "last_choice";

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            NovelChoiceOption[] choices =
            {
                new NovelChoiceOption(firstChoice, context.Graph.GetNodeIdFromOutput(id, 0), firstSignal, firstValue),
                new NovelChoiceOption(secondChoice, context.Graph.GetNodeIdFromOutput(id, 1), secondSignal, secondValue)
            };
            return NovelNodeResult.Choice(prompt, stateKey, choices);
        }

        public override string GetOutputPortName(int index) => index == 0 ? "Choice A" : "Choice B";
    }
}
