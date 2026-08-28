using System;

namespace NovelGraph
{
    [Serializable]
    [NodeInfo("Reroute", "Flow/Reroute", true, true,
        description: "Passes story flow through a compact organizing node.")]
    public class RerouteNode : NovelGraphNode
    {
    }

    [Serializable]
    [NodeInfo("Named Reroute Usage", "Flow/Named Reroute Usage", true, false,
        description: "Sends story flow to a selected Named Reroute Declaration without requiring a long wire.")]
    public class NamedRerouteInNode : NovelGraphNode
    {
        [ExposedProperty, UnityEngine.Tooltip("Named Reroute Declaration selected from this graph. The editor stores its stable ID, so renaming the declaration is safe.")]
        public string declarationId;

        [UnityEngine.SerializeField, UnityEngine.HideInInspector]
        public string routeName;

        public void SetDeclaration(NamedRerouteOutNode declaration)
        {
            declarationId = declaration?.DeclarationId ?? string.Empty;
            routeName = declaration?.routeName ?? string.Empty;
        }

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            if (string.IsNullOrWhiteSpace(declarationId) && string.IsNullOrWhiteSpace(routeName))
            {
                throw new InvalidOperationException("Named Reroute Usage needs a declaration selected.");
            }

            NamedRerouteOutNode destination = context.Graph.GetNamedRerouteOut(declarationId, routeName);
            if (destination == null)
            {
                string reference = string.IsNullOrWhiteSpace(routeName) ? declarationId : routeName;
                throw new InvalidOperationException($"Named Reroute Declaration '{reference}' was not found in graph '{context.Graph.name}'.");
            }

            return NovelNodeResult.Continue(context.Graph.GetNodeIdFromOutput(destination.id, 0));
        }
    }

    [Serializable]
    [NodeInfo("Named Reroute Declaration", "Flow/Named Reroute Declaration", false, true,
        description: "Declares a reusable flow destination that Named Reroute Usage nodes select from a dropdown.")]
    public class NamedRerouteOutNode : NovelGraphNode
    {
        [UnityEngine.SerializeField, UnityEngine.HideInInspector]
        private string m_declarationId;

        [ExposedProperty, UnityEngine.Tooltip("Friendly declaration name shown in Named Reroute Usage dropdowns. Renaming it does not break existing usages.")]
        public string routeName = "Named Route";

        public string DeclarationId
        {
            get
            {
                EnsureDeclarationId();
                return m_declarationId;
            }
        }

        public NamedRerouteOutNode()
        {
            EnsureDeclarationId();
        }

        private void EnsureDeclarationId()
        {
            if (string.IsNullOrWhiteSpace(m_declarationId))
            {
                m_declarationId = Guid.NewGuid().ToString("N");
            }
        }
    }
}
