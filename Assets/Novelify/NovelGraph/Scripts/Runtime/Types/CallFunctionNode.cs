using System.Collections.Generic;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Call Function", "Events/Call Function", true, true, false, false, false, true,
        description: "Invokes a player UnityEvent binding or a typed public method on a scene GameObject component.")]
    public class CallFunctionNode : NovelGraphNode
    {
        [ExposedProperty, UnityEngine.Tooltip("Player Binding preserves the UnityEvent workflow. Component Method invokes a public method on a scene component.")]
        public NovelFunctionCallMode callMode;

        [ExposedProperty, UnityEngine.Tooltip("Binding ID configured in Novel Graph Player > Function Bindings.")]
        public string functionId;

        [ExposedProperty, UnityEngine.Tooltip("How the scene GameObject is resolved for a Component Method call.")]
        public NovelFunctionTargetMode targetMode;

        [ExposedProperty, UnityEngine.Tooltip("Target ID, GameObject name, or tag. Ignored when Target Mode is Story Owner.")]
        public string target;

        [ExposedProperty, UnityEngine.Tooltip("Component class name, full name, or assembly-qualified name. Leave empty to search all components on the target.")]
        public string componentType;

        [ExposedProperty, UnityEngine.Tooltip("Case-sensitive public instance method to invoke on the selected component.")]
        public string methodName;

        [ExposedProperty, UnityEngine.Tooltip("Typed method inputs. Name all entries to match method parameter names, or leave names empty to use list order.")]
        public List<NovelFunctionArgument> arguments = new List<NovelFunctionArgument>();

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            if (callMode == NovelFunctionCallMode.PlayerBinding)
            {
                context.CallFunction(functionId);
            }
            else
            {
                context.CallFunction(new NovelComponentFunctionCall(
                    targetMode,
                    target,
                    componentType,
                    methodName,
                    arguments,
                    context.State));
            }

            return base.Execute(context);
        }
    }
}
