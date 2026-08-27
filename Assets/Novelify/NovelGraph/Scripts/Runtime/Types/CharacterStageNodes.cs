using UnityEngine;

namespace NovelGraph
{
    [System.Serializable]
    [NodeInfo("Show Character", "Characters/Show Character", true, true, false, false, false, false,
        description: "Adds a character to the stage at a named screen position, with an optional fade and expression.")]
    public class ShowCharacterNode : NovelGraphNode
    {
        [ExposedProperty, Tooltip("Character asset to place on screen.")]
        public NovelCharacter character;
        [ExposedProperty, Tooltip("Expression ID to display. Leave empty to use the character's default expression.")]
        public string expression;
        [ExposedProperty, Tooltip("Horizontal stage slot used by this character.")]
        public NovelCharacterPosition stagePosition = NovelCharacterPosition.Center;
        [ExposedProperty, Min(0f), Tooltip("Seconds used to fade and move the character into place.")]
        public float transitionDuration = 0.2f;
        [ExposedProperty, Min(0.05f), Tooltip("Additional display scale applied after render framing.")]
        public float scale = 1f;
        [ExposedProperty, Tooltip("Mirror the rendered expression in addition to its saved framing setting.")]
        public bool flipX;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.StageCharacter(NovelCharacterStageCommand.Show(character, expression, stagePosition,
                transitionDuration, scale, flipX));
            return base.Execute(context);
        }
    }

    [System.Serializable]
    [NodeInfo("Move Character", "Characters/Move Character", true, true, false, false, false, false,
        description: "Slides a visible character to another left, centre, or right stage slot.")]
    public class MoveCharacterNode : NovelGraphNode
    {
        [ExposedProperty, Tooltip("Visible character to move.")]
        public NovelCharacter character;
        [ExposedProperty, Tooltip("Destination horizontal stage slot.")]
        public NovelCharacterPosition stagePosition = NovelCharacterPosition.Center;
        [ExposedProperty, Min(0f), Tooltip("Seconds used for the move. Zero moves immediately.")]
        public float duration = 0.35f;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.StageCharacter(NovelCharacterStageCommand.Move(character, stagePosition, duration));
            return base.Execute(context);
        }
    }

    [System.Serializable]
    [NodeInfo("Set Emotion", "Characters/Set Emotion", true, true, false, false, false, false,
        description: "Switches a visible character to a named expression such as neutral, talking, or shocked.")]
    public class SetCharacterExpressionNode : NovelGraphNode
    {
        [ExposedProperty, Tooltip("Visible character whose expression should change.")]
        public NovelCharacter character;
        [ExposedProperty, Tooltip("Case-insensitive expression ID configured on the Character asset.")]
        public string expression = "neutral";

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.StageCharacter(NovelCharacterStageCommand.SetExpression(character, expression));
            return base.Execute(context);
        }
    }

    [System.Serializable]
    [NodeInfo("Animate Character", "Characters/Animate Character", true, true, false, false, false, false,
        description: "Plays a simple talking, shocked, shaking, bouncing, jumping, or pulsing motion on a visible character.")]
    public class AnimateCharacterNode : NovelGraphNode
    {
        [ExposedProperty, Tooltip("Visible character to animate.")]
        public NovelCharacter character;
        [ExposedProperty, Tooltip("Procedural motion to play on top of the current expression animation.")]
        public NovelCharacterMotion motion = NovelCharacterMotion.Talking;
        [ExposedProperty, Min(0f), Tooltip("Motion duration in seconds. A looping motion continues until replaced or hidden.")]
        public float duration = 0.5f;
        [ExposedProperty, Range(0f, 3f), Tooltip("Multiplier for the motion distance or pulse size.")]
        public float intensity = 1f;
        [ExposedProperty, Tooltip("Keep playing until another animation, dialogue, hide, or clear command replaces it.")]
        public bool loop;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.StageCharacter(NovelCharacterStageCommand.Animate(character, motion, duration, intensity, loop));
            return base.Execute(context);
        }
    }

    [System.Serializable]
    [NodeInfo("Hide Character", "Characters/Hide Character", true, true, false, false, false, false,
        description: "Fades one staged character out without affecting the others.")]
    public class HideCharacterNode : NovelGraphNode
    {
        [ExposedProperty, Tooltip("Visible character to remove from the stage.")]
        public NovelCharacter character;
        [ExposedProperty, Min(0f), Tooltip("Seconds used to fade the character out.")]
        public float duration = 0.2f;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.StageCharacter(NovelCharacterStageCommand.Hide(character, duration));
            return base.Execute(context);
        }
    }

    [System.Serializable]
    [NodeInfo("Clear Characters", "Characters/Clear Characters", true, true, false, false, false, false,
        description: "Fades every character off the stage, which is useful between scenes.")]
    public class ClearCharactersNode : NovelGraphNode
    {
        [ExposedProperty, Min(0f), Tooltip("Seconds used to fade all staged characters out.")]
        public float duration = 0.2f;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.StageCharacter(NovelCharacterStageCommand.Clear(duration));
            return base.Execute(context);
        }
    }

    [System.Serializable]
    [NodeInfo("Focus Character", "Characters/Focus Character", true, true, false, false, false, false,
        description: "Highlights one staged character and dims the others; leave Character empty to remove focus.")]
    public class FocusCharacterNode : NovelGraphNode
    {
        [ExposedProperty, Tooltip("Character to highlight. Leave empty to return every character to full brightness.")]
        public NovelCharacter character;
        [ExposedProperty, Min(0f), Tooltip("Seconds reserved for presentation implementations that animate the focus change.")]
        public float duration = 0.15f;

        public override NovelNodeResult Execute(NovelGraphContext context)
        {
            context.StageCharacter(NovelCharacterStageCommand.Focus(character, duration));
            return base.Execute(context);
        }
    }
}
