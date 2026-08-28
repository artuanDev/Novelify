using System;
using UnityEditor.Experimental.GraphView;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private DropdownField m_namedRerouteDropdown;
        private readonly Dictionary<string, NamedRerouteOutNode> m_namedRerouteChoices =
            new Dictionary<string, NamedRerouteOutNode>();

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
            tooltip = info.description;
            titleContainer.tooltip = info.description;

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

                if (m_graphNode is NamedRerouteInNode &&
                    property.Name == nameof(NamedRerouteInNode.declarationId))
                {
                    DrawNamedRerouteSelector(property);
                    continue;
                }

                if (info.hasVariablePorts)
                {
                    ApplyFieldTooltip(DrawProperty(property.Name), property);
                }
                else
                {
                    ApplyFieldTooltip(DrawDialogueBox(property.Name), property);
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
                        ApplyFieldTooltip(field, property);
                        CreateOutputValuePort(field);
                        //field.RegisterValueChangeCallback(OnFieldChangeCallback);
                    }
                }
            }
            RefreshExpandedState();
        }

        private void DrawNamedRerouteSelector(FieldInfo property)
        {
            TooltipAttribute tooltipAttribute = property.GetCustomAttribute<TooltipAttribute>();
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.tooltip = tooltipAttribute?.tooltip ?? "Select a Named Reroute Declaration.";

            m_namedRerouteDropdown = new DropdownField("Declaration");
            m_namedRerouteDropdown.style.flexGrow = 1f;
            m_namedRerouteDropdown.tooltip = row.tooltip;
            m_namedRerouteDropdown.RegisterValueChangedCallback(change =>
            {
                if (!m_namedRerouteChoices.TryGetValue(change.newValue, out NamedRerouteOutNode declaration))
                {
                    return;
                }

                Undo.RecordObject(m_serializedObject.targetObject, "Select Named Reroute Declaration");
                ((NamedRerouteInNode)m_graphNode).SetDeclaration(declaration);
                EditorUtility.SetDirty(m_serializedObject.targetObject);
                m_serializedObject.Update();
            });
            m_namedRerouteDropdown.RegisterCallback<MouseDownEvent>(
                _ => RefreshNamedRerouteChoices(),
                TrickleDown.TrickleDown);

            Button createButton = new Button(CreateAndSelectNamedRerouteDeclaration)
            {
                text = "+ New"
            };
            createButton.tooltip = "Create a new Named Reroute Declaration near this usage and select it immediately.";
            createButton.style.marginLeft = 4f;

            row.Add(m_namedRerouteDropdown);
            row.Add(createButton);
            extensionContainer.Add(row);
            RefreshNamedRerouteChoices();
        }

        private void RefreshNamedRerouteChoices()
        {
            if (m_namedRerouteDropdown == null || !(m_serializedObject.targetObject is NovelGraphAsset graph))
            {
                return;
            }

            List<NamedRerouteOutNode> declarations = graph.Nodes
                .OfType<NamedRerouteOutNode>()
                .OrderBy(declaration => declaration.routeName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(declaration => declaration.DeclarationId, StringComparer.Ordinal)
                .ToList();
            NamedRerouteInNode usage = (NamedRerouteInNode)m_graphNode;

            if (string.IsNullOrWhiteSpace(usage.declarationId) && !string.IsNullOrWhiteSpace(usage.routeName))
            {
                NamedRerouteOutNode legacyMatch = declarations.FirstOrDefault(declaration =>
                    string.Equals(declaration.routeName?.Trim(), usage.routeName.Trim(), StringComparison.Ordinal));
                if (legacyMatch != null)
                {
                    usage.SetDeclaration(legacyMatch);
                    EditorUtility.SetDirty(graph);
                }
            }

            m_namedRerouteChoices.Clear();
            var labels = new List<string>();
            string selectedLabel = string.Empty;
            foreach (NamedRerouteOutNode declaration in declarations)
            {
                string baseLabel = string.IsNullOrWhiteSpace(declaration.routeName)
                    ? "Unnamed Declaration"
                    : declaration.routeName.Trim();
                string label = baseLabel;
                if (labels.Contains(label))
                {
                    string shortId = declaration.DeclarationId.Substring(0, Math.Min(6, declaration.DeclarationId.Length));
                    label = $"{baseLabel} [{shortId}]";
                }

                labels.Add(label);
                m_namedRerouteChoices[label] = declaration;
                if (string.Equals(usage.declarationId, declaration.DeclarationId, StringComparison.Ordinal))
                {
                    selectedLabel = label;
                }
            }

            if (labels.Count == 0)
            {
                labels.Add("No declarations — click + New");
            }
            else if (string.IsNullOrWhiteSpace(selectedLabel))
            {
                selectedLabel = string.IsNullOrWhiteSpace(usage.declarationId)
                    ? "Select a declaration…"
                    : "Missing declaration";
                labels.Insert(0, selectedLabel);
            }

            m_namedRerouteDropdown.choices = labels;
            m_namedRerouteDropdown.SetValueWithoutNotify(
                string.IsNullOrWhiteSpace(selectedLabel) ? labels[0] : selectedLabel);
        }

        private void CreateAndSelectNamedRerouteDeclaration()
        {
            NovelGraphView graphView = GetFirstAncestorOfType<NovelGraphView>();
            if (graphView == null || !(m_serializedObject.targetObject is NovelGraphAsset graph))
            {
                return;
            }

            var existingNames = new HashSet<string>(
                graph.Nodes.OfType<NamedRerouteOutNode>()
                    .Select(declaration => declaration.routeName ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);
            string routeName = "Named Route";
            for (int suffix = 2; existingNames.Contains(routeName); suffix++)
            {
                routeName = $"Named Route {suffix}";
            }

            var declaration = new NamedRerouteOutNode { routeName = routeName };
            Rect usagePosition = GetPosition();
            declaration.SetPosition(new Rect(
                usagePosition.x + Mathf.Max(320f, usagePosition.width + 100f),
                usagePosition.y,
                240f,
                140f));

            graphView.Add(declaration);
            ((NamedRerouteInNode)m_graphNode).SetDeclaration(declaration);
            EditorUtility.SetDirty(graph);
            m_serializedObject.Update();
            RefreshNamedRerouteChoices();
        }

        private static void ApplyFieldTooltip(PropertyField field, FieldInfo property)
        {
            TooltipAttribute tooltipAttribute = property.GetCustomAttribute<TooltipAttribute>();
            field.tooltip = tooltipAttribute != null
                ? tooltipAttribute.tooltip
                : $"Edit {ObjectNames.NicifyVariableName(property.Name)}.";
            if (field.parent is Port propertyPort)
            {
                propertyPort.tooltip = field.tooltip;
            }
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
            m_inputPort.tooltip = $"Story flow enters {title} here. {tooltip}";
            m_inputPort.portColor = Resources.Load<NovelgraphSettings>("NovelifySettings").inputPortColor;
            m_ports.Add(m_inputPort);
            inputContainer.Add(m_inputPort);
        }

        private void CreateFlowOutputPort(int index)
        {
            Port m_outputPort =InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(PortTypes.FlowPort));
            m_outputPort.portName = m_graphNode.GetOutputPortName(index);
            m_outputPort.tooltip = $"Continue through {m_graphNode.GetOutputPortName(index)}.";
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
            variablePort.tooltip = string.IsNullOrWhiteSpace(field.tooltip)
                ? "Outputs this node value."
                : $"Outputs value: {field.tooltip}";
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
