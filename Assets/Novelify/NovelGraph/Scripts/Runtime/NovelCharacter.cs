using UnityEngine;

namespace NovelGraph
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Novelify/Character")]
    public class NovelCharacter : ScriptableObject
    {
        [SerializeField, Tooltip("Name displayed above dialogue spoken by this character.")]
        private string m_displayName = "Character";

        [SerializeField, Tooltip("Optional short sound played for each visible letter. A generated tone is used when this is empty.")]
        private AudioClip m_letterSound;

        [SerializeField, Range(80f, 1200f), Tooltip("Frequency of the generated fallback voice tone in hertz.")]
        private float m_synthesizedFrequency = 440f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of this character's per-letter voice sound.")]
        private float m_voiceVolume = 0.2f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Random pitch variation applied to repeated letter sounds.")]
        private float m_pitchVariation = 0.08f;

        [SerializeField, Tooltip("Color used for this character's displayed name.")]
        private Color m_nameColor = new Color(0.15f, 0.55f, 0.52f, 1f);

        public string DisplayName => string.IsNullOrWhiteSpace(m_displayName) ? name : m_displayName;
        public AudioClip LetterSound => m_letterSound;
        public float SynthesizedFrequency => m_synthesizedFrequency;
        public float VoiceVolume => m_voiceVolume;
        public float PitchVariation => m_pitchVariation;
        public Color NameColor => m_nameColor;

        public void Configure(string displayName, float frequency, Color nameColor)
        {
            m_displayName = displayName;
            m_synthesizedFrequency = Mathf.Clamp(frequency, 80f, 1200f);
            m_nameColor = nameColor;
        }
    }
}
