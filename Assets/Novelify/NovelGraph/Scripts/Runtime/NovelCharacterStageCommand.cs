using UnityEngine;

namespace NovelGraph
{
    public enum NovelCharacterPosition
    {
        FarLeft,
        Left,
        Center,
        Right,
        FarRight
    }

    public enum NovelCharacterMotion
    {
        None,
        Talking,
        Shocked,
        Shake,
        Bounce,
        Jump,
        Pulse
    }

    public enum NovelCharacterStageCommandType
    {
        Show,
        Move,
        SetExpression,
        Animate,
        Hide,
        Clear,
        Focus
    }

    public readonly struct NovelCharacterStageCommand
    {
        public NovelCharacterStageCommandType Type { get; }
        public NovelCharacter Character { get; }
        public string Expression { get; }
        public NovelCharacterPosition Position { get; }
        public NovelCharacterMotion Motion { get; }
        public float Duration { get; }
        public float Intensity { get; }
        public float Scale { get; }
        public bool FlipX { get; }
        public bool Loop { get; }

        private NovelCharacterStageCommand(
            NovelCharacterStageCommandType type,
            NovelCharacter character = null,
            string expression = "",
            NovelCharacterPosition position = NovelCharacterPosition.Center,
            NovelCharacterMotion motion = NovelCharacterMotion.None,
            float duration = 0f,
            float intensity = 1f,
            float scale = 1f,
            bool flipX = false,
            bool loop = false)
        {
            Type = type;
            Character = character;
            Expression = expression ?? string.Empty;
            Position = position;
            Motion = motion;
            Duration = Mathf.Max(0f, duration);
            Intensity = Mathf.Max(0f, intensity);
            Scale = Mathf.Max(0.05f, scale);
            FlipX = flipX;
            Loop = loop;
        }

        public static NovelCharacterStageCommand Show(NovelCharacter character, string expression,
            NovelCharacterPosition position, float duration, float scale, bool flipX) =>
            new NovelCharacterStageCommand(NovelCharacterStageCommandType.Show, character, expression,
                position, duration: duration, scale: scale, flipX: flipX);

        public static NovelCharacterStageCommand Move(NovelCharacter character,
            NovelCharacterPosition position, float duration) =>
            new NovelCharacterStageCommand(NovelCharacterStageCommandType.Move, character,
                position: position, duration: duration);

        public static NovelCharacterStageCommand SetExpression(NovelCharacter character, string expression) =>
            new NovelCharacterStageCommand(NovelCharacterStageCommandType.SetExpression, character, expression);

        public static NovelCharacterStageCommand Animate(NovelCharacter character, NovelCharacterMotion motion,
            float duration, float intensity, bool loop) =>
            new NovelCharacterStageCommand(NovelCharacterStageCommandType.Animate, character, motion: motion,
                duration: duration, intensity: intensity, loop: loop);

        public static NovelCharacterStageCommand Hide(NovelCharacter character, float duration) =>
            new NovelCharacterStageCommand(NovelCharacterStageCommandType.Hide, character, duration: duration);

        public static NovelCharacterStageCommand Clear(float duration) =>
            new NovelCharacterStageCommand(NovelCharacterStageCommandType.Clear, duration: duration);

        public static NovelCharacterStageCommand Focus(NovelCharacter character, float duration) =>
            new NovelCharacterStageCommand(NovelCharacterStageCommandType.Focus, character, duration: duration);
    }
}
