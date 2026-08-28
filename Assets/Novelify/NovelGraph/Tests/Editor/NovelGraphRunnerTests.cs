using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NovelGraph.Tests
{
    public class NovelGraphRunnerTests
    {
        private NovelGraphAsset m_graph;

        [SetUp]
        public void SetUp()
        {
            m_graph = ScriptableObject.CreateInstance<NovelGraphAsset>();
            m_graph.name = "Runner Test Graph";
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(m_graph);
        }

        [Test]
        public void RunnerPausesForDialogueAndCompletesAfterAdvance()
        {
            StartNode start = Add(new StartNode());
            DialogueNode dialogue = Add(new DialogueNode { speaker = "Mira", dialogue = "Ready?" });
            EndNode end = Add(new EndNode());
            m_graph.Connect(start, 0, dialogue);
            m_graph.Connect(dialogue, 0, end);

            NovelGraphRunner runner = new NovelGraphRunner();
            runner.Start(m_graph);

            Assert.That(runner.Status, Is.EqualTo(NovelGraphRunnerStatus.WaitingForAdvance));
            Assert.That(runner.CurrentPresentation.Speaker, Is.EqualTo("Mira"));
            Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Ready?"));

            Assert.That(runner.Advance(), Is.True);
            Assert.That(runner.Status, Is.EqualTo(NovelGraphRunnerStatus.Completed));
        }

        [Test]
        public void ChoiceStoresStateRaisesSignalAndUsesSelectedBranch()
        {
            StartNode start = Add(new StartNode());
            TwoChoiceNode choice = Add(new TwoChoiceNode
            {
                prompt = "Choose",
                firstChoice = "Left",
                firstSignal = "went_left",
                firstValue = 10,
                secondChoice = "Right",
                secondSignal = "went_right",
                secondValue = 20,
                stateKey = "direction"
            });
            DialogueNode left = Add(new DialogueNode { dialogue = "Left branch" });
            DialogueNode right = Add(new DialogueNode { dialogue = "Right branch" });
            m_graph.Connect(start, 0, choice);
            m_graph.Connect(choice, 0, left);
            m_graph.Connect(choice, 1, right);

            string raisedSignal = null;
            NovelGraphRunner runner = new NovelGraphRunner();
            runner.SignalRaised += signal => raisedSignal = signal;
            runner.Start(m_graph);

            Assert.That(runner.Status, Is.EqualTo(NovelGraphRunnerStatus.WaitingForChoice));
            Assert.That(runner.Choose(1), Is.True);
            Assert.That(runner.State.GetInt("direction"), Is.EqualTo(20));
            Assert.That(raisedSignal, Is.EqualTo("went_right"));
            Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Right branch"));
        }

        [Test]
        public void IntegerConditionSelectsMatchingOutput()
        {
            StartNode start = Add(new StartNode());
            SetIntNode set = Add(new SetIntNode { key = "trust", value = 1 });
            IntConditionNode condition = Add(new IntConditionNode { key = "trust", expectedValue = 1 });
            DialogueNode equal = Add(new DialogueNode { dialogue = "Equal" });
            DialogueNode notEqual = Add(new DialogueNode { dialogue = "Not equal" });
            m_graph.Connect(start, 0, set);
            m_graph.Connect(set, 0, condition);
            m_graph.Connect(condition, 0, equal);
            m_graph.Connect(condition, 1, notEqual);

            NovelGraphRunner runner = new NovelGraphRunner();
            runner.Start(m_graph);

            Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Equal"));
        }

        [Test]
        public void SaveDataRestoresTheCurrentPresentationAndState()
        {
            StartNode start = Add(new StartNode());
            SetIntNode set = Add(new SetIntNode { key = "chapter", value = 3 });
            DialogueNode dialogue = Add(new DialogueNode { dialogue = "A saved line" });
            m_graph.Connect(start, 0, set);
            m_graph.Connect(set, 0, dialogue);

            NovelGraphRunner original = new NovelGraphRunner();
            original.Start(m_graph);
            NovelGraphSaveData save = original.CaptureSaveData();

            NovelGraphRunner restored = new NovelGraphRunner();
            Assert.That(restored.Restore(m_graph, save), Is.True);
            Assert.That(restored.State.GetInt("chapter"), Is.EqualTo(3));
            Assert.That(restored.CurrentPresentation.Text, Is.EqualTo("A saved line"));
        }

        [Test]
        public void CharacterDialogueCarriesVoicePresentationSettings()
        {
            NovelCharacter character = ScriptableObject.CreateInstance<NovelCharacter>();
            character.Configure("Mira Vale", 520f, Color.cyan);
            try
            {
                StartNode start = Add(new StartNode());
                DialogueNode dialogue = Add(new DialogueNode
                {
                    speakerMode = NovelSpeakerMode.Character,
                    character = character,
                    dialogue = "A voiced line",
                    playLetterSounds = true,
                    charactersPerSecond = 24f
                });
                m_graph.Connect(start, 0, dialogue);

                NovelGraphRunner runner = new NovelGraphRunner();
                runner.Start(m_graph);

                Assert.That(runner.CurrentPresentation.Speaker, Is.EqualTo("Mira Vale"));
                Assert.That(runner.CurrentPresentation.Character, Is.SameAs(character));
                Assert.That(runner.CurrentPresentation.PlayLetterSounds, Is.True);
                Assert.That(runner.CurrentPresentation.CharactersPerSecond, Is.EqualTo(24f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void CallFunctionRequestsBindingAndContinues()
        {
            StartNode start = Add(new StartNode());
            CallFunctionNode call = Add(new CallFunctionNode { functionId = "open_harbor_gates" });
            EndNode end = Add(new EndNode());
            m_graph.Connect(start, 0, call);
            m_graph.Connect(call, 0, end);

            string requestedFunction = null;
            NovelGraphRunner runner = new NovelGraphRunner();
            runner.FunctionRequested += functionId => requestedFunction = functionId;
            runner.Start(m_graph);

            Assert.That(requestedFunction, Is.EqualTo("open_harbor_gates"));
            Assert.That(runner.Status, Is.EqualTo(NovelGraphRunnerStatus.Completed));
        }

        [Test]
        public void ReroutesAndNamedReroutesContinueWithoutLongConnections()
        {
            StartNode start = Add(new StartNode());
            RerouteNode reroute = Add(new RerouteNode());
            NamedRerouteOutNode routeOut = Add(new NamedRerouteOutNode { routeName = "far_side" });
            NamedRerouteInNode routeIn = Add(new NamedRerouteInNode());
            routeIn.SetDeclaration(routeOut);
            routeOut.routeName = "renamed_after_usage_was_created";
            DialogueNode destination = Add(new DialogueNode { dialogue = "Arrived" });
            m_graph.Connect(start, 0, reroute);
            m_graph.Connect(reroute, 0, routeIn);
            m_graph.Connect(routeOut, 0, destination);

            NovelGraphRunner runner = new NovelGraphRunner();
            runner.Start(m_graph);

            Assert.That(runner.Status, Is.EqualTo(NovelGraphRunnerStatus.WaitingForAdvance));
            Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Arrived"));
        }

        [Test]
        public void LegacyNamedRerouteNamesStillResolve()
        {
            StartNode start = Add(new StartNode());
            NamedRerouteInNode legacyUsage = Add(new NamedRerouteInNode { routeName = "legacy_route" });
            NamedRerouteOutNode declaration = Add(new NamedRerouteOutNode { routeName = "legacy_route" });
            DialogueNode destination = Add(new DialogueNode { dialogue = "Legacy arrived" });
            m_graph.Connect(start, 0, legacyUsage);
            m_graph.Connect(declaration, 0, destination);

            NovelGraphRunner runner = new NovelGraphRunner();
            runner.Start(m_graph);

            Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Legacy arrived"));
        }

        [Test]
        public void NovelPageCanBeCalledTwiceAndReturnsToEachCaller()
        {
            NovelPageAsset page = ScriptableObject.CreateInstance<NovelPageAsset>();
            page.name = "Reusable Page";
            try
            {
                StartNode pageStart = AddTo(page, new StartNode());
                DialogueNode pageLine = AddTo(page, new DialogueNode { dialogue = "Reusable line" });
                EndNode pageEnd = AddTo(page, new EndNode());
                page.Connect(pageStart, 0, pageLine);
                page.Connect(pageLine, 0, pageEnd);

                StartNode start = Add(new StartNode());
                NovelPageNode firstCall = Add(new NovelPageNode { page = page });
                DialogueNode between = Add(new DialogueNode { dialogue = "Between calls" });
                NovelPageNode secondCall = Add(new NovelPageNode { page = page });
                DialogueNode after = Add(new DialogueNode { dialogue = "After calls" });
                m_graph.Connect(start, 0, firstCall);
                m_graph.Connect(firstCall, 0, between);
                m_graph.Connect(between, 0, secondCall);
                m_graph.Connect(secondCall, 0, after);

                NovelGraphRunner runner = new NovelGraphRunner();
                runner.Start(m_graph);
                Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Reusable line"));

                NovelGraphSaveData save = runner.CaptureSaveData();
                Assert.That(save.callStack, Has.Count.EqualTo(1));
                NovelGraphRunner restored = new NovelGraphRunner();
                Assert.That(restored.Restore(m_graph, save), Is.True);
                Assert.That(restored.CurrentPresentation.Text, Is.EqualTo("Reusable line"));

                Assert.That(runner.Advance(), Is.True);
                Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Between calls"));
                Assert.That(runner.Advance(), Is.True);
                Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("Reusable line"));
                Assert.That(runner.Advance(), Is.True);
                Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("After calls"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(page);
            }
        }

        [Test]
        public void ComponentFunctionUsesNamedTypedAndStoryStateArguments()
        {
            GameObject owner = new GameObject("Story Owner");
            GameObject rockObject = new GameObject("Test Rock", typeof(AudioSource));
            NovelGraphAdvancedSampleRock rock = rockObject.AddComponent<NovelGraphAdvancedSampleRock>();
            NovelFunctionTarget target = rockObject.AddComponent<NovelFunctionTarget>();
            target.SetTargetId("test_rock");
            try
            {
                StartNode start = Add(new StartNode());
                SetIntNode setDamage = Add(new SetIntNode { key = "damage", value = 1 });
                CallFunctionNode call = Add(new CallFunctionNode
                {
                    callMode = NovelFunctionCallMode.ComponentMethod,
                    targetMode = NovelFunctionTargetMode.TargetId,
                    target = "test_rock",
                    componentType = nameof(NovelGraphAdvancedSampleRock),
                    methodName = nameof(NovelGraphAdvancedSampleRock.Strike),
                    arguments = new System.Collections.Generic.List<NovelFunctionArgument>
                    {
                        new NovelFunctionArgument
                        {
                            name = "playSound",
                            type = NovelFunctionArgumentType.Boolean,
                            boolValue = false
                        },
                        new NovelFunctionArgument
                        {
                            name = "impactLabel",
                            type = NovelFunctionArgumentType.String,
                            stringValue = "test hammer"
                        },
                        new NovelFunctionArgument
                        {
                            name = "damage",
                            source = NovelFunctionArgumentSource.StoryState,
                            type = NovelFunctionArgumentType.Integer,
                            stateKey = "damage"
                        }
                    }
                });
                EndNode end = Add(new EndNode());
                m_graph.Connect(start, 0, setDamage);
                m_graph.Connect(setDamage, 0, call);
                m_graph.Connect(call, 0, end);

                NovelGraphRunner runner = new NovelGraphRunner();
                runner.ComponentFunctionRequested += request => NovelFunctionInvoker.Invoke(request, owner);
                runner.Start(m_graph, owner);

                Assert.That(runner.Status, Is.EqualTo(NovelGraphRunnerStatus.Completed));
                Assert.That(rock.RemainingHealth, Is.EqualTo(3));
                Assert.That(rock.LastImpactLabel, Is.EqualTo("test hammer"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rockObject);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void CharacterUtilityNodesRaiseStageCommandsAndContinue()
        {
            NovelCharacter character = ScriptableObject.CreateInstance<NovelCharacter>();
            try
            {
                StartNode start = Add(new StartNode());
                ShowCharacterNode show = Add(new ShowCharacterNode
                {
                    character = character,
                    expression = "shocked",
                    stagePosition = NovelCharacterPosition.Left,
                    transitionDuration = 0.4f
                });
                DialogueNode dialogue = Add(new DialogueNode { dialogue = "After staging" });
                m_graph.Connect(start, 0, show);
                m_graph.Connect(show, 0, dialogue);

                NovelCharacterStageCommand received = default;
                bool commandRaised = false;
                NovelGraphRunner runner = new NovelGraphRunner();
                runner.CharacterStageRequested += command =>
                {
                    received = command;
                    commandRaised = true;
                };
                runner.Start(m_graph);

                Assert.That(commandRaised, Is.True);
                Assert.That(received.Type, Is.EqualTo(NovelCharacterStageCommandType.Show));
                Assert.That(received.Character, Is.SameAs(character));
                Assert.That(received.Expression, Is.EqualTo("shocked"));
                Assert.That(received.Position, Is.EqualTo(NovelCharacterPosition.Left));
                Assert.That(runner.CurrentPresentation.Text, Is.EqualTo("After staging"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void CharacterDialogueRequestsExpressionFocusAndSpeakingMotion()
        {
            NovelCharacter character = ScriptableObject.CreateInstance<NovelCharacter>();
            try
            {
                StartNode start = Add(new StartNode());
                DialogueNode dialogue = Add(new DialogueNode
                {
                    speakerMode = NovelSpeakerMode.Character,
                    character = character,
                    dialogue = "Watch this",
                    expression = "neutral",
                    characterPosition = NovelCharacterPosition.Right,
                    speakingMotion = NovelCharacterMotion.Bounce
                });
                m_graph.Connect(start, 0, dialogue);

                var commands = new System.Collections.Generic.List<NovelCharacterStageCommand>();
                NovelGraphRunner runner = new NovelGraphRunner();
                runner.CharacterStageRequested += commands.Add;
                runner.Start(m_graph);

                Assert.That(commands.Any(command =>
                    command.Type == NovelCharacterStageCommandType.Show &&
                    command.Expression == "neutral" &&
                    command.Position == NovelCharacterPosition.Right), Is.True);
                Assert.That(commands.Any(command =>
                    command.Type == NovelCharacterStageCommandType.Focus), Is.True);
                Assert.That(commands.Any(command =>
                    command.Type == NovelCharacterStageCommandType.Animate &&
                    command.Motion == NovelCharacterMotion.Bounce), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void EverythingShowcaseCoversEveryNodeType()
        {
            NovelGraphAsset showcase = AssetDatabase.LoadAssetAtPath<NovelGraphAsset>(
                "Assets/Novelify/Samples/Stories/TheGlassMoonProtocol.asset");
            Assert.That(showcase, Is.Not.Null);
            Assert.That(showcase.Nodes.Count, Is.EqualTo(69));
            Assert.That(showcase.Connections.Count, Is.EqualTo(68));

            Type[] expectedTypes =
            {
                typeof(StartNode),
                typeof(EndNode),
                typeof(DialogueNode),
                typeof(TwoChoiceNode),
                typeof(MultiChoiceNode),
                typeof(SetIntNode),
                typeof(IntConditionNode),
                typeof(SignalNode),
                typeof(CallFunctionNode),
                typeof(DebugLogNode),
                typeof(DebugValue),
                typeof(ShowCharacterNode),
                typeof(MoveCharacterNode),
                typeof(SetCharacterExpressionNode),
                typeof(AnimateCharacterNode),
                typeof(FocusCharacterNode),
                typeof(HideCharacterNode),
                typeof(ClearCharactersNode)
            };
            foreach (Type expectedType in expectedTypes)
            {
                Assert.That(showcase.Nodes.Any(expectedType.IsInstanceOfType), Is.True,
                    $"Showcase is missing {expectedType.Name}.");
            }
        }

        [Test]
        public void EverythingShowcaseCharactersDemonstrateLayeredPortraits()
        {
            NovelCharacter lyra = AssetDatabase.LoadAssetAtPath<NovelCharacter>(
                "Assets/Novelify/Samples/Characters/Lyra.asset");
            NovelCharacter orin = AssetDatabase.LoadAssetAtPath<NovelCharacter>(
                "Assets/Novelify/Samples/Characters/Orin.asset");

            Assert.That(lyra, Is.Not.Null);
            Assert.That(orin, Is.Not.Null);
            Assert.That(lyra.Body.HasVisual, Is.True);
            Assert.That(orin.Body.HasVisual, Is.True);
            Assert.That(lyra.Expressions.Any(item =>
                item != null && string.Equals(item.Id, "talking", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(orin.Expressions.Any(item =>
                item != null && string.Equals(item.Id, "talking", StringComparison.OrdinalIgnoreCase)), Is.False);

            foreach (NovelCharacter character in new[] { lyra, orin })
            {
                foreach (NovelCharacterExpression emotion in character.Expressions)
                {
                    Assert.That(emotion, Is.Not.Null);
                    Assert.That(emotion.Eyes.HasVisual, Is.True, character.name + " " + emotion.Id + " has no eyes.");
                    Assert.That(emotion.Mouth.HasVisual, Is.True, character.name + " " + emotion.Id + " has no mouth.");
                    Assert.That(emotion.Mouth.GetFrame(0f, false), Is.Not.Null);
                    Assert.That(emotion.Mouth.GetFrame(0.2f, true), Is.Not.Null);
                }
            }
        }

        [Test]
        public void FinishingDialogueStopsMouthAndMotionButKeepsEmotion()
        {
            NovelCharacter character = ScriptableObject.CreateInstance<NovelCharacter>();
            GameObject owner = new GameObject("Dialogue Presentation Test");
            try
            {
                NovelCharacterExpression shocked = new NovelCharacterExpression();
                shocked.Configure("shocked", null, Array.Empty<Sprite>(), 1f, true,
                    new Vector2(0.5f, 0.5f), 0.5f);
                character.ConfigureExpressions("shocked", shocked);

                NovelGraphPlayer player = owner.AddComponent<NovelGraphPlayer>();
                MethodInfo handleCommand = typeof(NovelGraphPlayer).GetMethod(
                    "HandleCharacterStageRequested", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo stopDialogue = typeof(NovelGraphPlayer).GetMethod(
                    "StopDialoguePresentation", BindingFlags.Instance | BindingFlags.NonPublic);
                handleCommand.Invoke(player, new object[]
                {
                    NovelCharacterStageCommand.Show(character, "shocked",
                        NovelCharacterPosition.Left, 0f, 1f, false)
                });
                handleCommand.Invoke(player, new object[]
                {
                    NovelCharacterStageCommand.Animate(character,
                        NovelCharacterMotion.Talking, 0f, 1f, true)
                });

                FieldInfo stagedCharactersField = typeof(NovelGraphPlayer).GetField(
                    "m_stagedCharacters", BindingFlags.Instance | BindingFlags.NonPublic);
                var stagedCharacters = (System.Collections.IEnumerable)stagedCharactersField.GetValue(player);
                object staged = stagedCharacters.Cast<object>().Single();
                staged.GetType().GetField("IsTalking").SetValue(staged, true);

                stopDialogue.Invoke(player, new object[] { character });

                Assert.That(staged.GetType().GetField("Expression").GetValue(staged), Is.EqualTo("shocked"));
                Assert.That(staged.GetType().GetField("Motion").GetValue(staged),
                    Is.EqualTo(NovelCharacterMotion.None));
                Assert.That(staged.GetType().GetField("MotionLoops").GetValue(staged), Is.False);
                Assert.That(staged.GetType().GetField("IsTalking").GetValue(staged), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void EmotionSelectsItsOwnMouthAndTalkingOnlyChangesMouthFrames()
        {
            Texture2D texture = new Texture2D(8, 8);
            Sprite idleMouth = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f);
            Sprite openMouth = Sprite.Create(texture, new Rect(4f, 0f, 4f, 4f), Vector2.one * 0.5f);
            try
            {
                NovelCharacterMouthLayer mouth = new NovelCharacterMouthLayer();
                mouth.Configure(
                    idleMouth,
                    Array.Empty<Sprite>(),
                    new[] { openMouth },
                    8f,
                    new Vector2(0.5f, 0.5f),
                    6f,
                    new Vector2(0.02f, -0.18f));
                NovelCharacterExpression shocked = new NovelCharacterExpression();
                shocked.ConfigureLayers("shocked", new NovelCharacterSpriteLayer(), mouth);

                Assert.That(shocked.Id, Is.EqualTo("shocked"));
                Assert.That(shocked.Mouth.GetFrame(0f, false), Is.SameAs(idleMouth));
                Assert.That(shocked.Mouth.GetFrame(0f, true), Is.SameAs(openMouth));
                Assert.That(shocked.Mouth.Framing.Offset, Is.EqualTo(new Vector2(0.02f, -0.18f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(idleMouth);
                UnityEngine.Object.DestroyImmediate(openMouth);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void FramingWindowSavePersistsNestedLayerSettings()
        {
            const string path = "Assets/Novelify/NovelGraph/Tests/Editor/TemporaryFramingCharacter.asset";
            NovelCharacter character = ScriptableObject.CreateInstance<NovelCharacter>();
            NovelGraph.Editor.NovelCharacterFramingWindow window = null;
            try
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(character, path);
                window = ScriptableObject.CreateInstance<NovelGraph.Editor.NovelCharacterFramingWindow>();
                typeof(NovelGraph.Editor.NovelCharacterFramingWindow)
                    .GetField("m_character", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(window, character);
                MethodInfo save = typeof(NovelGraph.Editor.NovelCharacterFramingWindow).GetMethod(
                    "SaveFraming", BindingFlags.Instance | BindingFlags.NonPublic);
                save.Invoke(window, new object[]
                {
                    character.Body.Framing,
                    new Vector2(0.4f, 0.65f),
                    3.25f,
                    new Vector2(-0.12f, 0.28f),
                    true
                });

                Assert.That(EditorUtility.IsDirty(character), Is.False);
                Assert.That(character.Body.Framing.Point, Is.EqualTo(new Vector2(0.4f, 0.65f)));
                Assert.That(character.Body.Framing.Radius, Is.EqualTo(3.25f));
                Assert.That(character.Body.Framing.Offset, Is.EqualTo(new Vector2(-0.12f, 0.28f)));
                Assert.That(character.Body.Framing.FlipX, Is.True);
            }
            finally
            {
                if (window != null) UnityEngine.Object.DestroyImmediate(window);
                AssetDatabase.DeleteAsset(path);
                if (character != null) UnityEngine.Object.DestroyImmediate(character, true);
            }
        }

        [Test]
        public void RuntimeLabelStatesCannotHighlightText()
        {
            GUIStyle style = new GUIStyle();
            Texture2D highlight = new Texture2D(1, 1);
            try
            {
                style.hover.background = highlight;
                style.active.background = highlight;
                style.focused.background = highlight;
                style.hover.textColor = Color.yellow;
                Color expected = new Color(0.11f, 0.12f, 0.13f, 1f);

                MethodInfo setLabelColor = typeof(NovelGraphPlayer).GetMethod(
                    "SetLabelColor", BindingFlags.Static | BindingFlags.NonPublic);
                setLabelColor.Invoke(null, new object[] { style, expected });

                GUIStyleState[] states =
                {
                    style.normal, style.hover, style.active, style.focused,
                    style.onNormal, style.onHover, style.onActive, style.onFocused
                };
                foreach (GUIStyleState state in states)
                {
                    Assert.That(state.textColor, Is.EqualTo(expected));
                    Assert.That(state.background, Is.Null);
                }
                Assert.That(style.richText, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(highlight);
            }
        }

        [Test]
        public void EveryNodeAndExposedFieldHasTooltipText()
        {
            Type[] nodeTypes = typeof(NovelGraphNode).Assembly.GetTypes()
                .Where(type => type != typeof(NovelGraphNode) &&
                               !type.IsAbstract &&
                               typeof(NovelGraphNode).IsAssignableFrom(type))
                .ToArray();

            foreach (Type nodeType in nodeTypes)
            {
                NodeInfoAttribute nodeInfo = nodeType.GetCustomAttribute<NodeInfoAttribute>();
                Assert.That(nodeInfo, Is.Not.Null, $"{nodeType.Name} is missing NodeInfo.");
                Assert.That(nodeInfo.description, Is.Not.Empty, $"{nodeType.Name} is missing a node tooltip.");

                FieldInfo[] exposedFields = nodeType.GetFields()
                    .Where(field => field.GetCustomAttribute<ExposedPropertyAttribute>() != null)
                    .ToArray();
                foreach (FieldInfo field in exposedFields)
                {
                    TooltipAttribute tooltip = field.GetCustomAttribute<TooltipAttribute>();
                    Assert.That(tooltip, Is.Not.Null, $"{nodeType.Name}.{field.Name} is missing Tooltip.");
                    Assert.That(tooltip.tooltip, Is.Not.Empty, $"{nodeType.Name}.{field.Name} has an empty Tooltip.");
                }
            }
        }

        private T Add<T>(T node) where T : NovelGraphNode
        {
            m_graph.AddNode(node);
            return node;
        }

        private static T AddTo<T>(NovelGraphAsset graph, T node) where T : NovelGraphNode
        {
            graph.AddNode(node);
            return node;
        }
    }
}
