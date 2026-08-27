using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovelGraph
{
    [Serializable]
    public class NovelCharacterFraming
    {
        [SerializeField, Tooltip("Point inside the sprite that is aligned to the layer position, in normalized sprite coordinates.")]
        private Vector2 m_point = new Vector2(0.5f, 0.5f);

        [SerializeField, Min(0.01f), Tooltip("Half of the visible height as a fraction of the sprite height. Larger values make a tightly cropped layer smaller.")]
        private float m_radius = 0.5f;

        [SerializeField, Tooltip("Layer offset from the character centre, measured as a fraction of the character viewport height.")]
        private Vector2 m_offset;

        [SerializeField, Tooltip("Mirror this layer horizontally before applying its framing.")]
        private bool m_flipX;

        public Vector2 Point => m_point;
        public float Radius => Mathf.Max(0.01f, m_radius);
        public Vector2 Offset => m_offset;
        public bool FlipX => m_flipX;

        public void Set(Vector2 point, float radius, bool flipX)
        {
            Set(point, radius, m_offset, flipX);
        }

        public void Set(Vector2 point, float radius, Vector2 offset, bool flipX)
        {
            m_point = new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
            m_radius = Mathf.Max(0.01f, radius);
            m_offset = new Vector2(
                Mathf.Clamp(offset.x, -4f, 4f),
                Mathf.Clamp(offset.y, -4f, 4f));
            m_flipX = flipX;
        }
    }

    [Serializable]
    public class NovelCharacterSpriteLayer
    {
        [SerializeField, Tooltip("Default sprite for this layer.")]
        private Sprite m_sprite;

        [SerializeField, Tooltip("Optional animation frames. When assigned, these replace the default sprite during playback.")]
        private List<Sprite> m_animationFrames = new List<Sprite>();

        [SerializeField, Min(0.1f), Tooltip("Playback speed for this layer's optional animation.")]
        private float m_framesPerSecond = 8f;

        [SerializeField, Tooltip("Loop this layer's optional animation.")]
        private bool m_loop = true;

        [SerializeField, Tooltip("Scale, anchor, offset, and mirroring used to position this layer.")]
        private NovelCharacterFraming m_framing = new NovelCharacterFraming();

        public Sprite Sprite => m_sprite;
        public NovelCharacterFraming Framing => m_framing ?? (m_framing = new NovelCharacterFraming());
        public float FramesPerSecond => Mathf.Max(0.1f, m_framesPerSecond);
        public bool Loop => m_loop;
        public bool HasVisual => m_sprite != null || (m_animationFrames != null && m_animationFrames.Exists(frame => frame != null));

        public Sprite GetFrame(float elapsedTime)
        {
            int count = m_animationFrames != null ? m_animationFrames.Count : 0;
            if (count == 0)
            {
                return m_sprite;
            }

            int frame = Mathf.FloorToInt(Mathf.Max(0f, elapsedTime) * FramesPerSecond);
            frame = m_loop ? frame % count : Mathf.Min(frame, count - 1);
            return m_animationFrames[frame] != null ? m_animationFrames[frame] : m_sprite;
        }

        public void Configure(
            Sprite sprite,
            IEnumerable<Sprite> animationFrames,
            float framesPerSecond,
            bool loop,
            Vector2 framingPoint,
            float framingRadius,
            Vector2 framingOffset,
            bool flipX = false)
        {
            m_sprite = sprite;
            m_animationFrames = animationFrames != null
                ? new List<Sprite>(animationFrames)
                : new List<Sprite>();
            m_framesPerSecond = Mathf.Max(0.1f, framesPerSecond);
            m_loop = loop;
            Framing.Set(framingPoint, framingRadius, framingOffset, flipX);
        }
    }

    [Serializable]
    public class NovelCharacterMouthLayer
    {
        [SerializeField, Tooltip("Closed or resting mouth sprite used while this character is not speaking.")]
        private Sprite m_idleSprite;

        [SerializeField, Tooltip("Optional resting-mouth animation frames.")]
        private List<Sprite> m_idleAnimationFrames = new List<Sprite>();

        [SerializeField, Tooltip("Mouth frames cycled while this character's dialogue is being typed.")]
        private List<Sprite> m_talkingAnimationFrames = new List<Sprite>();

        [SerializeField, Min(0.1f), Tooltip("Playback speed for idle and talking mouth frames.")]
        private float m_framesPerSecond = 8f;

        [SerializeField, Tooltip("Scale, anchor, offset, and mirroring used to position this mouth.")]
        private NovelCharacterFraming m_framing = new NovelCharacterFraming();

        public Sprite IdleSprite => m_idleSprite;
        public NovelCharacterFraming Framing => m_framing ?? (m_framing = new NovelCharacterFraming());
        public float FramesPerSecond => Mathf.Max(0.1f, m_framesPerSecond);
        public bool HasVisual =>
            m_idleSprite != null ||
            (m_idleAnimationFrames != null && m_idleAnimationFrames.Exists(frame => frame != null)) ||
            (m_talkingAnimationFrames != null && m_talkingAnimationFrames.Exists(frame => frame != null));

        public Sprite GetFrame(float elapsedTime, bool talking)
        {
            List<Sprite> frames = talking && m_talkingAnimationFrames != null &&
                                  m_talkingAnimationFrames.Count > 0
                ? m_talkingAnimationFrames
                : m_idleAnimationFrames;
            int count = frames != null ? frames.Count : 0;
            if (count == 0)
            {
                return m_idleSprite;
            }

            int frame = Mathf.FloorToInt(Mathf.Max(0f, elapsedTime) * FramesPerSecond) % count;
            return frames[frame] != null ? frames[frame] : m_idleSprite;
        }

        public void Configure(
            Sprite idleSprite,
            IEnumerable<Sprite> idleAnimationFrames,
            IEnumerable<Sprite> talkingAnimationFrames,
            float framesPerSecond,
            Vector2 framingPoint,
            float framingRadius,
            Vector2 framingOffset,
            bool flipX = false)
        {
            m_idleSprite = idleSprite;
            m_idleAnimationFrames = idleAnimationFrames != null
                ? new List<Sprite>(idleAnimationFrames)
                : new List<Sprite>();
            m_talkingAnimationFrames = talkingAnimationFrames != null
                ? new List<Sprite>(talkingAnimationFrames)
                : new List<Sprite>();
            m_framesPerSecond = Mathf.Max(0.1f, framesPerSecond);
            Framing.Set(framingPoint, framingRadius, framingOffset, flipX);
        }
    }

    [Serializable]
    public class NovelCharacterExpression
    {
        [SerializeField, Tooltip("Case-insensitive emotion name used by Dialogue and Set Emotion nodes, such as neutral, happy, shocked, or suspicious.")]
        private string m_id = "neutral";

        [SerializeField, Tooltip("Eyes and eyebrows used for this emotion. Optional animation frames can provide blinking or eye movement.")]
        private NovelCharacterSpriteLayer m_eyes = new NovelCharacterSpriteLayer();

        [SerializeField, Tooltip("Idle and talking mouth sprites used for this emotion.")]
        private NovelCharacterMouthLayer m_mouth = new NovelCharacterMouthLayer();

        // Kept serialized so characters made before layered portraits continue to render.
        [SerializeField, HideInInspector]
        private Sprite m_sprite;

        [SerializeField, HideInInspector]
        private List<Sprite> m_animationFrames = new List<Sprite>();

        [SerializeField, HideInInspector]
        private float m_framesPerSecond = 8f;

        [SerializeField, HideInInspector]
        private bool m_loop = true;

        [SerializeField, HideInInspector]
        private NovelCharacterFraming m_framing = new NovelCharacterFraming();

        public string Id => string.IsNullOrWhiteSpace(m_id) ? "neutral" : m_id.Trim();
        public NovelCharacterSpriteLayer Eyes => m_eyes ?? (m_eyes = new NovelCharacterSpriteLayer());
        public NovelCharacterMouthLayer Mouth => m_mouth ?? (m_mouth = new NovelCharacterMouthLayer());
        public bool HasLayeredVisuals => Eyes.HasVisual || Mouth.HasVisual;

        public Sprite Sprite => m_sprite;
        public NovelCharacterFraming Framing => m_framing ?? (m_framing = new NovelCharacterFraming());
        public float FramesPerSecond => Mathf.Max(0.1f, m_framesPerSecond);
        public bool Loop => m_loop;
        public bool HasLegacyVisual => m_sprite != null ||
                                       (m_animationFrames != null && m_animationFrames.Exists(frame => frame != null));

        public Sprite GetFrame(float elapsedTime)
        {
            int count = m_animationFrames != null ? m_animationFrames.Count : 0;
            if (count == 0)
            {
                return m_sprite;
            }

            int frame = Mathf.FloorToInt(Mathf.Max(0f, elapsedTime) * FramesPerSecond);
            frame = m_loop ? frame % count : Mathf.Min(frame, count - 1);
            return m_animationFrames[frame] != null ? m_animationFrames[frame] : m_sprite;
        }

        public void Configure(
            string id,
            Sprite sprite,
            IEnumerable<Sprite> animationFrames,
            float framesPerSecond,
            bool loop,
            Vector2 framingPoint,
            float framingRadius,
            bool flipX = false)
        {
            m_id = string.IsNullOrWhiteSpace(id) ? "neutral" : id.Trim();
            m_sprite = sprite;
            m_animationFrames = animationFrames != null
                ? new List<Sprite>(animationFrames)
                : new List<Sprite>();
            m_framesPerSecond = Mathf.Max(0.1f, framesPerSecond);
            m_loop = loop;
            Framing.Set(framingPoint, framingRadius, flipX);
        }

        public void ConfigureLayers(
            string id,
            NovelCharacterSpriteLayer eyes,
            NovelCharacterMouthLayer mouth)
        {
            m_id = string.IsNullOrWhiteSpace(id) ? "neutral" : id.Trim();
            m_eyes = eyes ?? new NovelCharacterSpriteLayer();
            m_mouth = mouth ?? new NovelCharacterMouthLayer();
        }
    }

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

        [Header("Layered Portrait")]
        [SerializeField, Tooltip("Character body with an empty face. It stays visible while eye and mouth layers change independently.")]
        private NovelCharacterSpriteLayer m_body = new NovelCharacterSpriteLayer();

        [SerializeField, Tooltip("Emotion used when a graph leaves the emotion name empty or cannot find it.")]
        private string m_defaultExpression = "neutral";

        [SerializeField, Tooltip("Emotion-specific eye and mouth layers. Talking mouth frames play automatically during dialogue.")]
        private List<NovelCharacterExpression> m_expressions =
            new List<NovelCharacterExpression> { new NovelCharacterExpression() };

        public string DisplayName => string.IsNullOrWhiteSpace(m_displayName) ? name : m_displayName;
        public AudioClip LetterSound => m_letterSound;
        public float SynthesizedFrequency => m_synthesizedFrequency;
        public float VoiceVolume => m_voiceVolume;
        public float PitchVariation => m_pitchVariation;
        public Color NameColor => m_nameColor;
        public NovelCharacterSpriteLayer Body => m_body ?? (m_body = new NovelCharacterSpriteLayer());
        public string DefaultExpression => string.IsNullOrWhiteSpace(m_defaultExpression) ? "neutral" : m_defaultExpression.Trim();
        public IReadOnlyList<NovelCharacterExpression> Expressions => m_expressions;

        public NovelCharacterExpression GetExpression(string expressionId)
        {
            if (m_expressions == null || m_expressions.Count == 0)
            {
                return null;
            }

            string requested = string.IsNullOrWhiteSpace(expressionId) ? DefaultExpression : expressionId.Trim();
            NovelCharacterExpression fallback = null;
            for (int i = 0; i < m_expressions.Count; i++)
            {
                NovelCharacterExpression expression = m_expressions[i];
                if (expression == null)
                {
                    continue;
                }

                if (fallback == null || string.Equals(expression.Id, DefaultExpression, StringComparison.OrdinalIgnoreCase))
                {
                    fallback = expression;
                }

                if (string.Equals(expression.Id, requested, StringComparison.OrdinalIgnoreCase))
                {
                    return expression;
                }
            }

            return fallback;
        }

        public bool UsesLayeredPortrait(NovelCharacterExpression expression)
        {
            return Body.HasVisual || (expression != null && expression.HasLayeredVisuals);
        }

        public void ConfigureBody(NovelCharacterSpriteLayer body)
        {
            m_body = body ?? new NovelCharacterSpriteLayer();
        }

        public void ConfigureExpressions(
            string defaultExpression,
            params NovelCharacterExpression[] expressions)
        {
            m_defaultExpression = string.IsNullOrWhiteSpace(defaultExpression)
                ? "neutral"
                : defaultExpression.Trim();
            m_expressions = expressions != null
                ? new List<NovelCharacterExpression>(expressions)
                : new List<NovelCharacterExpression>();
        }

        public void Configure(string displayName, float frequency, Color nameColor)
        {
            m_displayName = displayName;
            m_synthesizedFrequency = Mathf.Clamp(frequency, 80f, 1200f);
            m_nameColor = nameColor;
        }
    }
}
