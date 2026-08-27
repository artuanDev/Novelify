using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NovelGraph.Editor
{
    public class NovelCharacterFramingWindow : EditorWindow
    {
        private enum PortraitLayer
        {
            Body,
            EmotionEyes,
            EmotionMouth,
            LegacyPortrait
        }

        [SerializeField] private NovelCharacter m_character;
        [SerializeField] private int m_expressionIndex;
        [SerializeField] private PortraitLayer m_layer;
        [SerializeField] private float m_previewAspect = 16f / 9f;
        [SerializeField] private bool m_previewTalking = true;

        private string m_saveStatus;
        private double m_saveStatusUntil;

        [MenuItem("Tools/Novelify/Character Framing Tool")]
        public static void Open()
        {
            GetWindow<NovelCharacterFramingWindow>("Character Framing").minSize = new Vector2(460f, 620f);
        }

        private void OnEnable()
        {
            TryUseSelection();
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
            SaveCharacterAsset();
        }

        private void OnSelectionChange()
        {
            TryUseSelection();
            Repaint();
        }

        private void TryUseSelection()
        {
            if (Selection.activeObject is NovelCharacter selected)
            {
                m_character = selected;
                m_expressionIndex = 0;
                ChooseInitialLayer();
            }
        }

        private void ChooseInitialLayer()
        {
            NovelCharacterExpression expression = GetSelectedExpression();
            m_layer = m_character != null && !m_character.Body.HasVisual &&
                      expression != null && expression.HasLegacyVisual
                ? PortraitLayer.LegacyPortrait
                : PortraitLayer.Body;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Layered Character Framing", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "Compose a character from Body, Emotion Eyes, and Emotion Mouth layers. " +
                "Each emotion owns its eyes plus its own idle/talking mouth. Talking frames play automatically during dialogue.",
                MessageType.Info);

            NovelCharacter nextCharacter = (NovelCharacter)EditorGUILayout.ObjectField(
                "Character", m_character, typeof(NovelCharacter), false);
            if (nextCharacter != m_character)
            {
                SaveCharacterAsset();
                m_character = nextCharacter;
                m_expressionIndex = 0;
                ChooseInitialLayer();
            }

            if (m_character == null)
            {
                EditorGUILayout.HelpBox("Select or assign a Novelify Character asset.", MessageType.Warning);
                return;
            }

            m_layer = (PortraitLayer)EditorGUILayout.EnumPopup("Edit Layer", m_layer);
            bool needsExpression = m_layer != PortraitLayer.Body;
            NovelCharacterExpression expression = null;
            if (needsExpression)
            {
                if (m_character.Expressions == null || m_character.Expressions.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "Add at least one emotion entry to the Character asset.",
                        MessageType.Warning);
                    return;
                }

                m_expressionIndex = Mathf.Clamp(m_expressionIndex, 0, m_character.Expressions.Count - 1);
                string[] names = m_character.Expressions
                    .Select(item => item != null ? item.Id : "<missing>")
                    .ToArray();
                m_expressionIndex = EditorGUILayout.Popup("Emotion", m_expressionIndex, names);
                expression = GetSelectedExpression();
                if (expression == null)
                {
                    EditorGUILayout.HelpBox("This emotion entry is missing.", MessageType.Error);
                    return;
                }
            }

            if (m_layer == PortraitLayer.EmotionMouth)
            {
                m_previewTalking = EditorGUILayout.Toggle("Preview Talking", m_previewTalking);
            }

            float previewTime = (float)EditorApplication.timeSinceStartup;
            NovelCharacterFraming framing;
            Sprite selectedSprite;
            if (!TryGetSelectedLayer(expression, previewTime, out framing, out selectedSprite))
            {
                EditorGUILayout.HelpBox("The selected layer is unavailable.", MessageType.Error);
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Selected Layer Framing", EditorStyles.boldLabel);
            Vector2 point = EditorGUILayout.Vector2Field("Sprite Anchor", framing.Point);
            float radius = Mathf.Max(0.01f, EditorGUILayout.FloatField("Visible Radius", framing.Radius));
            Vector2 offset = EditorGUILayout.Vector2Field("Layer Offset", framing.Offset);
            bool flipX = EditorGUILayout.Toggle("Flip Horizontally", framing.FlipX);
            if (point != framing.Point ||
                !Mathf.Approximately(radius, framing.Radius) ||
                offset != framing.Offset ||
                flipX != framing.FlipX)
            {
                SaveFraming(framing, point, radius, offset, flipX);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Layer"))
                {
                    SaveFraming(
                        framing,
                        new Vector2(0.5f, 0.5f),
                        0.5f,
                        Vector2.zero,
                        false);
                }

                using (new EditorGUI.DisabledScope(!needsExpression))
                {
                    if (GUILayout.Button("Previous Emotion"))
                    {
                        m_expressionIndex = (m_expressionIndex - 1 + m_character.Expressions.Count) %
                            m_character.Expressions.Count;
                    }
                    if (GUILayout.Button("Next Emotion"))
                    {
                        m_expressionIndex = (m_expressionIndex + 1) % m_character.Expressions.Count;
                    }
                }
            }

            GUI.backgroundColor = new Color(0.35f, 0.9f, 0.72f, 1f);
            if (GUILayout.Button("Save Settings", GUILayout.Height(30f)))
            {
                SaveCharacterAsset(true);
            }
            GUI.backgroundColor = Color.white;
            if (!string.IsNullOrEmpty(m_saveStatus) &&
                EditorApplication.timeSinceStartup < m_saveStatusUntil)
            {
                EditorGUILayout.HelpBox(m_saveStatus, MessageType.None);
            }

            m_previewAspect = EditorGUILayout.Slider("Preview Aspect", m_previewAspect, 0.5f, 2.5f);
            if (selectedSprite == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a sprite or animation frames to this layer in the Character Inspector. " +
                    "Other assigned layers will still appear in the composite preview.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("In-game composite preview", EditorStyles.boldLabel);
            float width = Mathf.Max(100f, position.width - 24f);
            float height = Mathf.Min(360f, width / Mathf.Max(0.1f, m_previewAspect));
            Rect preview = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(true));
            DrawCompositePreview(preview, expression, previewTime, framing);
            HandlePreviewInput(preview, framing);
            EditorGUILayout.HelpBox(
                "Click or drag to place the selected layer. Use the mouse wheel to scale it. " +
                "Layer Offset supports tightly cropped eye and mouth sprites; Sprite Anchor chooses which point inside that sprite is attached.",
                MessageType.None);
        }

        private NovelCharacterExpression GetSelectedExpression()
        {
            if (m_character == null ||
                m_character.Expressions == null ||
                m_character.Expressions.Count == 0)
            {
                return null;
            }

            m_expressionIndex = Mathf.Clamp(m_expressionIndex, 0, m_character.Expressions.Count - 1);
            return m_character.Expressions[m_expressionIndex];
        }

        private bool TryGetSelectedLayer(
            NovelCharacterExpression expression,
            float elapsed,
            out NovelCharacterFraming framing,
            out Sprite sprite)
        {
            framing = null;
            sprite = null;
            switch (m_layer)
            {
                case PortraitLayer.Body:
                    framing = m_character.Body.Framing;
                    sprite = m_character.Body.GetFrame(elapsed);
                    return true;
                case PortraitLayer.EmotionEyes:
                    if (expression == null) return false;
                    framing = expression.Eyes.Framing;
                    sprite = expression.Eyes.GetFrame(elapsed);
                    return true;
                case PortraitLayer.EmotionMouth:
                    if (expression == null) return false;
                    framing = expression.Mouth.Framing;
                    sprite = expression.Mouth.GetFrame(elapsed, m_previewTalking);
                    return true;
                case PortraitLayer.LegacyPortrait:
                    if (expression == null) return false;
                    framing = expression.Framing;
                    sprite = expression.GetFrame(elapsed);
                    return true;
                default:
                    return false;
            }
        }

        private void DrawCompositePreview(
            Rect preview,
            NovelCharacterExpression expression,
            float elapsed,
            NovelCharacterFraming selectedFraming)
        {
            EditorGUI.DrawRect(preview, new Color(0.08f, 0.09f, 0.1f, 1f));
            GUI.BeginGroup(preview);
            if (m_character.UsesLayeredPortrait(expression))
            {
                DrawLayer(
                    new Rect(Vector2.zero, preview.size),
                    m_character.Body.GetFrame(elapsed),
                    m_character.Body.Framing);
                if (expression != null)
                {
                    DrawLayer(
                        new Rect(Vector2.zero, preview.size),
                        expression.Eyes.GetFrame(elapsed),
                        expression.Eyes.Framing);
                    DrawLayer(
                        new Rect(Vector2.zero, preview.size),
                        expression.Mouth.GetFrame(elapsed, m_previewTalking),
                        expression.Mouth.Framing);
                }
            }
            else if (expression != null)
            {
                DrawLayer(
                    new Rect(Vector2.zero, preview.size),
                    expression.GetFrame(elapsed),
                    expression.Framing);
            }
            GUI.EndGroup();

            Vector2 target = preview.center + new Vector2(
                selectedFraming.Offset.x * preview.height,
                -selectedFraming.Offset.y * preview.height);
            Handles.BeginGUI();
            Handles.color = Color.cyan;
            Handles.DrawLine(new Vector3(target.x - 11f, target.y),
                new Vector3(target.x + 11f, target.y));
            Handles.DrawLine(new Vector3(target.x, target.y - 11f),
                new Vector3(target.x, target.y + 11f));
            Handles.DrawWireDisc(target, Vector3.forward, 7f);
            Handles.EndGUI();
        }

        private static void DrawLayer(Rect preview, Sprite sprite, NovelCharacterFraming framing)
        {
            if (sprite == null || sprite.texture == null || framing == null)
            {
                return;
            }

            Rect spriteRect = sprite.rect;
            float scale = preview.height /
                (2f * framing.Radius * Mathf.Max(1f, spriteRect.height));
            float width = spriteRect.width * scale;
            float height = spriteRect.height * scale;
            bool flip = framing.FlipX;
            float pointX = flip ? 1f - framing.Point.x : framing.Point.x;
            Vector2 target = preview.center + new Vector2(
                framing.Offset.x * preview.height,
                -framing.Offset.y * preview.height);
            Rect destination = new Rect(
                target.x - pointX * width,
                target.y - (1f - framing.Point.y) * height,
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

        private void HandlePreviewInput(Rect preview, NovelCharacterFraming framing)
        {
            Event current = Event.current;
            int controlId = GUIUtility.GetControlID(
                "NovelifyCharacterFramingPreview".GetHashCode(), FocusType.Passive, preview);
            if (!preview.Contains(current.mousePosition))
            {
                if (current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                }
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                GUIUtility.hotControl = controlId;
                Vector2 relative = current.mousePosition - preview.center;
                Vector2 offset = new Vector2(
                    relative.x / preview.height,
                    -relative.y / preview.height);
                SaveFraming(framing, framing.Point, framing.Radius, offset, framing.FlipX);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag &&
                     current.button == 0 &&
                     GUIUtility.hotControl == controlId)
            {
                Vector2 offset = framing.Offset + new Vector2(
                    current.delta.x / preview.height,
                    -current.delta.y / preview.height);
                SaveFraming(framing, framing.Point, framing.Radius, offset, framing.FlipX);
                current.Use();
            }
            else if (current.type == EventType.MouseUp &&
                     current.button == 0 &&
                     GUIUtility.hotControl == controlId)
            {
                GUIUtility.hotControl = 0;
                current.Use();
            }
            else if (current.type == EventType.ScrollWheel)
            {
                float radius = framing.Radius * (1f + current.delta.y * 0.06f);
                SaveFraming(
                    framing,
                    framing.Point,
                    Mathf.Clamp(radius, 0.01f, 20f),
                    framing.Offset,
                    framing.FlipX);
                current.Use();
            }
        }

        private void SaveFraming(
            NovelCharacterFraming framing,
            Vector2 point,
            float radius,
            Vector2 offset,
            bool flipX)
        {
            if (m_character == null || framing == null)
            {
                return;
            }

            Undo.RecordObject(m_character, "Edit Character Layer Framing");
            framing.Set(point, radius, offset, flipX);
            EditorUtility.SetDirty(m_character);
            AssetDatabase.SaveAssetIfDirty(m_character);
            m_saveStatus = "Framing saved to the Character asset.";
            m_saveStatusUntil = EditorApplication.timeSinceStartup + 1.5d;
        }

        private void SaveCharacterAsset(bool showStatus = false)
        {
            if (m_character == null)
            {
                return;
            }

            EditorUtility.SetDirty(m_character);
            AssetDatabase.SaveAssetIfDirty(m_character);
            if (showStatus)
            {
                m_saveStatus = "All character settings saved.";
                m_saveStatusUntil = EditorApplication.timeSinceStartup + 2d;
            }
        }
    }
}
