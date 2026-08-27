using System;
using UnityEngine;

namespace NovelGraph
{
    public sealed class NovelGraphContext
    {
        private readonly Action<string> m_signalHandler;

        public NovelGraphAsset Graph { get; }
        public NovelGraphState State { get; }
        public GameObject Owner { get; }

        internal NovelGraphContext(
            NovelGraphAsset graph,
            NovelGraphState state,
            GameObject owner,
            Action<string> signalHandler)
        {
            Graph = graph;
            State = state;
            Owner = owner;
            m_signalHandler = signalHandler;
        }

        public void RaiseSignal(string signal)
        {
            if (!string.IsNullOrWhiteSpace(signal))
            {
                m_signalHandler?.Invoke(signal.Trim());
            }
        }
    }
}
