using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NovelGraph
{
    public static class NovelFunctionInvoker
    {
        public static object Invoke(NovelComponentFunctionCall call, GameObject storyOwner)
        {
            if (call == null)
            {
                throw new ArgumentNullException(nameof(call));
            }

            GameObject target = ResolveTarget(call, storyOwner);
            if (target == null)
            {
                throw new InvalidOperationException($"Novelify could not resolve function target '{call.Target}' using {call.TargetMode}.");
            }

            if (string.IsNullOrWhiteSpace(call.MethodName))
            {
                throw new InvalidOperationException("Component Method Call Function needs a method name.");
            }

            Component[] components = target.GetComponents<Component>();
            if (!string.IsNullOrWhiteSpace(call.ComponentType))
            {
                components = components.Where(component =>
                    component != null && MatchesComponentType(component.GetType(), call.ComponentType.Trim())).ToArray();
            }

            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                MethodInfo[] methods = component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(method => !method.IsSpecialName && !method.IsGenericMethodDefinition &&
                                     string.Equals(method.Name, call.MethodName.Trim(), StringComparison.Ordinal))
                    .OrderBy(method => method.MetadataToken)
                    .ToArray();

                foreach (MethodInfo method in methods)
                {
                    if (!TryBuildArguments(method, call, out object[] values))
                    {
                        continue;
                    }

                    try
                    {
                        return method.Invoke(component, values);
                    }
                    catch (TargetInvocationException exception) when (exception.InnerException != null)
                    {
                        throw new InvalidOperationException(
                            $"{component.GetType().Name}.{method.Name} failed: {exception.InnerException.Message}",
                            exception.InnerException);
                    }
                }
            }

            string componentLabel = string.IsNullOrWhiteSpace(call.ComponentType) ? "any component" : call.ComponentType;
            throw new MissingMethodException(
                $"Novelify could not find a compatible public method '{call.MethodName}' on {componentLabel} at '{target.name}'.");
        }

        private static GameObject ResolveTarget(NovelComponentFunctionCall call, GameObject storyOwner)
        {
            switch (call.TargetMode)
            {
                case NovelFunctionTargetMode.StoryOwner:
                    return storyOwner;
                case NovelFunctionTargetMode.TargetId:
                    return NovelFunctionTarget.FindTarget(call.Target);
                case NovelFunctionTargetMode.GameObjectName:
                    return string.IsNullOrWhiteSpace(call.Target) ? null : GameObject.Find(call.Target.Trim());
                case NovelFunctionTargetMode.Tag:
                    if (string.IsNullOrWhiteSpace(call.Target))
                    {
                        return null;
                    }

                    try
                    {
                        return GameObject.FindWithTag(call.Target.Trim());
                    }
                    catch (UnityException exception)
                    {
                        throw new InvalidOperationException($"Novelify function target tag '{call.Target}' is not defined.", exception);
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool MatchesComponentType(Type componentType, string requestedType)
        {
            return string.Equals(componentType.Name, requestedType, StringComparison.Ordinal) ||
                   string.Equals(componentType.FullName, requestedType, StringComparison.Ordinal) ||
                   string.Equals(componentType.AssemblyQualifiedName, requestedType, StringComparison.Ordinal);
        }

        private static bool TryBuildArguments(MethodInfo method, NovelComponentFunctionCall call, out object[] values)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != call.Arguments.Count)
            {
                values = null;
                return false;
            }

            values = new object[parameters.Length];
            bool matchByName = call.Arguments.Count > 0 && call.Arguments.All(argument =>
                argument != null && !string.IsNullOrWhiteSpace(argument.name));

            for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                int argumentIndex = parameterIndex;
                if (matchByName)
                {
                    argumentIndex = FindArgument(call.Arguments, parameters[parameterIndex].Name);
                    if (argumentIndex < 0)
                    {
                        return false;
                    }
                }

                NovelFunctionArgument argument = call.Arguments[argumentIndex];
                if (argument == null || !TryConvert(argument.Resolve(call.State), parameters[parameterIndex].ParameterType, out values[parameterIndex]))
                {
                    return false;
                }
            }

            return true;
        }

        private static int FindArgument(IReadOnlyList<NovelFunctionArgument> arguments, string parameterName)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                if (string.Equals(arguments[i].name.Trim(), parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryConvert(object value, Type targetType, out object converted)
        {
            Type effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (value == null)
            {
                converted = null;
                return !effectiveType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
            }

            if (effectiveType.IsInstanceOfType(value))
            {
                converted = value;
                return true;
            }

            try
            {
                if (effectiveType.IsEnum)
                {
                    converted = value is string text
                        ? Enum.Parse(effectiveType, text, true)
                        : Enum.ToObject(effectiveType, value);
                    return true;
                }

                converted = Convert.ChangeType(value, effectiveType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception exception) when (exception is InvalidCastException || exception is FormatException || exception is OverflowException)
            {
                converted = null;
                return false;
            }
        }
    }
}
