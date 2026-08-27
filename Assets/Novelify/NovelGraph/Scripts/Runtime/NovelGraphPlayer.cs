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

        public NovelFunctionBinding(string id)
        {
            m_id = id;
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
            if (m_graphInstance != null) Destroy(m_graphInstance);
            if (m_whiteTexture != null) Destroy(m_whiteTexture);
            foreach (AudioClip clip in m_generatedVoiceClips.Values)
            {
                if (clip != null) Destroy(clip);
            }
        }

        public void SetGraph(NovelGraphAsset graphAsset) => m_graphAsset = graphAsset;

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
            ShowStatus(m_runner.Restore(m_graphInstance, data, gameObject) ? "Progress loaded" : "Save is not compatible");
        }

        public void SetAccentColor(Color color) => m_accentColor = color;

        private void AttachRunner()
        {
            m_runner.PresentationChanged += HandlePresentationChanged;
            m_runner.SignalRaised += HandleSignal;
            m_runner.FunctionRequested += HandleFunctionRequested;
            m_runner.Completed += HandleCompleted;
            m_runner.Faulted += ShowStatus;
        }

        private void DetachRunner()
        {
            if (m_runner == null) return;
            m_runner.PresentationChanged -= HandlePresentationChanged;
            m_runner.SignalRaised -= HandleSignal;
            m_runner.FunctionRequested -= HandleFunctionRequested;
            m_runner.Completed -= HandleCompleted;
            m_runner.Faulted -= ShowStatus;
        }

        private void HandlePresentationChanged(NovelNodeResult presentation)
        {
            m_visibleCharacterCount = presentation.Type == NovelNodeResultType.Dialogue ? 0 : presentation.Text.Length;
            m_typewriterStartedAt = Time.unscaledTime;
            if (presentation.Type == NovelNodeResultType.Dialogue && presentation.PlayLetterSounds)
            {
                EnsureVoiceSource();
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
                return;
            }

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
            m_speakerStyle.normal.textColor = m_accentColor;
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), m_backdropColor);
            DrawRect(new Rect(0f, 0f, 10f, Screen.height), m_accentColor);

            float margin = Mathf.Clamp(Screen.width * 0.04f, 24f, 72f);
            float contentWidth = Screen.width - margin * 2f;
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

        private void DrawDialogue(float margin, float contentWidth)
        {
            NovelNodeResult presentation = m_runner.CurrentPresentation;
            m_speakerStyle.normal.textColor = presentation.Character != null
                ? presentation.Character.NameColor
                : m_accentColor;
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
            m_titleStyle.normal.textColor = Color.white;
            m_speakerStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            m_speakerStyle.normal.textColor = m_accentColor;
            m_dialogueStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
            m_dialogueStyle.normal.textColor = new Color(0.11f, 0.12f, 0.13f, 1f);
            m_promptStyle = new GUIStyle(m_dialogueStyle) { fontSize = 24, fontStyle = FontStyle.Bold };
            m_promptStyle.normal.textColor = Color.white;
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
            m_statusStyle.normal.textColor = new Color(0.95f, 0.68f, 0.28f, 1f);
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
