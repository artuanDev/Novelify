using System;
using UnityEngine;

namespace NovelGraph
{
    [CreateAssetMenu(menuName = "Novel Graph/New Novel Page", fileName = "New Novel Page")]
    public class NovelPageAsset : NovelGraphAsset, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private string m_pageId;

        public string PageId
        {
            get
            {
                EnsurePageId();
                return m_pageId;
            }
        }

        public void OnBeforeSerialize() => EnsurePageId();

        public void OnAfterDeserialize() { }

        private void OnEnable() => EnsurePageId();

        private void EnsurePageId()
        {
            if (string.IsNullOrWhiteSpace(m_pageId))
            {
                m_pageId = Guid.NewGuid().ToString("N");
            }
        }
    }
}
