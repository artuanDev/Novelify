using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Two Choice", "Story/Choice/Two Choices", true, true,
        false, false, true, true, 2)]
    public class TwoChoiceNode : NovelGraphNode
    {
        [ExposedProperty, TextArea(2, 5)]
        public string prompt;

        [ExposedProperty]
        public string firstChoice = "Choice A";

        [ExposedProperty]
        public string firstSignal;

        [ExposedProperty]
        public int firstValue = 1;

        [ExposedProperty]
        public string secondChoice = "Choice B";

        [ExposedProperty]
        public string secondSignal;

        [ExposedProperty]
        public int secondValue;

        [ExposedProperty]
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
