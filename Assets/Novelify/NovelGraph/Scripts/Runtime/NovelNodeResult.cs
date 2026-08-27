using System;
using System.Collections.Generic;

namespace NovelGraph
{
    public enum NovelNodeResultType
    {
        Continue,
        Dialogue,
        Choice,
        Complete
    }

    public readonly struct NovelChoiceOption
    {
        public string Label { get; }
        public string NextNodeId { get; }
        public string Signal { get; }
        public int StateValue { get; }

        public NovelChoiceOption(string label, string nextNodeId, string signal, int stateValue)
        {
            Label = label;
            NextNodeId = nextNodeId;
            Signal = signal;
            StateValue = stateValue;
        }
    }

    public readonly struct NovelNodeResult
    {
        public NovelNodeResultType Type { get; }
        public string NextNodeId { get; }
        public string Speaker { get; }
        public string Text { get; }
        public string StateKey { get; }
        public IReadOnlyList<NovelChoiceOption> Choices { get; }

        private NovelNodeResult(
            NovelNodeResultType type,
            string nextNodeId = "",
            string speaker = "",
            string text = "",
            string stateKey = "",
            IReadOnlyList<NovelChoiceOption> choices = null)
        {
            Type = type;
            NextNodeId = nextNodeId ?? string.Empty;
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
            StateKey = stateKey ?? string.Empty;
            Choices = choices ?? Array.Empty<NovelChoiceOption>();
        }

        public static NovelNodeResult Continue(string nextNodeId) =>
            new NovelNodeResult(NovelNodeResultType.Continue, nextNodeId);

        public static NovelNodeResult Dialogue(string speaker, string text, string nextNodeId) =>
            new NovelNodeResult(NovelNodeResultType.Dialogue, nextNodeId, speaker, text);

        public static NovelNodeResult Choice(string prompt, string stateKey, IReadOnlyList<NovelChoiceOption> choices) =>
            new NovelNodeResult(NovelNodeResultType.Choice, text: prompt, stateKey: stateKey, choices: choices);

        public static NovelNodeResult Complete() => new NovelNodeResult(NovelNodeResultType.Complete);
    }
}
