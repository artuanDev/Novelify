using System;
using UnityEngine;

namespace NovelGraph
{
    public sealed class NovelGraphContext
    {
        private readonly Action<string> m_signalHandler;
        private readonly Action<string> m_functionHandler;
        private readonly Action<NovelComponentFunctionCall> m_componentFunctionHandler;
        private readonly Action<NovelCharacterStageCommand> m_characterStageHandler;

        public NovelGraphAsset Graph { get; private set; }
        public NovelGraphState State { get; }
        public GameObject Owner { get; }

        internal NovelGraphContext(
            NovelGraphAsset graph,
            NovelGraphState state,
            GameObject owner,
            Action<string> signalHandler,
            Action<string> functionHandler,
            Action<NovelComponentFunctionCall> componentFunctionHandler,
            Action<NovelCharacterStageCommand> characterStageHandler)
        {
            Graph = graph;
            State = state;
            Owner = owner;
            m_signalHandler = signalHandler;
            m_functionHandler = functionHandler;
            m_componentFunctionHandler = componentFunctionHandler;
            m_characterStageHandler = characterStageHandler;
        }

        internal void SetGraph(NovelGraphAsset graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public void RaiseSignal(string signal)
        {
            if (!string.IsNullOrWhiteSpace(signal))
            {
                m_signalHandler?.Invoke(signal.Trim());
            }
        }

        public void CallFunction(string functionId)
        {
            if (!string.IsNullOrWhiteSpace(functionId))
            {
                m_functionHandler?.Invoke(functionId.Trim());
            }
        }

        public void CallFunction(NovelComponentFunctionCall functionCall)
        {
            if (functionCall == null)
            {
                throw new ArgumentNullException(nameof(functionCall));
            }

            m_componentFunctionHandler?.Invoke(functionCall);
        }

        public void StageCharacter(NovelCharacterStageCommand command)
        {
            m_characterStageHandler?.Invoke(command);
        }
    }
}
