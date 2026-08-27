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
