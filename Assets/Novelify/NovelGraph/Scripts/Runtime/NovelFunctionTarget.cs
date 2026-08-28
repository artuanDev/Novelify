using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovelGraph
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Novelify/Function Target")]
    public sealed class NovelFunctionTarget : MonoBehaviour
    {
        private static readonly Dictionary<string, List<NovelFunctionTarget>> s_targets =
            new Dictionary<string, List<NovelFunctionTarget>>(StringComparer.Ordinal);

        [SerializeField, Tooltip("Stable, case-sensitive ID used by Component Method Call Function nodes.")]
        private string m_targetId;

        public string TargetId => m_targetId;

        public void SetTargetId(string targetId)
        {
            bool wasActive = isActiveAndEnabled;
            if (wasActive)
            {
                Unregister();
            }

            m_targetId = targetId == null ? string.Empty : targetId.Trim();
            if (wasActive)
            {
                Register();
            }
        }

        internal static GameObject FindTarget(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId) ||
                !s_targets.TryGetValue(targetId.Trim(), out List<NovelFunctionTarget> targets))
            {
                return null;
            }

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                NovelFunctionTarget target = targets[i];
                if (target == null)
                {
                    targets.RemoveAt(i);
                    continue;
                }

                if (target.isActiveAndEnabled)
                {
                    return target.gameObject;
                }
            }

            return null;
        }

        private void OnEnable() => Register();

        private void OnDisable() => Unregister();

        private void OnValidate()
        {
            m_targetId = m_targetId == null ? string.Empty : m_targetId.Trim();
        }

        private void Register()
        {
            if (string.IsNullOrWhiteSpace(m_targetId))
            {
                return;
            }

            if (!s_targets.TryGetValue(m_targetId, out List<NovelFunctionTarget> targets))
            {
                targets = new List<NovelFunctionTarget>();
                s_targets.Add(m_targetId, targets);
            }

            if (!targets.Contains(this))
            {
                targets.Add(this);
            }
        }

        private void Unregister()
        {
            if (string.IsNullOrWhiteSpace(m_targetId) || !s_targets.TryGetValue(m_targetId, out List<NovelFunctionTarget> targets))
            {
                return;
            }

            targets.Remove(this);
            if (targets.Count == 0)
            {
                s_targets.Remove(m_targetId);
            }
        }
    }
}
