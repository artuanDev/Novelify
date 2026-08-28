using System.Collections;
using UnityEngine;

namespace NovelGraph
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Novelify/Samples/Advanced Sample Rock")]
    public sealed class NovelGraphAdvancedSampleRock : MonoBehaviour
    {
        [SerializeField, Min(1)] private int m_health = 4;
        [SerializeField] private Color m_hitColor = new Color(0.9f, 0.42f, 0.16f, 1f);

        private AudioSource m_audioSource;
        private AudioClip m_generatedImpactClip;
        private Renderer m_renderer;

        public int RemainingHealth => m_health;
        public string LastImpactLabel { get; private set; } = string.Empty;

        private void Awake()
        {
            m_audioSource = GetComponent<AudioSource>();
            m_renderer = GetComponent<Renderer>();
        }

        public void Strike(string impactLabel, int damage, bool playSound)
        {
            LastImpactLabel = impactLabel ?? string.Empty;
            m_health = Mathf.Max(0, m_health - Mathf.Max(0, damage));
            SetHitColor();

            if (playSound)
            {
                PlayImpactSound();
            }

            Debug.Log($"{name} received '{LastImpactLabel}' for {damage} damage. Remaining health: {m_health}.", this);
            if (m_health == 0)
            {
                StartCoroutine(BreakAfterSound());
            }
        }

        private void SetHitColor()
        {
            if (m_renderer == null)
            {
                return;
            }

            var properties = new MaterialPropertyBlock();
            m_renderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", m_hitColor);
            properties.SetColor("_Color", m_hitColor);
            m_renderer.SetPropertyBlock(properties);
        }

        private void PlayImpactSound()
        {
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (m_generatedImpactClip == null)
            {
                const int sampleRate = 22050;
                const int sampleCount = 3307;
                float[] samples = new float[sampleCount];
                for (int i = 0; i < samples.Length; i++)
                {
                    float time = (float)i / sampleRate;
                    float envelope = Mathf.Exp(-22f * time);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * 105f * time) * envelope * 0.45f;
                }

                m_generatedImpactClip = AudioClip.Create("Novelify Rock Impact", sampleCount, 1, sampleRate, false);
                m_generatedImpactClip.SetData(samples, 0);
            }

            m_audioSource.PlayOneShot(m_generatedImpactClip);
        }

        private IEnumerator BreakAfterSound()
        {
            yield return new WaitForSeconds(0.2f);
            Debug.Log($"{name} broke itself after playing its own impact sound.", this);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (m_generatedImpactClip != null)
            {
                Destroy(m_generatedImpactClip);
            }
        }
    }
}
