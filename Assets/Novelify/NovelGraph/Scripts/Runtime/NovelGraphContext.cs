using System;
using UnityEngine;

namespace NovelGraph
{
    public sealed class NovelGraphContext
    {
        private readonly Action<string> m_signalHandler;
        private readonly Action<string> m_functionHandler;

        public NovelGraphAsset Graph { get; }
        public NovelGraphState State { get; }
        public GameObject Owner { get; }

        internal NovelGraphContext(
            NovelGraphAsset graph,
            NovelGraphState state,
            GameObject owner,
            Action<string> signalHandler,
            Action<string> functionHandler)
        {
            Graph = graph;
            State = state;
            Owner = owner;
            m_signalHandler = signalHandler;
            m_functionHandler = functionHandler;
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
    }
}
