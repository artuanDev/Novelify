using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NovelGraph.Editor
{
    public static class NovelifyShowcaseContent
    {
        private const string SampleRoot = "Assets/Novelify/Samples";
        private const string PortraitRoot = SampleRoot + "/Portraits";
        private const string GraphPath = SampleRoot + "/Stories/TheGlassMoonProtocol.asset";
        private const string ScenePath = SampleRoot + "/Scenes/EverythingShowcase.unity";
        private const string LyraPath = SampleRoot + "/Characters/Lyra.asset";
        private const string OrinPath = SampleRoot + "/Characters/Orin.asset";
        private const string OriginalScenePath = SampleRoot + "/Scenes/DecisionEventsSample.unity";

        private enum PortraitEmotion
        {
            Neutral,
            TalkingClosed,
            TalkingOpen,
            Shocked,
            Determined,
            Suspicious
        }

        [InitializeOnLoadMethod]
        private static void CreateMissingShowcaseAfterImport()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null ||
                NeedsLayeredShowcaseUpgrade())
            {
                EditorApplication.delayCall += CreateShowcaseContent;
            }
        }

        private static bool NeedsLayeredShowcaseUpgrade()
        {
            NovelCharacter lyra = AssetDatabase.LoadAssetAtPath<NovelCharacter>(LyraPath);
            NovelCharacter orin = AssetDatabase.LoadAssetAtPath<NovelCharacter>(OrinPath);
            return NeedsLayeredUpgrade(lyra) || NeedsLayeredUpgrade(orin);
        }

        private static bool NeedsLayeredUpgrade(NovelCharacter character)
        {
            if (character == null || !character.Body.HasVisual ||
                character.Expressions == null || character.Expressions.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < character.Expressions.Count; i++)
            {
                NovelCharacterExpression emotion = character.Expressions[i];
                if (emotion == null ||
                    string.Equals(emotion.Id, "talking", StringComparison.OrdinalIgnoreCase) ||
                    !emotion.Eyes.HasVisual ||
                    !emotion.Mouth.HasVisual)
                {
                    return true;
                }
            }
            return false;
        }

        [MenuItem("Tools/Novelify/Create or Refresh Everything Showcase")]
        public static void CreateShowcaseContent()
        {
            EnsureFolders();
            NovelCharacter lyra = GetOrCreateCharacter(
                LyraPath, "Lyra Quill", 610f, new Color(0.16f, 0.78f, 0.78f, 1f));
            NovelCharacter orin = GetOrCreateCharacter(
                OrinPath, "Orin Voss", 245f, new Color(0.86f, 0.46f, 0.92f, 1f));
            ConfigureLyra(lyra);
            ConfigureOrin(orin);

            NovelGraphAsset graph = AssetDatabase.LoadAssetAtPath<NovelGraphAsset>(GraphPath);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<NovelGraphAsset>();
                AssetDatabase.CreateAsset(graph, GraphPath);
            }

            BuildGraph(graph, lyra, orin);
            CreateScene(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Novelify everything showcase created: {ScenePath}");
        }

        public static void CreateShowcaseContentBatch()
        {
            CreateShowcaseContent();
        }

        private static void ConfigureLyra(NovelCharacter character)
        {
            Color suit = new Color(0.08f, 0.55f, 0.6f, 1f);
            Color hair = new Color(0.04f, 0.12f, 0.2f, 1f);
            Color skin = new Color(0.94f, 0.7f, 0.58f, 1f);
            Sprite body = GeneratePortrait(
                "LyraBody", suit, hair, skin, PortraitEmotion.Neutral, true, false, false);
            Sprite neutralEyes = GeneratePortrait(
                "LyraNeutralEyes", suit, hair, skin, PortraitEmotion.Neutral, false, true, false);
            Sprite neutralMouth = GeneratePortrait(
                "LyraNeutralMouth", suit, hair, skin, PortraitEmotion.Neutral, false, false, true);
            Sprite neutralTalk = GeneratePortrait(
                "LyraNeutralTalk", suit, hair, skin, PortraitEmotion.TalkingOpen, false, false, true);
            Sprite shockedEyes = GeneratePortrait(
                "LyraShockedEyes", suit, hair, skin, PortraitEmotion.Shocked, false, true, false);
            Sprite shockedMouth = GeneratePortrait(
                "LyraShockedMouth", suit, hair, skin, PortraitEmotion.Shocked, false, false, true);
            Sprite determinedEyes = GeneratePortrait(
                "LyraDeterminedEyes", suit, hair, skin, PortraitEmotion.Determined, false, true, false);
            Sprite determinedMouth = GeneratePortrait(
                "LyraDeterminedMouth", suit, hair, skin, PortraitEmotion.Determined, false, false, true);

            character.ConfigureBody(BodyLayer(body));
            character.ConfigureExpressions("neutral",
                Emotion("neutral", neutralEyes, neutralMouth, neutralTalk, 7f),
                Emotion("shocked", shockedEyes, shockedMouth, neutralTalk, 7f),
                Emotion("determined", determinedEyes, determinedMouth, neutralTalk, 7f));
            EditorUtility.SetDirty(character);
        }

        private static void ConfigureOrin(NovelCharacter character)
        {
            Color suit = new Color(0.46f, 0.18f, 0.58f, 1f);
            Color hair = new Color(0.72f, 0.76f, 0.86f, 1f);
            Color skin = new Color(0.62f, 0.82f, 0.88f, 1f);
            Sprite body = GeneratePortrait(
                "OrinBody", suit, hair, skin, PortraitEmotion.Neutral, true, false, false);
            Sprite neutralEyes = GeneratePortrait(
                "OrinNeutralEyes", suit, hair, skin, PortraitEmotion.Neutral, false, true, false);
            Sprite neutralMouth = GeneratePortrait(
                "OrinNeutralMouth", suit, hair, skin, PortraitEmotion.Neutral, false, false, true);
            Sprite neutralTalk = GeneratePortrait(
                "OrinNeutralTalk", suit, hair, skin, PortraitEmotion.TalkingOpen, false, false, true);
            Sprite shockedEyes = GeneratePortrait(
                "OrinShockedEyes", suit, hair, skin, PortraitEmotion.Shocked, false, true, false);
            Sprite shockedMouth = GeneratePortrait(
                "OrinShockedMouth", suit, hair, skin, PortraitEmotion.Shocked, false, false, true);
            Sprite suspiciousEyes = GeneratePortrait(
                "OrinSuspiciousEyes", suit, hair, skin, PortraitEmotion.Suspicious, false, true, false);
            Sprite suspiciousMouth = GeneratePortrait(
                "OrinSuspiciousMouth", suit, hair, skin, PortraitEmotion.Suspicious, false, false, true);

            character.ConfigureBody(BodyLayer(body));
            character.ConfigureExpressions("neutral",
                Emotion("neutral", neutralEyes, neutralMouth, neutralTalk, 6f),
                Emotion("shocked", shockedEyes, shockedMouth, neutralTalk, 6f),
                Emotion("suspicious", suspiciousEyes, suspiciousMouth, neutralTalk, 6f));
            EditorUtility.SetDirty(character);
        }

        private static NovelCharacterSpriteLayer BodyLayer(Sprite sprite)
        {
            NovelCharacterSpriteLayer layer = new NovelCharacterSpriteLayer();
            layer.Configure(
                sprite,
                Array.Empty<Sprite>(),
                1f,
                true,
                new Vector2(0.5f, 0.58f),
                0.47f,
                Vector2.zero);
            return layer;
        }

        private static NovelCharacterExpression Emotion(
            string id,
            Sprite eyes,
            Sprite idleMouth,
            Sprite talkingMouth,
            float framesPerSecond)
        {
            NovelCharacterSpriteLayer eyeLayer = new NovelCharacterSpriteLayer();
            eyeLayer.Configure(
                eyes,
                Array.Empty<Sprite>(),
                1f,
                true,
                new Vector2(0.5f, 0.58f),
                0.47f,
                Vector2.zero);
            NovelCharacterMouthLayer mouthLayer = new NovelCharacterMouthLayer();
            mouthLayer.Configure(
                idleMouth,
                Array.Empty<Sprite>(),
                new[] { idleMouth, talkingMouth },
                framesPerSecond,
                new Vector2(0.5f, 0.58f),
                0.47f,
                Vector2.zero);
            NovelCharacterExpression expression = new NovelCharacterExpression();
            expression.ConfigureLayers(id, eyeLayer, mouthLayer);
            return expression;
        }

        private static NovelCharacter GetOrCreateCharacter(
            string path,
            string displayName,
            float frequency,
            Color nameColor)
        {
            NovelCharacter character = AssetDatabase.LoadAssetAtPath<NovelCharacter>(path);
            if (character == null)
            {
                character = ScriptableObject.CreateInstance<NovelCharacter>();
                AssetDatabase.CreateAsset(character, path);
            }
            character.Configure(displayName, frequency, nameColor);
            EditorUtility.SetDirty(character);
            return character;
        }

        private static void BuildGraph(
            NovelGraphAsset graph,
            NovelCharacter lyra,
            NovelCharacter orin)
        {
            graph.Nodes.Clear();
            graph.Connections.Clear();

            StartNode start = Add(graph, new StartNode(), 40f, 320f);
            DebugLogNode debugLog = Add(graph, new DebugLogNode
            {
                logMessage = "Everything Showcase started: The Glass Moon Protocol."
            }, 300f, 320f);
            DebugValue debugValue = Add(graph, new DebugValue { value = 404 }, 560f, 320f);
            DialogueNode opening = Narration(graph,
                "Moonbase Vesper, 03:17. The artificial moon above the colony has begun answering questions nobody asked.",
                820f, 320f);
            ShowCharacterNode showLyra = Add(graph, new ShowCharacterNode
            {
                character = lyra,
                expression = "neutral",
                stagePosition = NovelCharacterPosition.Left,
                transitionDuration = 0.45f
            }, 1100f, 220f);
            ShowCharacterNode showOrin = Add(graph, new ShowCharacterNode
            {
                character = orin,
                expression = "neutral",
                stagePosition = NovelCharacterPosition.Right,
                transitionDuration = 0.45f,
                flipX = true
            }, 1100f, 420f);
            FocusCharacterNode focusLyra = Add(graph, new FocusCharacterNode
            {
                character = lyra,
                duration = 0.2f
            }, 1380f, 320f);
            DialogueNode lyraOpening = CharacterLine(graph, lyra,
                "Orin, the lunar reactor is singing through every emergency channel. Tell me that is your diagnostic.",
                "neutral", NovelCharacterPosition.Left, NovelCharacterMotion.Talking, true, 34f, 1660f, 320f);
            SetCharacterExpressionNode shockOrin = Add(graph, new SetCharacterExpressionNode
            {
                character = orin,
                expression = "shocked"
            }, 1940f, 260f);
            AnimateCharacterNode shockMotion = Add(graph, new AnimateCharacterNode
            {
                character = orin,
                motion = NovelCharacterMotion.Shocked,
                duration = 0.9f,
                intensity = 1.25f
            }, 1940f, 460f);
            DialogueNode orinWarning = CharacterLine(graph, orin,
                "That is not a diagnostic. The signal just used my childhood access phrase.",
                "shocked", NovelCharacterPosition.Right, NovelCharacterMotion.Shake, true, 29f, 2220f, 320f);
            MoveCharacterNode moveLyra = Add(graph, new MoveCharacterNode
            {
                character = lyra,
                stagePosition = NovelCharacterPosition.Center,
                duration = 0.5f
            }, 2500f, 220f);
            SetCharacterExpressionNode determinedLyra = Add(graph, new SetCharacterExpressionNode
            {
                character = lyra,
                expression = "determined"
            }, 2500f, 420f);
            AnimateCharacterNode jumpLyra = Add(graph, new AnimateCharacterNode
            {
                character = lyra,
                motion = NovelCharacterMotion.Jump,
                duration = 0.7f,
                intensity = 0.55f
            }, 2780f, 220f);
            DialogueNode lyraDecision = CharacterLine(graph, lyra,
                "Then we stop guessing. Four systems, four risks, and about one minute before the glass moon opens.",
                "determined", NovelCharacterPosition.Center, NovelCharacterMotion.Bounce, true, 36f, 3060f, 320f);
            CallFunctionNode alarm = Add(graph, new CallFunctionNode
            {
                functionId = "activate_station_alarm"
            }, 3340f, 320f);
            MultiChoiceNode operation = Add(graph, new MultiChoiceNode
            {
                prompt = "Choose the Glass Moon Protocol. Save here, then replay every route to test the complete showcase.",
                choiceA = "Stabilize the lunar reactor",
                choiceB = "Decode and answer the impossible signal",
                choiceC = "Evacuate in the last escape pod",
                choiceD = "Touch the anomaly before it opens",
                stateKey = "glass_moon_route"
            }, 3620f, 320f);

            graph.Connect(start, 0, debugLog);
            graph.Connect(debugLog, 0, debugValue);
            graph.Connect(debugValue, 0, opening);
            graph.Connect(opening, 0, showLyra);
            graph.Connect(showLyra, 0, showOrin);
            graph.Connect(showOrin, 0, focusLyra);
            graph.Connect(focusLyra, 0, lyraOpening);
            graph.Connect(lyraOpening, 0, shockOrin);
            graph.Connect(shockOrin, 0, shockMotion);
            graph.Connect(shockMotion, 0, orinWarning);
            graph.Connect(orinWarning, 0, moveLyra);
            graph.Connect(moveLyra, 0, determinedLyra);
            graph.Connect(determinedLyra, 0, jumpLyra);
            graph.Connect(jumpLyra, 0, lyraDecision);
            graph.Connect(lyraDecision, 0, alarm);
            graph.Connect(alarm, 0, operation);

            BuildReactorRoute(graph, operation, lyra, orin);
            BuildSignalRoute(graph, operation, lyra, orin);
            BuildEvacuationRoute(graph, operation, lyra, orin);
            BuildAnomalyRoute(graph, operation, lyra, orin);
            EditorUtility.SetDirty(graph);
        }

        private static void BuildReactorRoute(
            NovelGraphAsset graph,
            MultiChoiceNode operation,
            NovelCharacter lyra,
            NovelCharacter orin)
        {
            FocusCharacterNode focus = Add(graph, new FocusCharacterNode
            {
                character = lyra
            }, 3960f, -720f);
            SetCharacterExpressionNode expression = Add(graph, new SetCharacterExpressionNode
            {
                character = lyra,
                expression = "determined"
            }, 4240f, -720f);
            AnimateCharacterNode pulse = Add(graph, new AnimateCharacterNode
            {
                character = lyra,
                motion = NovelCharacterMotion.Pulse,
                duration = 1.2f,
                intensity = 1.35f
            }, 4520f, -720f);
            CallFunctionNode stabilize = Add(graph, new CallFunctionNode
            {
                functionId = "stabilize_moon_reactor"
            }, 4800f, -720f);
            SetIntNode setStable = Add(graph, new SetIntNode
            {
                key = "reactor_stable",
                value = 1
            }, 5080f, -720f);
            IntConditionNode verify = Add(graph, new IntConditionNode
            {
                key = "reactor_stable",
                expectedValue = 1
            }, 5360f, -720f);
            SignalNode stableSignal = Add(graph, new SignalNode
            {
                signal = "reactor_stable"
            }, 5640f, -800f);
            DialogueNode success = CharacterLine(graph, lyra,
                "Containment is holding. The moon is still singing, but now it has to breathe on our rhythm.",
                "determined", NovelCharacterPosition.Center, NovelCharacterMotion.Pulse, true, 37f, 5920f, -800f);
            HideCharacterNode hideOrin = Add(graph, new HideCharacterNode
            {
                character = orin,
                duration = 0.4f
            }, 6200f, -800f);
            EndNode successEnd = Add(graph, new EndNode(), 6480f, -800f);
            DialogueNode impossibleFailure = Narration(graph,
                "The diagnostic says the reactor is unstable. This branch exists to verify the Not Equal condition output.",
                5640f, -600f);
            EndNode failureEnd = Add(graph, new EndNode(), 5920f, -600f);

            graph.Connect(operation, 0, focus);
            graph.Connect(focus, 0, expression);
            graph.Connect(expression, 0, pulse);
            graph.Connect(pulse, 0, stabilize);
            graph.Connect(stabilize, 0, setStable);
            graph.Connect(setStable, 0, verify);
            graph.Connect(verify, 0, stableSignal);
            graph.Connect(stableSignal, 0, success);
            graph.Connect(success, 0, hideOrin);
            graph.Connect(hideOrin, 0, successEnd);
            graph.Connect(verify, 1, impossibleFailure);
            graph.Connect(impossibleFailure, 0, failureEnd);
        }

        private static void BuildSignalRoute(
            NovelGraphAsset graph,
            MultiChoiceNode operation,
            NovelCharacter lyra,
            NovelCharacter orin)
        {
            MoveCharacterNode moveOrin = Add(graph, new MoveCharacterNode
            {
                character = orin,
                stagePosition = NovelCharacterPosition.Center,
                duration = 0.55f
            }, 3960f, -160f);
            SetCharacterExpressionNode suspicious = Add(graph, new SetCharacterExpressionNode
            {
                character = orin,
                expression = "suspicious"
            }, 4240f, -160f);
            AnimateCharacterNode bounce = Add(graph, new AnimateCharacterNode
            {
                character = orin,
                motion = NovelCharacterMotion.Bounce,
                duration = 0.9f,
                intensity = 0.7f
            }, 4520f, -160f);
            DialogueNode decode = CharacterLine(graph, orin,
                "There. Beneath the reactor noise: coordinates, a heartbeat, and a request to be remembered.",
                "suspicious", NovelCharacterPosition.Center, NovelCharacterMotion.Talking, true, 31f, 4800f, -160f);
            TwoChoiceNode reply = Add(graph, new TwoChoiceNode
            {
                prompt = "The signal addresses you by name. How do you answer?",
                firstChoice = "Answer: We remember you",
                firstSignal = "signal_answered",
                firstValue = 1,
                secondChoice = "Jam the transmission",
                secondSignal = "signal_jammed",
                secondValue = 0,
                stateKey = "signal_reply"
            }, 5080f, -160f);
            IntConditionNode answerCondition = Add(graph, new IntConditionNode
            {
                key = "signal_reply",
                expectedValue = 1
            }, 5360f, -260f);
            IntConditionNode jamCondition = Add(graph, new IntConditionNode
            {
                key = "signal_reply",
                expectedValue = 1
            }, 5360f, -40f);

            CallFunctionNode openGate = Add(graph, new CallFunctionNode
            {
                functionId = "open_anomaly_gate"
            }, 5640f, -260f);
            DialogueNode chorus = CustomLine(graph, "THE CHORUS",
                "MEMORY ACCEPTED. BORROWED MOON RETURNING TO ORIGINAL ORBIT.",
                5920f, -260f);
            EndNode answerEnd = Add(graph, new EndNode(), 6200f, -260f);

            CallFunctionNode blackout = Add(graph, new CallFunctionNode
            {
                functionId = "trigger_station_blackout"
            }, 5640f, -40f);
            SetCharacterExpressionNode shockLyra = Add(graph, new SetCharacterExpressionNode
            {
                character = lyra,
                expression = "shocked"
            }, 5920f, -40f);
            AnimateCharacterNode shakeLyra = Add(graph, new AnimateCharacterNode
            {
                character = lyra,
                motion = NovelCharacterMotion.Shake,
                duration = 1.2f,
                intensity = 1.4f
            }, 6200f, -40f);
            DialogueNode jamEnding = Narration(graph,
                "The transmission dies. Every light follows it, leaving two silhouettes beneath a perfectly silent moon.",
                6480f, -40f);
            EndNode jamEnd = Add(graph, new EndNode(), 6760f, -40f);
            EndNode impossibleAnswerEnd = Add(graph, new EndNode(), 5640f, -420f);
            EndNode impossibleJamEnd = Add(graph, new EndNode(), 5640f, 140f);

            graph.Connect(operation, 1, moveOrin);
            graph.Connect(moveOrin, 0, suspicious);
            graph.Connect(suspicious, 0, bounce);
            graph.Connect(bounce, 0, decode);
            graph.Connect(decode, 0, reply);
            graph.Connect(reply, 0, answerCondition);
            graph.Connect(reply, 1, jamCondition);
            graph.Connect(answerCondition, 0, openGate);
            graph.Connect(answerCondition, 1, impossibleAnswerEnd);
            graph.Connect(openGate, 0, chorus);
            graph.Connect(chorus, 0, answerEnd);
            graph.Connect(jamCondition, 0, impossibleJamEnd);
            graph.Connect(jamCondition, 1, blackout);
            graph.Connect(blackout, 0, shockLyra);
            graph.Connect(shockLyra, 0, shakeLyra);
            graph.Connect(shakeLyra, 0, jamEnding);
            graph.Connect(jamEnding, 0, jamEnd);
        }

        private static void BuildEvacuationRoute(
            NovelGraphAsset graph,
            MultiChoiceNode operation,
            NovelCharacter lyra,
            NovelCharacter orin)
        {
            FocusCharacterNode focusOrin = Add(graph, new FocusCharacterNode
            {
                character = orin
            }, 3960f, 380f);
            MoveCharacterNode lyraLeft = Add(graph, new MoveCharacterNode
            {
                character = lyra,
                stagePosition = NovelCharacterPosition.FarLeft,
                duration = 0.55f
            }, 4240f, 300f);
            MoveCharacterNode orinRight = Add(graph, new MoveCharacterNode
            {
                character = orin,
                stagePosition = NovelCharacterPosition.FarRight,
                duration = 0.55f
            }, 4240f, 480f);
            AnimateCharacterNode lyraJump = Add(graph, new AnimateCharacterNode
            {
                character = lyra,
                motion = NovelCharacterMotion.Jump,
                duration = 0.8f,
                intensity = 0.7f
            }, 4520f, 300f);
            AnimateCharacterNode orinBounce = Add(graph, new AnimateCharacterNode
            {
                character = orin,
                motion = NovelCharacterMotion.Bounce,
                duration = 1.1f,
                intensity = 0.8f
            }, 4520f, 480f);
            DialogueNode quietOrin = CharacterLine(graph, orin,
                "One seat. You take it. I can keep the docking clamps open from here.",
                "neutral", NovelCharacterPosition.FarRight, NovelCharacterMotion.Talking, false, 27f, 4800f, 380f);
            HideCharacterNode hideLyra = Add(graph, new HideCharacterNode
            {
                character = lyra,
                duration = 0.55f
            }, 5080f, 300f);
            SignalNode evacuationSignal = Add(graph, new SignalNode
            {
                signal = "evacuation_started"
            }, 5080f, 480f);
            CallFunctionNode launch = Add(graph, new CallFunctionNode
            {
                functionId = "launch_escape_pod"
            }, 5360f, 380f);
            ClearCharactersNode clear = Add(graph, new ClearCharactersNode
            {
                duration = 0.7f
            }, 5640f, 380f);
            DialogueNode ending = Narration(graph,
                "The pod falls toward the colony. In its small rear window, Orin becomes a violet point against the opening moon.",
                5920f, 380f);
            EndNode end = Add(graph, new EndNode(), 6200f, 380f);

            graph.Connect(operation, 2, focusOrin);
            graph.Connect(focusOrin, 0, lyraLeft);
            graph.Connect(lyraLeft, 0, orinRight);
            graph.Connect(orinRight, 0, lyraJump);
            graph.Connect(lyraJump, 0, orinBounce);
            graph.Connect(orinBounce, 0, quietOrin);
            graph.Connect(quietOrin, 0, hideLyra);
            graph.Connect(hideLyra, 0, evacuationSignal);
            graph.Connect(evacuationSignal, 0, launch);
            graph.Connect(launch, 0, clear);
            graph.Connect(clear, 0, ending);
            graph.Connect(ending, 0, end);
        }

        private static void BuildAnomalyRoute(
            NovelGraphAsset graph,
            MultiChoiceNode operation,
            NovelCharacter lyra,
            NovelCharacter orin)
        {
            SetCharacterExpressionNode shockLyra = Add(graph, new SetCharacterExpressionNode
            {
                character = lyra,
                expression = "shocked"
            }, 3960f, 820f);
            SetCharacterExpressionNode shockOrin = Add(graph, new SetCharacterExpressionNode
            {
                character = orin,
                expression = "shocked"
            }, 3960f, 1020f);
            AnimateCharacterNode pulseLyra = Add(graph, new AnimateCharacterNode
            {
                character = lyra,
                motion = NovelCharacterMotion.Pulse,
                duration = 0f,
                intensity = 1.6f,
                loop = true
            }, 4240f, 820f);
            AnimateCharacterNode shockOrinMotion = Add(graph, new AnimateCharacterNode
            {
                character = orin,
                motion = NovelCharacterMotion.Shocked,
                duration = 1.5f,
                intensity = 1.4f
            }, 4240f, 1020f);
            FocusCharacterNode clearFocus = Add(graph, new FocusCharacterNode
            {
                character = null,
                duration = 0.2f
            }, 4520f, 920f);
            SignalNode touched = Add(graph, new SignalNode
            {
                signal = "anomaly_touched"
            }, 4800f, 920f);
            DialogueNode anomaly = CustomLine(graph, "THE GLASS MOON",
                "CONTACT CONFIRMED. TWO OBSERVERS. ONE MEMORY. OPENING THE DOOR BETWEEN.",
                5080f, 920f);
            CallFunctionNode blackout = Add(graph, new CallFunctionNode
            {
                functionId = "trigger_station_blackout"
            }, 5360f, 920f);
            ClearCharactersNode clear = Add(graph, new ClearCharactersNode
            {
                duration = 1.1f
            }, 5640f, 920f);
            DialogueNode ending = Narration(graph,
                "For one impossible second, every choice they did not make stands beside them in the dark.",
                5920f, 920f);
            EndNode end = Add(graph, new EndNode(), 6200f, 920f);

            graph.Connect(operation, 3, shockLyra);
            graph.Connect(shockLyra, 0, shockOrin);
            graph.Connect(shockOrin, 0, pulseLyra);
            graph.Connect(pulseLyra, 0, shockOrinMotion);
            graph.Connect(shockOrinMotion, 0, clearFocus);
            graph.Connect(clearFocus, 0, touched);
            graph.Connect(touched, 0, anomaly);
            graph.Connect(anomaly, 0, blackout);
            graph.Connect(blackout, 0, clear);
            graph.Connect(clear, 0, ending);
            graph.Connect(ending, 0, end);
        }

        private static DialogueNode Narration(
            NovelGraphAsset graph,
            string text,
            float x,
            float y)
        {
            return Add(graph, new DialogueNode
            {
                speakerMode = NovelSpeakerMode.Narrator,
                dialogue = text,
                playLetterSounds = false,
                charactersPerSecond = 42f,
                showCharacter = false,
                focusSpeaker = false,
                speakingMotion = NovelCharacterMotion.None
            }, x, y);
        }

        private static DialogueNode CustomLine(
            NovelGraphAsset graph,
            string speaker,
            string text,
            float x,
            float y)
        {
            return Add(graph, new DialogueNode
            {
                speakerMode = NovelSpeakerMode.Custom,
                speaker = speaker,
                dialogue = text,
                playLetterSounds = false,
                charactersPerSecond = 25f,
                showCharacter = false,
                focusSpeaker = false,
                speakingMotion = NovelCharacterMotion.None
            }, x, y);
        }

        private static DialogueNode CharacterLine(
            NovelGraphAsset graph,
            NovelCharacter character,
            string text,
            string expression,
            NovelCharacterPosition position,
            NovelCharacterMotion motion,
            bool playLetterSounds,
            float charactersPerSecond,
            float x,
            float y)
        {
            return Add(graph, new DialogueNode
            {
                speakerMode = NovelSpeakerMode.Character,
                character = character,
                dialogue = text,
                playLetterSounds = playLetterSounds,
                charactersPerSecond = charactersPerSecond,
                showCharacter = true,
                expression = expression,
                characterPosition = position,
                characterTransitionDuration = 0.3f,
                speakingMotion = motion,
                motionIntensity = 1f,
                focusSpeaker = true
            }, x, y);
        }

        private static T Add<T>(
            NovelGraphAsset graph,
            T node,
            float x,
            float y) where T : NovelGraphNode
        {
            node.SetPosition(new Rect(x, y, 250f, 170f));
            graph.AddNode(node);
            return node;
        }

        private static void CreateScene(NovelGraphAsset graph)
        {
            Scene previousScene = SceneManager.GetActiveScene();
            bool replaceEmptyScene = previousScene.IsValid() &&
                                     string.IsNullOrEmpty(previousScene.path) &&
                                     previousScene.rootCount == 0 &&
                                     !previousScene.isDirty;
            NewSceneMode mode = replaceEmptyScene ? NewSceneMode.Single : NewSceneMode.Additive;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            SceneManager.SetActiveScene(scene);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.018f, 0.055f, 1f);

            GameObject storyObject = new GameObject("Glass Moon Showcase Player");
            NovelGraphPlayer player = storyObject.AddComponent<NovelGraphPlayer>();
            player.SetGraph(graph);
            player.SetStoryTitle("THE GLASS MOON PROTOCOL");
            player.SetBackdropColor(new Color(0.025f, 0.018f, 0.055f, 1f));
            player.SetAccentColor(new Color(0.16f, 0.78f, 0.78f, 1f));
            NovelGraphSampleEventReceiver receiver =
                storyObject.AddComponent<NovelGraphSampleEventReceiver>();
            BindFunction(player, "activate_station_alarm", receiver.ActivateStationAlarm);
            BindFunction(player, "stabilize_moon_reactor", receiver.StabilizeMoonReactor);
            BindFunction(player, "open_anomaly_gate", receiver.OpenAnomalyGate);
            BindFunction(player, "trigger_station_blackout", receiver.TriggerStationBlackout);
            BindFunction(player, "launch_escape_pod", receiver.LaunchEscapePod);

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!replaceEmptyScene)
            {
                if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(OriginalScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static void BindFunction(
            NovelGraphPlayer player,
            string id,
            UnityAction callback)
        {
            NovelFunctionBinding binding = new NovelFunctionBinding(id);
            UnityEventTools.AddPersistentListener(binding.Callback, callback);
            player.FunctionBindings.Add(binding);
        }

        private static Sprite GeneratePortrait(
            string fileName,
            Color suit,
            Color hair,
            Color skin,
            PortraitEmotion emotion,
            bool drawBody,
            bool drawEyes,
            bool drawMouth)
        {
            const int width = 320;
            const int height = 640;
            Color32[] pixels = new Color32[width * height];
            Color32 suitColor = suit;
            Color32 suitShadow = Color.Lerp(suit, Color.black, 0.35f);
            Color32 hairColor = hair;
            Color32 skinColor = skin;
            Color32 skinShadow = Color.Lerp(skin, Color.black, 0.18f);
            Color32 eyeWhite = new Color(0.94f, 0.98f, 1f, 1f);
            Color32 eyeColor = new Color(0.04f, 0.12f, 0.18f, 1f);
            Color32 mouthColor = new Color(0.24f, 0.04f, 0.1f, 1f);
            Color32 accent = new Color(0.96f, 0.74f, 0.2f, 1f);

            if (drawBody)
            {
                DrawEllipse(pixels, width, height, 160, 122, 145, 205, suitShadow);
                DrawEllipse(pixels, width, height, 160, 150, 128, 190, suitColor);
                DrawRect(pixels, width, height, 132, 292, 188, 363, skinShadow);
                DrawEllipse(pixels, width, height, 160, 456, 118, 154, hairColor);
                DrawEllipse(pixels, width, height, 160, 430, 96, 126, skinColor);
                DrawEllipse(pixels, width, height, 67, 430, 15, 27, skinShadow);
                DrawEllipse(pixels, width, height, 253, 430, 15, 27, skinShadow);
                DrawEllipse(pixels, width, height, 160, 523, 104, 70, hairColor);
                DrawRect(pixels, width, height, 58, 354, 82, 445, hairColor);
                DrawRect(pixels, width, height, 238, 354, 262, 445, hairColor);
                DrawRect(pixels, width, height, 42, 204, 278, 218, accent);
                DrawEllipse(pixels, width, height, 160, 168, 24, 24, accent);
                DrawEllipse(pixels, width, height, 160, 168, 12, 12, suitShadow);
            }

            if (drawEyes)
            {
                bool shocked = emotion == PortraitEmotion.Shocked;
                int eyeHeight = shocked ? 17 : 10;
                DrawEllipse(pixels, width, height, 122, 447, 22, eyeHeight, eyeWhite);
                DrawEllipse(pixels, width, height, 198, 447, 22, eyeHeight, eyeWhite);
                DrawEllipse(pixels, width, height, 122, 447, shocked ? 6 : 7, shocked ? 9 : 7, eyeColor);
                DrawEllipse(pixels, width, height, 198, 447, shocked ? 6 : 7, shocked ? 9 : 7, eyeColor);

                if (emotion == PortraitEmotion.Determined)
                {
                    DrawLine(pixels, width, height, 99, 474, 142, 462, 6, hairColor);
                    DrawLine(pixels, width, height, 178, 462, 221, 474, 6, hairColor);
                }
                else if (emotion == PortraitEmotion.Suspicious)
                {
                    DrawLine(pixels, width, height, 98, 466, 143, 466, 5, hairColor);
                    DrawLine(pixels, width, height, 179, 459, 220, 474, 6, hairColor);
                }
                else
                {
                    DrawLine(pixels, width, height, 100, 471, 143, 471, 5, hairColor);
                    DrawLine(pixels, width, height, 177, 471, 220, 471, 5, hairColor);
                }
            }

            if (drawMouth)
            {
                if (emotion == PortraitEmotion.TalkingOpen)
                {
                    DrawEllipse(pixels, width, height, 160, 384, 25, 18, mouthColor);
                    DrawRect(pixels, width, height, 145, 378, 175, 384, new Color32(238, 134, 148, 255));
                }
                else if (emotion == PortraitEmotion.Shocked)
                {
                    DrawEllipse(pixels, width, height, 160, 382, 23, 29, mouthColor);
                }
                else if (emotion == PortraitEmotion.Determined)
                {
                    DrawLine(pixels, width, height, 138, 382, 181, 389, 5, mouthColor);
                }
                else if (emotion == PortraitEmotion.Suspicious)
                {
                    DrawLine(pixels, width, height, 139, 386, 178, 380, 4, mouthColor);
                }
                else
                {
                    DrawLine(pixels, width, height, 140, 384, 180, 384, 4, mouthColor);
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            string assetPath = $"{PortraitRoot}/{fileName}.png";
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void DrawEllipse(
            Color32[] pixels, int width, int height,
            int centerX, int centerY, int radiusX, int radiusY, Color32 color)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    float normalized = x * x / (float)(radiusX * radiusX) +
                                       y * y / (float)(radiusY * radiusY);
                    if (normalized <= 1f)
                    {
                        SetPixel(pixels, width, height, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void DrawRect(
            Color32[] pixels, int width, int height,
            int minX, int minY, int maxX, int maxY, Color32 color)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    SetPixel(pixels, width, height, x, y, color);
                }
            }
        }

        private static void DrawLine(
            Color32[] pixels, int width, int height,
            int startX, int startY, int endX, int endY, int thickness, Color32 color)
        {
            int steps = Mathf.Max(Mathf.Abs(endX - startX), Mathf.Abs(endY - startY));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(startX, endX, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(startY, endY, t));
                DrawEllipse(pixels, width, height, x, y, thickness, thickness, color);
            }
        }

        private static void SetPixel(
            Color32[] pixels, int width, int height, int x, int y, Color32 color)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                pixels[y * width + x] = color;
            }
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Novelify/Samples/Stories"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Novelify/Samples/Scenes"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Novelify/Samples/Characters"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Novelify/Samples/Portraits"));
        }
    }
}
