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
                case "reactor_stable":
                    m_player.SetAccentColor(new Color(0.18f, 0.82f, 0.72f, 1f));
                    break;
                case "signal_answered":
                    m_player.SetAccentColor(new Color(0.68f, 0.42f, 0.96f, 1f));
                    break;
                case "signal_jammed":
                    m_player.SetAccentColor(new Color(0.92f, 0.38f, 0.24f, 1f));
                    break;
                case "evacuation_started":
                    m_player.SetAccentColor(new Color(0.98f, 0.68f, 0.18f, 1f));
                    break;
                case "anomaly_touched":
                    m_player.SetAccentColor(new Color(0.9f, 0.22f, 0.72f, 1f));
                    break;
            }

            Debug.Log($"Sample gameplay event received: {signal}", this);
        }

        public void IntensifyStorm()
        {
            m_player.SetAccentColor(new Color(0.32f, 0.48f, 0.78f, 1f));
            Debug.Log("Bound function called: the storm intensity increased.", this);
        }

        public void OpenHarborGates()
        {
            m_player.SetAccentColor(new Color(0.16f, 0.7f, 0.42f, 1f));
            Debug.Log("Bound function called: the harbor gates opened.", this);
        }

        public void CutTowerPower()
        {
            m_player.SetAccentColor(new Color(0.72f, 0.18f, 0.18f, 1f));
            Debug.Log("Bound function called: tower power was cut.", this);
        }

        public void ActivateStationAlarm()
        {
            m_player.SetAccentColor(new Color(0.92f, 0.2f, 0.24f, 1f));
            Debug.Log("Bound function called: station alarm activated.", this);
        }

        public void StabilizeMoonReactor()
        {
            m_player.SetAccentColor(new Color(0.12f, 0.82f, 0.68f, 1f));
            Debug.Log("Bound function called: moon reactor stabilized.", this);
        }

        public void OpenAnomalyGate()
        {
            m_player.SetAccentColor(new Color(0.66f, 0.38f, 0.94f, 1f));
            Debug.Log("Bound function called: anomaly gate opened.", this);
        }

        public void TriggerStationBlackout()
        {
            m_player.SetAccentColor(new Color(0.34f, 0.08f, 0.16f, 1f));
            Debug.Log("Bound function called: station blackout triggered.", this);
        }

        public void LaunchEscapePod()
        {
            m_player.SetAccentColor(new Color(0.98f, 0.62f, 0.14f, 1f));
            Debug.Log("Bound function called: escape pod launched.", this);
        }
    }
}
