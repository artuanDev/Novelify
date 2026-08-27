using UnityEngine;

namespace NovelGraph
{
    [RequireComponent(typeof(NovelGraphPlayer))]
    public class NovelGraphSampleEventReceiver : MonoBehaviour
    {
        private NovelGraphPlayer m_player;

        private void Awake()
        {
            m_player = GetComponent<NovelGraphPlayer>();
            m_player.OnSignal.AddListener(HandleStorySignal);
        }

        private void OnDestroy()
        {
            if (m_player != null) m_player.OnSignal.RemoveListener(HandleStorySignal);
        }

        private void HandleStorySignal(string signal)
        {
            switch (signal)
            {
                case "mira_trusted":
                    m_player.SetAccentColor(new Color(0.12f, 0.62f, 0.58f, 1f));
                    break;
                case "mira_refused":
                    m_player.SetAccentColor(new Color(0.84f, 0.25f, 0.2f, 1f));
                    break;
                case "beacon_lit":
                    m_player.SetAccentColor(new Color(0.95f, 0.63f, 0.16f, 1f));
                    break;
            }

            Debug.Log($"Sample gameplay event received: {signal}", this);
        }
    }
}
