using System;
using System.Collections.Generic;
using System.Globalization;

namespace NovelGraph
{
    public enum NovelFunctionCallMode
    {
        PlayerBinding,
        ComponentMethod
    }

    public enum NovelFunctionTargetMode
    {
        StoryOwner,
        TargetId,
        GameObjectName,
        Tag
    }

    public enum NovelFunctionArgumentSource
    {
        Constant,
        StoryState
    }

    public enum NovelFunctionArgumentType
    {
        String,
        Integer,
        Float,
        Boolean
    }

    [Serializable]
    public class NovelFunctionArgument
    {
        [UnityEngine.Tooltip("Method parameter name. When every argument is named, arguments are matched by name instead of list order.")]
        public string name;

        [UnityEngine.Tooltip("Whether this input uses a value written here or reads an integer/bool from story state.")]
        public NovelFunctionArgumentSource source;

        [UnityEngine.Tooltip("Runtime type supplied to the component method.")]
        public NovelFunctionArgumentType type;

        [UnityEngine.Tooltip("Story-state key used when Source is Story State. Empty uses the argument name.")]
        public string stateKey;

        [UnityEngine.Tooltip("Constant string value used when Type is String.")]
        public string stringValue;

        [UnityEngine.Tooltip("Constant integer value used when Type is Integer.")]
        public int intValue;

        [UnityEngine.Tooltip("Constant floating-point value used when Type is Float.")]
        public float floatValue;

        [UnityEngine.Tooltip("Constant boolean value used when Type is Boolean.")]
        public bool boolValue;

        internal object Resolve(NovelGraphState state)
        {
            if (source == NovelFunctionArgumentSource.StoryState)
            {
                string key = string.IsNullOrWhiteSpace(stateKey) ? name : stateKey;
                int stateValue = state.GetInt(key);
                switch (type)
                {
                    case NovelFunctionArgumentType.String:
                        return stateValue.ToString(CultureInfo.InvariantCulture);
                    case NovelFunctionArgumentType.Integer:
                        return stateValue;
                    case NovelFunctionArgumentType.Float:
                        return (float)stateValue;
                    case NovelFunctionArgumentType.Boolean:
                        return stateValue != 0;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            switch (type)
            {
                case NovelFunctionArgumentType.String:
                    return stringValue ?? string.Empty;
                case NovelFunctionArgumentType.Integer:
                    return intValue;
                case NovelFunctionArgumentType.Float:
                    return floatValue;
                case NovelFunctionArgumentType.Boolean:
                    return boolValue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public sealed class NovelComponentFunctionCall
    {
        public NovelFunctionTargetMode TargetMode { get; }
        public string Target { get; }
        public string ComponentType { get; }
        public string MethodName { get; }
        public IReadOnlyList<NovelFunctionArgument> Arguments { get; }
        internal NovelGraphState State { get; }

        internal NovelComponentFunctionCall(
            NovelFunctionTargetMode targetMode,
            string target,
            string componentType,
            string methodName,
            IReadOnlyList<NovelFunctionArgument> arguments,
            NovelGraphState state)
        {
            TargetMode = targetMode;
            Target = target ?? string.Empty;
            ComponentType = componentType ?? string.Empty;
            MethodName = methodName ?? string.Empty;
            Arguments = arguments ?? Array.Empty<NovelFunctionArgument>();
            State = state ?? throw new ArgumentNullException(nameof(state));
        }
    }
}
