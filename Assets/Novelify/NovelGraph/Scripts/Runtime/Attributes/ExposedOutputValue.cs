using System;
using UnityEngine;

namespace NovelGraph
{
    public class ExposedOutputValue : Attribute
    {
        private string m_tooltip;

        public string tooltip => m_tooltip;

        public ExposedOutputValue(string tooltip = "")
        {
            m_tooltip = tooltip;
        }
    }
}
