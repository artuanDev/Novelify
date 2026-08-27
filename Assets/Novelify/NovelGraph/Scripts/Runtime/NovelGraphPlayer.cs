using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NovelGraph
{
    [Serializable]
    public class NovelSignalEvent : UnityEvent<string> { }

    [Serializable]
    public class NovelFunctionBinding
    {
        [SerializeField, Tooltip("ID used by a Call Function node.")]
        private string m_id;

        [SerializeField, Tooltip("Scene-object function invoked when a matching Call Function node executes.")]
        private UnityEvent m_callback = new UnityEvent();

        public string Id => m_id;
        public UnityEvent Callback => m_callback;

        public NovelFunctionBinding() { }

        public NovelFunctionBinding(string id)
        {
            m_id = id;
        }
    }

    internal sealed class NovelStagedCharacter
    {
        public NovelCharacter Character;
        public string Expression;
        public float PositionFrom;
        public float PositionTo;
        public float PositionStartedAt;
        public float PositionDuration;
        public float AlphaFrom;
        public float AlphaTo;
        public float AlphaStartedAt;
        public float AlphaDuration;
        public float Scale = 1f;
        public bool FlipX;
        public float ExpressionStartedAt;
        public NovelCharacterMotion Motion;
        public float MotionStartedAt;
        public float MotionDuration;
        public float MotionIntensity = 1f;
        public bool MotionLoops;
        public bool IsTalking;

        public float GetPosition(float now)
        {
            float t = PositionDuration <= 0f ? 1f : Mathf.Clamp01((now - PositionStartedAt) / PositionDuration);
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(PositionFrom, PositionTo, t);
        }

        public float GetAlpha(float now)
        {
            float t = AlphaDuration <= 0f ? 1f : Mathf.Clamp01((now - AlphaStartedAt) / AlphaDuration);
            return Mathf.Lerp(AlphaFrom, AlphaTo, t);
        }
    }

    public class NovelGraphPlayer : MonoBehaviour
    {
        private const string DefaultSaveSlot = "Novelify.Sample.Save";

        [Header("Story")]
        [SerializeField] private NovelGraphAsset m_graphAsset;
        [SerializeField] private bool m_playOnStart = true;
        [SerializeField] private string m_saveSlot = DefaultSaveSlot;

        [Header("Presentation")]
        [SerializeField] private string m_storyTitle = "NOVELIFY";
        [SerializeField] private Color m_backdropColor = new Color(0.07f, 0.08f, 0.09f, 1f);
        [SerializeField] private Color m_accentColor = new Color(0.82f, 0.27f, 0.22f, 1f);

        [Header("Events")]
        [SerializeField] private NovelSignalEvent m_onSignal = new NovelSignalEvent();
        [SerializeField] private UnityEvent m_onStoryCompleted = new UnityEvent();

        [Header("Function Bindings")]
        [SerializeField, Tooltip("Named scene-object functions available to Call Function nodes.")]
        private List<NovelFunctionBinding> m_functionBindings = new List<NovelFunctionBinding>();

        private NovelGraphAsset m_graphInstance;
        private NovelGraphRunner m_runner;
        private AudioSource m_voiceSource;
        private readonly Dictionary<NovelCharacter, AudioClip> m_generatedVoiceClips = new Dictionary<NovelCharacter, AudioClip>();
        private GUIStyle m_titleStyle;
        private GUIStyle m_speakerStyle;
        private GUIStyle m_dialogueStyle;
        private GUIStyle m_promptStyle;
        private GUIStyle m_buttonStyle;
        private GUIStyle m_utilityButtonStyle;
        private GUIStyle m_statusStyle;
        private Texture2D m_whiteTexture;
        private string m_statusMessage = string.Empty;
        private float m_statusUntil;
        private int m_visibleCharacterCount;
        private float m_typewriterStartedAt;
        private readonly List<NovelStagedCharacter> m_stagedCharacters = new List<NovelStagedCharacter>();
        private NovelCharacter m_focusedCharacter;
        private NovelCharacter m_dialogueCharacter;

        public NovelGraphRunner Runner => m_runner;
        public NovelSignalEvent OnSignal => m_onSignal;
        public List<NovelFunctionBinding> FunctionBindings => m_functionBindings;

        private void Start()
        {
            if (m_playOnStart)
            {
                StartStory();
            }
        }

        private void Update()
        {
            if (m_runner == null)
            {
                return;
            }

            UpdateTypewriter();

            if (m_runner.Status == NovelGraphRunnerStatus.WaitingForAdvance &&
                (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                RevealOrAdvance();
            }
            else if (m_runner.Status == NovelGraphRunnerStatus.WaitingForChoice)
            {
                for (int i = 0; i < m_runner.CurrentPresentation.Choices.Count && i < 9; i++)
                {
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                    {
                        m_runner.Choose(i);
                        break;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            DetachRunner();
            m_stagedCharacters.Clear();
            if (m_graphInstance != null) Destroy(m_graphInstance);
            if (m_whiteTexture != null) Destroy(m_whiteTexture);
            foreach (AudioClip clip in m_generatedVoiceClips.Values)
            {
                if (clip != null) Destroy(clip);
            }
        }

        public void SetGraph(NovelGraphAsset graphAsset) => m_graphAsset = graphAsset;
        public void SetStoryTitle(string title) =>
            m_storyTitle = string.IsNullOrWhiteSpace(title) ? "NOVELIFY" : title.Trim();
        public void SetBackdropColor(Color color) => m_backdropColor = color;

        public void StartStory()
        {
            if (m_graphAsset == null)
            {
                ShowStatus("Assign a Novel Graph asset to the player.");
                Debug.LogError("NovelGraphPlayer needs a graph asset.", this);
                return;
            }

            DetachRunner();
            if (m_graphInstance != null) Destroy(m_graphInstance);

            m_graphInstance = Instantiate(m_graphAsset);
            m_runner = new NovelGraphRunner();
            AttachRunner();
            m_stagedCharacters.Clear();
            m_focusedCharacter = null;
            m_dialogueCharacter = null;
            m_runner.Start(m_graphInstance, gameObject);
        }

        public void RestartStory()
        {
            StartStory();
            ShowStatus("Story restarted");
        }

        public void SaveStory()
        {
            if (m_runner == null || m_runner.Status == NovelGraphRunnerStatus.Faulted)
            {
                ShowStatus("Nothing to save");
                return;
            }

            PlayerPrefs.SetString(GetSaveKey(), JsonUtility.ToJson(m_runner.CaptureSaveData()));
            PlayerPrefs.Save();
            ShowStatus("Progress saved");
        }

        public void LoadStory()
        {
            string key = GetSaveKey();
            if (m_graphAsset == null || !PlayerPrefs.HasKey(key))
            {
                ShowStatus("No saved progress");
                return;
            }

            NovelGraphSaveData data = JsonUtility.FromJson<NovelGraphSaveData>(PlayerPrefs.GetString(key));
            DetachRunner();
            if (m_graphInstance != null) Destroy(m_graphInstance);

            m_graphInstance = Instantiate(m_graphAsset);
            m_runner = new NovelGraphRunner();
            AttachRunner();
            m_stagedCharacters.Clear();
            m_focusedCharacter = null;
            m_dialogueCharacter = null;
            ShowStatus(m_runner.Restore(m_graphInstance, data, gameObject) ? "Progress loaded" : "Save is not compatible");
        }

        public void SetAccentColor(Color color) => m_accentColor = color;

        private void AttachRunner()
        {
            m_runner.PresentationChanged += HandlePresentationChanged;
            m_runner.SignalRaised += HandleSignal;
            m_runner.FunctionRequested += HandleFunctionRequested;
            m_runner.CharacterStageRequested += HandleCharacterStageRequested;
            m_runner.Completed += HandleCompleted;
            m_runner.Faulted += ShowStatus;
        }

        private void DetachRunner()
        {
            if (m_runner == null) return;
            m_runner.PresentationChanged -= HandlePresentationChanged;
            m_runner.SignalRaised -= HandleSignal;
            m_runner.FunctionRequested -= HandleFunctionRequested;
            m_runner.CharacterStageRequested -= HandleCharacterStageRequested;
            m_runner.Completed -= HandleCompleted;
            m_runner.Faulted -= ShowStatus;
        }

        private void HandlePresentationChanged(NovelNodeResult presentation)
        {
            if (m_dialogueCharacter != null && m_dialogueCharacter != presentation.Character)
            {
                StopDialoguePresentation(m_dialogueCharacter);
            }
            m_dialogueCharacter = presentation.Type == NovelNodeResultType.Dialogue
                ? presentation.Character
                : null;
            NovelStagedCharacter speaker = FindStagedCharacter(m_dialogueCharacter);
            if (speaker != null)
            {
                speaker.IsTalking = true;
            }
            m_visibleCharacterCount = presentation.Type == NovelNodeResultType.Dialogue ? 0 : presentation.Text.Length;
            m_typewriterStartedAt = Time.unscaledTime;
            if (presentation.Type == NovelNodeResultType.Dialogue && presentation.PlayLetterSounds)
            {
                EnsureVoiceSource();
            }
        }

        private void HandleCharacterStageRequested(NovelCharacterStageCommand command)
        {
            float now = Time.unscaledTime;
            if (command.Type == NovelCharacterStageCommandType.Clear)
            {
                for (int i = 0; i < m_stagedCharacters.Count; i++)
                {
                    FadeCharacter(m_stagedCharacters[i], 0f, command.Duration, now);
                }
                m_focusedCharacter = null;
                return;
            }
            if (command.Type == NovelCharacterStageCommandType.Focus)
            {
                m_focusedCharacter = command.Character;
                return;
            }
            if (command.Character == null)
            {
                return;
            }

            NovelStagedCharacter staged = FindStagedCharacter(command.Character);
            if (command.Type == NovelCharacterStageCommandType.Show)
            {
                if (staged == null)
                {
                    float position = GetStagePosition(command.Position);
                    staged = new NovelStagedCharacter
                    {
                        Character = command.Character,
                        PositionFrom = position,
                        PositionTo = position,
                        AlphaFrom = 0f,
                        AlphaTo = 0f
                    };
                    m_stagedCharacters.Add(staged);
                }
                SetCharacterPosition(staged, GetStagePosition(command.Position), command.Duration, now);
                FadeCharacter(staged, 1f, command.Duration, now);
                staged.Scale = command.Scale;
                staged.FlipX = command.FlipX;
                SetExpression(staged, command.Expression, now);
                return;
            }
            if (staged == null)
            {
                return;
            }

            switch (command.Type)
            {
                case NovelCharacterStageCommandType.Move:
                    SetCharacterPosition(staged, GetStagePosition(command.Position), command.Duration, now);
                    break;
                case NovelCharacterStageCommandType.SetExpression:
                    SetExpression(staged, command.Expression, now);
                    break;
                case NovelCharacterStageCommandType.Animate:
                    staged.Motion = command.Motion;
                    staged.MotionStartedAt = now;
                    staged.MotionDuration = command.Duration;
                    staged.MotionIntensity = command.Intensity;
                    staged.MotionLoops = command.Loop;
                    break;
                case NovelCharacterStageCommandType.Hide:
                    FadeCharacter(staged, 0f, command.Duration, now);
                    if (m_focusedCharacter == command.Character) m_focusedCharacter = null;
                    break;
            }
        }

        private NovelStagedCharacter FindStagedCharacter(NovelCharacter character)
        {
            return m_stagedCharacters.Find(item => item.Character == character);
        }

        private static void SetExpression(NovelStagedCharacter staged, string expression, float now)
        {
            staged.Expression = string.IsNullOrWhiteSpace(expression)
                ? staged.Character.DefaultExpression
                : expression.Trim();
            staged.ExpressionStartedAt = now;
        }

        private static void SetCharacterPosition(NovelStagedCharacter staged, float target, float duration, float now)
        {
            staged.PositionFrom = staged.GetPosition(now);
            staged.PositionTo = target;
            staged.PositionStartedAt = now;
            staged.PositionDuration = duration;
        }

        private static void FadeCharacter(NovelStagedCharacter staged, float target, float duration, float now)
        {
            staged.AlphaFrom = staged.GetAlpha(now);
            staged.AlphaTo = target;
            staged.AlphaStartedAt = now;
            staged.AlphaDuration = duration;
        }

        private static float GetStagePosition(NovelCharacterPosition position)
        {
            switch (position)
            {
                case NovelCharacterPosition.FarLeft: return 0.1f;
                case NovelCharacterPosition.Left: return 0.28f;
                case NovelCharacterPosition.Right: return 0.72f;
                case NovelCharacterPosition.FarRight: return 0.9f;
                default: return 0.5f;
            }
        }

        private void StopDialoguePresentation(NovelCharacter character)
        {
            NovelStagedCharacter staged = FindStagedCharacter(character);
            if (staged != null)
            {
                staged.Motion = NovelCharacterMotion.None;
                staged.MotionLoops = false;
                staged.IsTalking = false;
                NovelCharacterExpression expression = character.GetExpression(staged.Expression);
                if (!character.UsesLayeredPortrait(expression) &&
                    string.Equals(staged.Expression, "talking", StringComparison.OrdinalIgnoreCase))
                {
                    SetExpression(staged, character.DefaultExpression, Time.unscaledTime);
                }
            }
        }

        private void HandleFunctionRequested(string functionId)
        {
            NovelFunctionBinding binding = m_functionBindings.Find(item =>
                item != null && string.Equals(item.Id, functionId, StringComparison.Ordinal));
            if (binding == null)
            {
                Debug.LogWarning($"Novelify function binding '{functionId}' was not found on {name}.", this);
                ShowStatus($"Missing function: {functionId}");
                return;
            }

            binding.Callback.Invoke();
            ShowStatus($"Function: {functionId}");
        }

        private void HandleSignal(string signal)
        {
            ShowStatus($"Event: {signal}");
            m_onSignal.Invoke(signal);
        }

        private void HandleCompleted()
        {
            ShowStatus("The end");
            m_onStoryCompleted.Invoke();
        }

        private void ShowStatus(string message)
        {
            m_statusMessage = message ?? string.Empty;
            m_statusUntil = Time.unscaledTime + 3f;
        }

        private bool IsTyping()
        {
            return m_runner != null &&
                   m_runner.Status == NovelGraphRunnerStatus.WaitingForAdvance &&
                   m_visibleCharacterCount < m_runner.CurrentPresentation.Text.Length;
        }

        private void RevealOrAdvance()
        {
            if (IsTyping())
            {
                m_visibleCharacterCount = m_runner.CurrentPresentation.Text.Length;
                StopDialoguePresentation(m_dialogueCharacter);
                return;
            }

            StopDialoguePresentation(m_dialogueCharacter);
            m_dialogueCharacter = null;
            m_runner.Advance();
        }

        private void UpdateTypewriter()
        {
            if (m_runner.Status != NovelGraphRunnerStatus.WaitingForAdvance)
            {
                return;
            }

            NovelNodeResult presentation = m_runner.CurrentPresentation;
            int previousCount = m_visibleCharacterCount;
            int targetCount = Mathf.Min(
                presentation.Text.Length,
                Mathf.FloorToInt((Time.unscaledTime - m_typewriterStartedAt) * presentation.CharactersPerSecond));
            if (targetCount <= previousCount)
            {
                return;
            }

            m_visibleCharacterCount = targetCount;
            if (m_visibleCharacterCount >= presentation.Text.Length)
            {
                StopDialoguePresentation(m_dialogueCharacter);
            }
            if (!presentation.PlayLetterSounds || presentation.Character == null)
            {
                return;
            }

            for (int i = previousCount; i < targetCount; i++)
            {
                if (char.IsLetterOrDigit(presentation.Text[i]))
                {
                    PlayLetterSound(presentation.Character);
                }
            }
        }

        private void EnsureVoiceSource()
        {
            if (m_voiceSource != null)
            {
                return;
            }

            m_voiceSource = GetComponent<AudioSource>();
            if (m_voiceSource == null)
            {
                m_voiceSource = gameObject.AddComponent<AudioSource>();
            }

            m_voiceSource.playOnAwake = false;
            m_voiceSource.spatialBlend = 0f;
        }

        private void PlayLetterSound(NovelCharacter character)
        {
            EnsureVoiceSource();
            AudioClip clip = character.LetterSound != null ? character.LetterSound : GetGeneratedVoiceClip(character);
            m_voiceSource.pitch = 1f + UnityEngine.Random.Range(-character.PitchVariation, character.PitchVariation);
            m_voiceSource.PlayOneShot(clip, character.VoiceVolume);
        }

        private AudioClip GetGeneratedVoiceClip(NovelCharacter character)
        {
            if (m_generatedVoiceClips.TryGetValue(character, out AudioClip cached))
            {
                return cached;
            }

            const int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * 0.035f);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float normalizedTime = i / (float)sampleCount;
                float envelope = 1f - normalizedTime;
                samples[i] = Mathf.Sin(2f * Mathf.PI * character.SynthesizedFrequency * i / sampleRate) * envelope * 0.35f;
            }

            AudioClip generated = AudioClip.Create($"{character.name} Letter Voice", sampleCount, 1, sampleRate, false);
            generated.SetData(samples, 0);
            m_generatedVoiceClips.Add(character, generated);
            return generated;
        }

        private string GetSaveKey()
        {
            string slot = string.IsNullOrWhiteSpace(m_saveSlot) ? DefaultSaveSlot : m_saveSlot.Trim();
            return $"{slot}.{(m_graphAsset != null ? m_graphAsset.name : "None")}";
        }

        private void OnGUI()
        {
            EnsureStyles();
            SetLabelColor(m_speakerStyle, m_accentColor);
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), m_backdropColor);
            DrawRect(new Rect(0f, 0f, 10f, Screen.height), m_accentColor);

            float margin = Mathf.Clamp(Screen.width * 0.04f, 24f, 72f);
            float contentWidth = Screen.width - margin * 2f;
            DrawCharacters(margin);
            float titleWidth = Screen.width < 700 ? 0f : contentWidth * 0.6f;
            GUI.Label(new Rect(margin, 30f, titleWidth, 42f), m_storyTitle, m_titleStyle);
            DrawUtilityButtons(margin, contentWidth);

            if (m_runner == null)
            {
                GUI.Label(new Rect(margin, Screen.height * 0.4f, contentWidth, 80f),
                    "Assign a graph and start the story.", m_dialogueStyle);
                return;
            }

            if (m_runner.Status == NovelGraphRunnerStatus.WaitingForChoice) DrawChoices(margin, contentWidth);
            else if (m_runner.Status == NovelGraphRunnerStatus.WaitingForAdvance) DrawDialogue(margin, contentWidth);
            else if (m_runner.Status == NovelGraphRunnerStatus.Completed)
                GUI.Label(new Rect(margin, Screen.height * 0.38f, contentWidth, 80f), "THE END", m_titleStyle);

            if (Time.unscaledTime < m_statusUntil && !string.IsNullOrEmpty(m_statusMessage))
            {
                GUI.Label(new Rect(margin, 84f, Mathf.Min(420f, contentWidth), 34f), m_statusMessage, m_statusStyle);
            }
        }

        private void DrawCharacters(float margin)
        {
            if (m_stagedCharacters.Count == 0)
            {
                return;
            }

            float panelHeight = Mathf.Clamp(Screen.height * 0.32f, 210f, 330f);
            float stageBottom = Screen.height - panelHeight - margin;
            Rect stage = new Rect(10f, 78f, Screen.width - 10f, Mathf.Max(1f, stageBottom - 78f));
            float now = Time.unscaledTime;

            GUI.BeginGroup(stage);
            for (int i = 0; i < m_stagedCharacters.Count; i++)
            {
                NovelStagedCharacter staged = m_stagedCharacters[i];
                float alpha = staged.GetAlpha(now);
                if (alpha <= 0.001f)
                {
                    continue;
                }

                float motionScale;
                Vector2 motionOffset = GetMotionOffset(staged, now, stage.size, out motionScale);
                float centreX = staged.GetPosition(now) * stage.width + motionOffset.x;
                float centreY = stage.height * 0.5f + motionOffset.y;

                float brightness = m_focusedCharacter == null || m_focusedCharacter == staged.Character
                    ? 1f
                    : 0.48f;
                Color previous = GUI.color;
                GUI.color = new Color(brightness, brightness, brightness, alpha);
                NovelCharacterExpression expression = staged.Character.GetExpression(staged.Expression);
                float elapsed = now - staged.ExpressionStartedAt;
                if (staged.Character.UsesLayeredPortrait(expression))
                {
                    DrawCharacterLayer(
                        staged.Character.Body.GetFrame(elapsed),
                        staged.Character.Body.Framing,
                        staged,
                        stage.size,
                        centreX,
                        centreY,
                        motionScale);
                    if (expression != null)
                    {
                        DrawCharacterLayer(
                            expression.Eyes.GetFrame(elapsed),
                            expression.Eyes.Framing,
                            staged,
                            stage.size,
                            centreX,
                            centreY,
                            motionScale);
                        DrawCharacterLayer(
                            expression.Mouth.GetFrame(elapsed, staged.IsTalking),
                            expression.Mouth.Framing,
                            staged,
                            stage.size,
                            centreX,
                            centreY,
                            motionScale);
                    }
                }
                else if (expression != null)
                {
                    DrawCharacterLayer(
                        expression.GetFrame(elapsed),
                        expression.Framing,
                        staged,
                        stage.size,
                        centreX,
                        centreY,
                        motionScale);
                }
                GUI.color = previous;
            }
            GUI.EndGroup();
        }

        private static void DrawCharacterLayer(
            Sprite sprite,
            NovelCharacterFraming framing,
            NovelStagedCharacter staged,
            Vector2 stageSize,
            float centreX,
            float centreY,
            float motionScale)
        {
            if (sprite == null || sprite.texture == null || framing == null)
            {
                return;
            }

            Rect spriteRect = sprite.rect;
            float displayScale = stageSize.y /
                (2f * framing.Radius * Mathf.Max(1f, spriteRect.height));
            displayScale *= staged.Scale * motionScale;
            float width = spriteRect.width * displayScale;
            float height = spriteRect.height * displayScale;
            bool flip = framing.FlipX ^ staged.FlipX;
            float pointX = flip ? 1f - framing.Point.x : framing.Point.x;
            float offsetX = (staged.FlipX ? -framing.Offset.x : framing.Offset.x) * stageSize.y;
            float offsetY = -framing.Offset.y * stageSize.y;
            Rect destination = new Rect(
                centreX + offsetX - pointX * width,
                centreY + offsetY - (1f - framing.Point.y) * height,
                width,
                height);
            Rect uv = new Rect(
                spriteRect.x / sprite.texture.width,
                spriteRect.y / sprite.texture.height,
                spriteRect.width / sprite.texture.width,
                spriteRect.height / sprite.texture.height);
            if (flip)
            {
                uv.x += uv.width;
                uv.width = -uv.width;
            }

            GUI.DrawTextureWithTexCoords(destination, sprite.texture, uv, true);
        }

        private static Vector2 GetMotionOffset(
            NovelStagedCharacter staged,
            float now,
            Vector2 stageSize,
            out float motionScale)
        {
            motionScale = 1f;
            if (staged.Motion == NovelCharacterMotion.None)
            {
                return Vector2.zero;
            }

            float elapsed = Mathf.Max(0f, now - staged.MotionStartedAt);
            if (!staged.MotionLoops && staged.MotionDuration > 0f && elapsed >= staged.MotionDuration)
            {
                staged.Motion = NovelCharacterMotion.None;
                return Vector2.zero;
            }

            float intensity = staged.MotionIntensity;
            float decay = staged.MotionLoops || staged.MotionDuration <= 0f
                ? 1f
                : 1f - Mathf.Clamp01(elapsed / staged.MotionDuration);
            switch (staged.Motion)
            {
                case NovelCharacterMotion.Talking:
                    motionScale = 1f + Mathf.Sin(elapsed * 15f) * 0.008f * intensity;
                    return new Vector2(0f, -Mathf.Abs(Mathf.Sin(elapsed * 14f)) * stageSize.y * 0.008f * intensity);
                case NovelCharacterMotion.Shocked:
                    motionScale = 1f + 0.06f * intensity * decay;
                    return new Vector2(Mathf.Sin(elapsed * 48f) * stageSize.x * 0.012f * intensity * decay, 0f);
                case NovelCharacterMotion.Shake:
                    return new Vector2(Mathf.Sin(elapsed * 35f) * stageSize.x * 0.012f * intensity * decay, 0f);
                case NovelCharacterMotion.Bounce:
                    return new Vector2(0f, -Mathf.Abs(Mathf.Sin(elapsed * 8f)) * stageSize.y * 0.04f * intensity);
                case NovelCharacterMotion.Jump:
                    float jumpProgress = staged.MotionDuration <= 0f
                        ? Mathf.Repeat(elapsed, 0.7f) / 0.7f
                        : Mathf.Clamp01(elapsed / staged.MotionDuration);
                    return new Vector2(0f, -Mathf.Sin(jumpProgress * Mathf.PI) * stageSize.y * 0.15f * intensity);
                case NovelCharacterMotion.Pulse:
                    motionScale = 1f + Mathf.Sin(elapsed * 8f) * 0.045f * intensity * decay;
                    return Vector2.zero;
                default:
                    return Vector2.zero;
            }
        }

        private void DrawDialogue(float margin, float contentWidth)
        {
            NovelNodeResult presentation = m_runner.CurrentPresentation;
            SetLabelColor(m_speakerStyle, presentation.Character != null
                ? presentation.Character.NameColor
                : m_accentColor);
            float panelHeight = Mathf.Clamp(Screen.height * 0.32f, 210f, 330f);
            Rect panel = new Rect(margin, Screen.height - panelHeight - margin, contentWidth, panelHeight);
            DrawRect(panel, new Color(0.94f, 0.94f, 0.91f, 0.98f));
            DrawRect(new Rect(panel.x, panel.y, 7f, panel.height), m_accentColor);

            float inner = Mathf.Clamp(panel.width * 0.035f, 26f, 46f);
            if (!string.IsNullOrWhiteSpace(presentation.Speaker))
            {
                GUI.Label(new Rect(panel.x + inner, panel.y + 24f, panel.width - inner * 2f, 34f),
                    presentation.Speaker.ToUpperInvariant(), m_speakerStyle);
            }

            GUI.Label(new Rect(panel.x + inner, panel.y + 68f, panel.width - inner * 2f, panel.height - 132f),
                presentation.Text.Substring(0, Mathf.Clamp(m_visibleCharacterCount, 0, presentation.Text.Length)), m_dialogueStyle);

            string continueLabel = IsTyping() ? "REVEAL" : "CONTINUE";
            if (GUI.Button(new Rect(panel.xMax - 142f, panel.yMax - 54f, 112f, 36f), continueLabel, m_utilityButtonStyle))
                RevealOrAdvance();
        }

        private void DrawChoices(float margin, float contentWidth)
        {
            NovelNodeResult presentation = m_runner.CurrentPresentation;
            float maxWidth = Mathf.Min(920f, contentWidth);
            float left = margin + (contentWidth - maxWidth) * 0.5f;
            float promptY = Mathf.Max(130f, Screen.height * 0.18f);
            GUI.Label(new Rect(left, promptY, maxWidth, 92f), presentation.Text, m_promptStyle);

            float buttonY = promptY + 112f;
            float availableHeight = Screen.height - buttonY - margin;
            float buttonHeight = Mathf.Clamp((availableHeight - 14f * (presentation.Choices.Count - 1)) / presentation.Choices.Count, 44f, 78f);
            for (int i = 0; i < presentation.Choices.Count; i++)
            {
                NovelChoiceOption choice = presentation.Choices[i];
                if (GUI.Button(new Rect(left, buttonY, maxWidth, buttonHeight), $"{i + 1}.  {choice.Label}", m_buttonStyle))
                    m_runner.Choose(i);
                buttonY += buttonHeight + 14f;
            }
        }

        private void DrawUtilityButtons(float margin, float contentWidth)
        {
            const float width = 72f;
            const float gap = 9f;
            float x = margin + contentWidth - width * 3f - gap * 2f;
            if (GUI.Button(new Rect(x, 30f, width, 34f), "SAVE", m_utilityButtonStyle)) SaveStory();
            if (GUI.Button(new Rect(x + width + gap, 30f, width, 34f), "LOAD", m_utilityButtonStyle)) LoadStory();
            if (GUI.Button(new Rect(x + (width + gap) * 2f, 30f, width, 34f), "RESTART", m_utilityButtonStyle)) RestartStory();
        }

        private void EnsureStyles()
        {
            if (m_whiteTexture == null)
            {
                m_whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                m_whiteTexture.SetPixel(0, 0, Color.white);
                m_whiteTexture.Apply();
            }

            if (m_titleStyle != null) return;
            m_titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            SetLabelColor(m_titleStyle, Color.white);
            m_speakerStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            SetLabelColor(m_speakerStyle, m_accentColor);
            m_dialogueStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
            SetLabelColor(m_dialogueStyle, new Color(0.11f, 0.12f, 0.13f, 1f));
            m_promptStyle = new GUIStyle(m_dialogueStyle) { fontSize = 24, fontStyle = FontStyle.Bold };
            SetLabelColor(m_promptStyle, Color.white);
            m_buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(28, 24, 8, 8),
                wordWrap = true
            };
            m_utilityButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold };
            m_statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            SetLabelColor(m_statusStyle, new Color(0.95f, 0.68f, 0.28f, 1f));
        }

        private static void SetLabelColor(GUIStyle style, Color color)
        {
            style.richText = false;
            SetLabelState(style.normal, color);
            SetLabelState(style.hover, color);
            SetLabelState(style.active, color);
            SetLabelState(style.focused, color);
            SetLabelState(style.onNormal, color);
            SetLabelState(style.onHover, color);
            SetLabelState(style.onActive, color);
            SetLabelState(style.onFocused, color);
        }

        private static void SetLabelState(GUIStyleState state, Color color)
        {
            state.textColor = color;
            state.background = null;
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, m_whiteTexture);
            GUI.color = previous;
        }
    }
}
