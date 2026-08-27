using System;
using UnityEditor.Experimental.GraphView;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine.UIElements;
using static NovelGraph.Editor.PortTypes;

namespace NovelGraph.Editor
{
    public class NovelGraphEditorNode : Node
    {
        private NovelGraphNode m_graphNode;

        private Port m_inputPort;
        private List<Port> m_outputPorts;
        private List<Port> m_variablePorts;
        private List<Port> m_outputValuesPorts;

        private List<Port> m_ports;
        private SerializedObject m_serializedObject;
        private SerializedProperty m_serializedProperty;

        public NovelGraphNode Node => m_graphNode;
        public List<Port> Ports => m_ports;

        public NovelGraphEditorNode(NovelGraphNode node, SerializedObject novelGraphObject)
        {
            this.AddToClassList("code-graph-node");

            m_serializedObject = novelGraphObject;
            m_graphNode = node;
            
            Type typeInfo = node.GetType();
            NodeInfoAttribute info = typeInfo.GetCustomAttribute<NodeInfoAttribute>();

            title = info.title;

            m_ports = new List<Port>();
            m_outputPorts = new List<Port>();
            m_variablePorts = new List<Port>();
            m_outputValuesPorts = new List<Port>();

            string[] depths = info.menuItem.Split('/');
            foreach (string depth in depths)
            {
                this.AddToClassList(depth.ToLower().Replace(' ', '-'));
            }

            this.name = typeInfo.Name;

            //We do this so the output is always index 0
            if (info.hasFlowOutput && info.hasChoiceEvent)
            {
                //create buttons for user election
                CreateMultiFlowPorts(info.numberOfOutputs);
            }
            else if(info.hasFlowOutput)
            {
                //continue normally by clicking (to do)
                CreateMultiFlowPorts(info.numberOfOutputs);
            }
            if (info.hasFlowInput)
            {
                CreateFlowInputPort();
            }

            foreach (FieldInfo property in typeInfo.GetFields())
            {
                if (property.GetCustomAttribute<ExposedPropertyAttribute>() == null)
                {
                    continue;
                }

                if (info.hasVariablePorts)
                {
                    DrawProperty(property.Name);
                }
                else
                {
                    DrawDialogueBox(property.Name);
                }
            }
            //Check if node has output variables
            if (info.hasOutputValues)
            {
                foreach (FieldInfo property in typeInfo.GetFields())
                {
                    if (property.GetCustomAttribute<ExposedOutputValue>() is ExposedOutputValue exposedOutputValue)
                    {
                        PropertyField field = DrawProperty(property.Name);
                        CreateOutputValuePort(field);
                        //field.RegisterValueChangeCallback(OnFieldChangeCallback);
                    }
                }
            }
            RefreshExpandedState();
        }

        private void FetchSerializedProperty()
        {
            SerializedProperty nodes = m_serializedObject.FindProperty("m_nodes");
            if (nodes.isArray)
            {
                int size = nodes.arraySize;
                for (int i = 0; i < size; i++)
                {
                    var element = nodes.GetArrayElementAtIndex(i);
                    var elementId = element.FindPropertyRelative("m_guid");
                    if (elementId.stringValue == m_graphNode.id)
                    {
                        m_serializedProperty = element;
                    }
                }
            }
        }
        
        //Need to create draw output value property
        private PropertyField DrawProperty(string propertyName)
        {
            Port variablePort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(PortTypes.VariablePort));
            variablePort.portName = "";
            variablePort.tooltip = "";

            if (m_serializedProperty == null)
            {
                FetchSerializedProperty();
            }

            SerializedProperty prop = m_serializedProperty.FindPropertyRelative(propertyName);
            
            PropertyField field = new PropertyField(prop);
            field.bindingPath = prop.propertyPath;

            inputContainer.Add(variablePort);
            variablePort.portColor = Color.yellow;
            variablePort.Add(field);

            m_ports.Add(variablePort);

            return field;
        }

        private PropertyField DrawDialogueBox(string propertyName)
        {
            if (m_serializedProperty == null)
            {
                FetchSerializedProperty();
            }

            SerializedProperty prop = m_serializedProperty.FindPropertyRelative(propertyName);

            Button button = new Button();
            PropertyField field = new PropertyField(prop);

            field.bindingPath = prop.propertyPath;
            extensionContainer.Add(field);

            return field;
        }

        private void CreateFlowInputPort()
        {
            NovelgraphSettings settings = Resources.Load<NovelgraphSettings>("Assets/Novelify/Resources/NovelifySettings");
            m_inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(PortTypes.FlowPort));
            m_inputPort.portName = "Input";
            m_inputPort.tooltip = "Flow input";
            m_inputPort.portColor = Resources.Load<NovelgraphSettings>("NovelifySettings").inputPortColor;
            m_ports.Add(m_inputPort);
            inputContainer.Add(m_inputPort);
        }

        private void CreateFlowOutputPort(int index)
        {
            Port m_outputPort =InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(PortTypes.FlowPort));
            m_outputPort.portName = m_graphNode.GetOutputPortName(index);
            m_outputPort.tooltip = "Flow output";
            m_outputPort.portColor = Resources.Load<NovelgraphSettings>("NovelifySettings").inputPortColor;
            m_ports.Add(m_outputPort);
            m_outputPorts.Add(m_outputPort);
            outputContainer.Add(m_outputPort);
        }

        private void CreateMultiFlowPorts(int numberOfPorts)
        {
            for (int i = 0; i < numberOfPorts; i++)
            {
                CreateFlowOutputPort(i);
            }
        }

        private void CreateVariablePort(PropertyField field)
        {
            //Create variable ports
           //Port variablePort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(PortTypes.VariablePort));
           //variablePort.portName = field.name;
           //variablePort.tooltip = "";
           //m_ports.Add(variablePort);

            //m_inputPort.Add(variablePort);
            //extensionContainer.Add(variablePort);
        }
        private void CreateOutputValuePort(PropertyField field)
        {
            //Create variable ports
            Port variablePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(PortTypes.VariablePort));
            variablePort.portName = field.name;
            variablePort.tooltip = "";
            m_ports.Add(variablePort);

            outputContainer.Add(variablePort);
            extensionContainer.Add(variablePort);
        }

        private List<PropertyField> GetVariableInputs()
        {
            return null;
        }

        public void SavePosition()
        {
            m_graphNode.SetPosition(GetPosition());
        }
    }
}
