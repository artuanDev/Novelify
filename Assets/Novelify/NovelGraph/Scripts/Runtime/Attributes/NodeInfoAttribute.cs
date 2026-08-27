using System;
using UnityEngine;

namespace NovelGraph
{
    public class NodeInfoAttribute : Attribute
    {
        private string m_nodeTitle;
        private string m_menuItem;
        private bool m_hasFlowInput;
        private bool m_hasFlowOutput;
        private bool m_hasVariablePorts;
        private bool m_hasOutputValues;
        private bool m_hasChoiceEvent;
        private bool m_hasDialogueBox;
        private int m_numberOfOutputs;

        public string title => m_nodeTitle;
        public string menuItem => m_menuItem;
        public bool hasFlowInput => m_hasFlowInput;
        public bool hasFlowOutput => m_hasFlowOutput;
        public bool hasVariablePorts => m_hasVariablePorts;
        public bool hasOutputValues => m_hasOutputValues;
        public bool hasChoiceEvent => m_hasChoiceEvent;
        public bool hasDialogueBox => m_hasDialogueBox;

        public int numberOfOutputs => m_numberOfOutputs;

        public NodeInfoAttribute(string title, string menuItem = "",
            bool hasFlowInputs = true, bool hasFlowOutputs = true,
            bool hasVariablePorts = false, bool hasOutputValues = false,
            bool hasChoiceEvent = false, bool hasDialogueBox = false, 
            int numberOfPorts = 1)
        {
            m_nodeTitle = title;
            m_menuItem = menuItem;
            m_hasFlowInput = hasFlowInputs;
            m_hasFlowOutput = hasFlowOutputs;
            m_hasVariablePorts = hasVariablePorts;
            m_hasOutputValues = hasOutputValues;
            m_hasChoiceEvent = hasChoiceEvent;
            m_hasDialogueBox = hasDialogueBox;
            m_numberOfOutputs = numberOfPorts;
        }
    }
}
