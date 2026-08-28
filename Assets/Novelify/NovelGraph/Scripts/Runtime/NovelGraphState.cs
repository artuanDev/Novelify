using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovelGraph
{
    [Serializable]
    public struct NovelGraphStateEntry
    {
        public string key;
        public int value;

        public NovelGraphStateEntry(string key, int value)
        {
            this.key = key;
            this.value = value;
        }
    }

    [Serializable]
    public class NovelGraphSaveData
    {
        public int version = 2;
        public string graphName;
        public string currentGraphId;
        public string nodeId;
        public List<NovelGraphStateEntry> state = new List<NovelGraphStateEntry>();
        public List<NovelGraphCallFrameSaveData> callStack = new List<NovelGraphCallFrameSaveData>();
    }

    [Serializable]
    public class NovelGraphCallFrameSaveData
    {
        public string graphId;
        public string returnNodeId;
    }

    public sealed class NovelGraphState
    {
        private readonly Dictionary<string, int> m_values = new Dictionary<string, int>(StringComparer.Ordinal);

        public int GetInt(string key, int fallback = 0)
        {
            return !string.IsNullOrWhiteSpace(key) && m_values.TryGetValue(key, out int value) ? value : fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            return GetInt(key, fallback ? 1 : 0) != 0;
        }

        public void SetInt(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("NovelGraph ignored an empty state key.");
                return;
            }

            m_values[key] = value;
        }

        public void SetBool(string key, bool value) => SetInt(key, value ? 1 : 0);

        public void Clear() => m_values.Clear();

        public List<NovelGraphStateEntry> ToList()
        {
            List<NovelGraphStateEntry> entries = new List<NovelGraphStateEntry>(m_values.Count);
            foreach (KeyValuePair<string, int> pair in m_values)
            {
                entries.Add(new NovelGraphStateEntry(pair.Key, pair.Value));
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.key, right.key));
            return entries;
        }

        public void Load(IEnumerable<NovelGraphStateEntry> entries)
        {
            Clear();
            if (entries == null)
            {
                return;
            }

            foreach (NovelGraphStateEntry entry in entries)
            {
                SetInt(entry.key, entry.value);
            }
        }
    }
}
