using UnityEngine;

namespace NovelGraph
{
    public class NovelGraphObject : MonoBehaviour
    {
        [SerializeField]
        private NovelGraphAsset m_graphAsset;

        [SerializeField]
        private bool m_playOnEnable = true;

        private NovelGraphAsset m_graphInstance;

        public NovelGraphRunner Runner { get; private set; }

        private void OnEnable()
        {
            if (m_playOnEnable)
            {
                StartGraph();
            }
        }

        private void OnDisable()
        {
            if (m_graphInstance != null)
            {
                Destroy(m_graphInstance);
                m_graphInstance = null;
            }
        }

        public void SetGraph(NovelGraphAsset graphAsset) => m_graphAsset = graphAsset;

        public void StartGraph()
        {
            if (m_graphAsset == null)
            {
                Debug.LogError("NovelGraphObject needs a graph asset.", this);
                return;
            }

            if (m_graphInstance != null)
            {
                Destroy(m_graphInstance);
            }

            m_graphInstance = Instantiate(m_graphAsset);
            Runner = new NovelGraphRunner();
            Runner.Start(m_graphInstance, gameObject);
        }
    }
}
