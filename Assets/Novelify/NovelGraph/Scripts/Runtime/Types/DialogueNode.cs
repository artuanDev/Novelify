using UnityEngine;

namespace NovelGraph
{
    public enum NovelSpeakerMode
    {
        Custom,
        Narrator,
        Character
    }

    [System.Serializable]
    [NodeInfo("Dialogue", "Story/Dialogue", true, true, false, false, false, true,
        description: "Shows a narrated or character-spoken line and waits for the player to continue.")]
    public class DialogueNode : NovelGraphNode
    {
        [ExposedProperty, Tooltip("Select whether this line belongs to the narrator, a Character asset, or a custom speaker name.")]
        public NovelSpeakerMode speakerMode;

        [ExposedProperty, Tooltip("Character speaking this line when Speaker Mode is Character.")]
        public NovelCharacter character;

        [ExposedProperty, Tooltip("Speaker name used only when Speaker Mode is Custom.")]
        public string speaker;

        [ExposedProperty, TextArea(3, 9), Tooltip("Dialogue text shown to the player.")]
        public string dialogue;

        [ExposedProperty, Tooltip("Play the selected character's voice sound as each non-whitespace letter appears.")]
        public bool playLetterSounds = true;

        [ExposedProperty, Min(1f), Tooltip("Number of dialogue characters revealed per second.")]
        public float charactersPerSecond = 36f;

        [ExposedProperty, Tooltip("Automatically place the selected Character on the stage for this line.")]
        public bool showCharacter = true;

        [ExposedProperty, Tooltip("Emotion ID displayed while this line is spoken, such as neutral or shocked. Talking mouth frames are automatic. Leave empty for the character default.")]
        public string expression;

        [ExposedProperty, Tooltip("Stage slot used when automatically showing the speaking character.")]
        public NovelCharacterPosition characterPosition = NovelCharacterPosition.Center;

        [ExposedProperty, Min(0f), Tooltip("Seconds used to bring the speaking character to the requested slot.")]
        public float characterTransitionDuration = 0.2f;

        [ExposedProperty, Tooltip("Optional whole-character motion played while this line types. Mouth animation is controlled separately and remains automatic.")]
        public NovelCharacterMotion speakingMotion = NovelCharacterMotion.Talking;

        [ExposedProperty, Range(0f, 3f), Tooltip("Strength of the speaking motion.")]
        public float motionIntensity = 1f;

        [ExposedProperty, Tooltip("Highlight the speaker and dim other staged characters during this line.")]
        public bool focusSpeaker = true;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            string resolvedSpeaker = speaker;
            NovelCharacter resolvedCharacter = null;
            if (speakerMode == NovelSpeakerMode.Narrator)
            {
                resolvedSpeaker = "Narrator";
            }
            else if (speakerMode == NovelSpeakerMode.Character)
            {
                resolvedCharacter = character;
                resolvedSpeaker = character != null ? character.DisplayName : "Missing Character";
                if (character != null && showCharacter)
                {
                    context.StageCharacter(NovelCharacterStageCommand.Show(
                        character, expression, characterPosition, characterTransitionDuration, 1f, false));
                }
                if (character != null)
                {
                    if (focusSpeaker)
                    {
                        context.StageCharacter(NovelCharacterStageCommand.Focus(character, characterTransitionDuration));
                    }
                    context.StageCharacter(NovelCharacterStageCommand.Animate(
                        character, speakingMotion, 0f, motionIntensity, speakingMotion != NovelCharacterMotion.None));
                }
            }

            return NovelNodeResult.Dialogue(
                resolvedSpeaker,
                dialogue,
                context.Graph.GetNodeIdFromOutput(id, 0),
                resolvedCharacter,
                resolvedCharacter != null && playLetterSounds,
                Mathf.Max(1f, charactersPerSecond));
        }
    }
}
