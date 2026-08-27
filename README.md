# Novelify

Novelify is a node-based narrative runtime and editor for Unity 6. It supports typed character dialogue with per-letter voices, narration, player decisions, persistent state, conditional branches, signals, bound scene functions, endings, and local save/load.

## Requirements

- Unity `6000.3.10f1`
- A desktop Game view; the included player scales to common desktop and mobile aspect ratios

## Try the sample

1. Let Unity finish importing the project.
2. Select **Tools > Novelify > Create or Refresh Sample Story** to generate the latest sample, characters, graph, and bindings.
3. Open `Assets/Novelify/Samples/Scenes/DecisionEventsSample.unity`.
4. Enter Play Mode.
5. Use **Continue**, click a decision, or press `Space`, `Enter`, or `1` through `4`.

The expanded **The Last Beacon** sample has narrator and character lines, two distinct synthesized voices, optional silent dialogue, two- and four-way choices, state and conditions, signals, three scene-function bindings, and multiple endings. `NovelGraphSampleEventReceiver` changes the interface accent and logs each gameplay reaction.

## Everything showcase

Open `Assets/Novelify/Samples/Scenes/EverythingShowcase.unity` for the deliberately complicated **The Glass Moon Protocol** sample. If it is missing or you want to reset it, choose **Tools > Novelify > Create or Refresh Everything Showcase**.

The showcase contains 69 nodes and 68 connections, two generated layered characters with faceless bodies, emotion-specific eyes and mouths, automatic talking-mouth frames, four major routes, eight endings, and every currently available node class.

The shared opening demonstrates two-character staging, left/right/centre movement, focus dimming, automatic mouth animation with neutral and shocked emotions, synthesized letter voices, debug nodes, an alarm function binding, and a four-way choice. Save at that choice and replay its routes:

- **Stabilize the lunar reactor:** Pulse animation, state assignment, integer condition, signal, function binding, hide character, and a success/failure split.
- **Decode the signal:** Suspicious emotion, bounce/talking animation, two-way choice, choice signals, both condition outputs, a custom non-character speaker, blackout, and shake.
- **Evacuate:** Far-left/far-right movement, jump and bounce, silent character dialogue, hide, signal, escape-pod function, and clear stage.
- **Touch the anomaly:** Two shocked characters, looping pulse, shocked motion, focus reset, custom speaker, signal, blackout, and slow clear-stage transition.

Save, load, restart, keyboard choices, multiple endings, narrator/custom/character dialogue, and typewriter reveal can be tested from this one scene.

## Author a story

1. In the Project window, choose **Create > Novel Graph > New Novel Graph**.
2. Double-click the new asset to open the graph editor.
3. Right-click empty graph space and add one **Process > Start Node**.
4. Add nodes from **Story**, **State**, **Events**, and **Process**.
5. Connect output ports to input ports. A graph should finish at an **End** node.
6. Add `NovelGraphPlayer` to a GameObject and assign the graph asset.

Node behavior:

| Node | Purpose |
| --- | --- |
| Dialogue | Shows narration, a Character asset, or a custom speaker with optional typed letter audio. |
| Show Character | Places a character at Far Left, Left, Center, Right, or Far Right with an emotion and fade. |
| Move Character | Slides a visible character between stage positions. |
| Set Emotion | Changes a visible character to a named expression. |
| Animate Character | Plays Talking, Shocked, Shake, Bounce, Jump, or Pulse motion. |
| Focus Character | Highlights one character and dims the other staged characters. |
| Hide Character | Fades one character off screen. |
| Clear Characters | Fades the entire cast off screen between scenes. |
| Two Choices | Shows two decisions, stores the selected integer, raises its signal, and follows that output. |
| Four Choices | Shows four decisions and stores the selected zero-based index. |
| Set Integer | Stores a named integer immediately. |
| Integer Condition | Uses **Equal** or **Not Equal** according to current state. |
| Raise Signal | Sends a named event to gameplay code. |
| Call Function | Invokes a matching `UnityEvent` function binding on the active player. |
| End | Completes the story and invokes the completion event. |

State keys, signal names, and function IDs are case-sensitive. Use stable, code-friendly names such as `trusted_mira`, `beacon_lit`, and `open_harbor_gates` because saves and gameplay listeners depend on them.

## Characters and voices

1. Choose **Create > Novelify > Character**.
2. Set the displayed name, optional letter sound clip, fallback tone frequency, volume, pitch variation, and name color.
3. On a Dialogue node, select **Character** as the speaker mode and assign the asset.
4. Enable **Play Letter Sounds** for voiced typing or disable it for a silent line.
5. Adjust **Characters Per Second** per line.

Narrator lines never play a character voice. When a Character has no audio clip, Novelify generates a short tone using that character's configured frequency. Pressing Continue while a line is typing reveals the whole line; pressing it again advances.

## Character sprites, emotions, and framing

Character portraits are composed in this order:

1. **Body:** One persistent body sprite with an empty face. It can also have optional animation frames.
2. **Emotion Eyes:** Each emotion entry owns its eye/eyebrow sprite and optional blink or eye-movement frames.
3. **Emotion Mouth:** The same emotion owns an idle mouth plus its own talking frames. Novelify switches between them automatically while that character's dialogue types.

Add emotion IDs such as **neutral**, **happy**, **shocked**, or **suspicious**, then set **Default Expression** to the fallback emotion. Do not create a **talking** emotion: talking is an independent mouth state. Older characters containing complete per-expression portraits remain supported through the hidden legacy fallback.

Open **Tools > Novelify > Character Framing Tool**, assign the Character, choose **Body**, **Emotion Eyes**, or **Emotion Mouth**, and select an emotion when needed. Click or drag the composite preview to place the selected layer. The cyan marker shows its layer offset; the mouse wheel or **Visible Radius** controls its scale, and **Sprite Anchor** chooses the attachment point inside the source sprite. Tightly cropped eyes and mouths usually need a larger radius plus a vertical offset. Press **Save Settings** to write the nested framing data immediately to the Character asset; edits are also auto-saved.

For a simple speaking line, set a Dialogue node to **Character** and use its presentation fields:

1. Enable **Show Character**.
2. Enter an emotion ID and choose a left/centre/right position.
3. Optionally pick a whole-character **Speaking Motion** such as Talking, Shocked, Bounce, or Shake. This is separate from the automatic mouth animation.
4. Enable **Focus Speaker** to dim other visible characters.

For scenes with two non-player characters, use **Show Character** once for each participant, placing one at Left and one at Right. Dialogue nodes then change each speaker's emotion, mouth state, motion, and focus automatically. When the speaker changes, the previous mouth returns to idle while its current emotion remains. Use the standalone nodes when an emotion or movement should happen between lines, and **Clear Characters** when changing scenes.

## Call scene functions

Graph assets cannot safely reference objects from a Unity scene. Novelify therefore uses stable string IDs:

1. Add an entry under **Novel Graph Player > Function Bindings**.
2. Give it an ID such as `open_harbor_gates`.
3. Add a callback, drag in a scene object, and select its public function.
4. Add **Events > Call Function** to the graph and enter the exact same ID.

When that node executes, the player's serialized `UnityEvent` invokes the selected object function and graph execution continues. A missing ID produces a visible notice and Console warning instead of stopping the story.

## React to decisions

For Inspector wiring, expand **Events > On Signal** on `NovelGraphPlayer` and connect a method that accepts one `string`. For code wiring:

```csharp
using NovelGraph;
using UnityEngine;

public class DoorEvents : MonoBehaviour
{
    [SerializeField] private NovelGraphPlayer player;

    private void OnEnable() => player.OnSignal.AddListener(HandleSignal);
    private void OnDisable() => player.OnSignal.RemoveListener(HandleSignal);

    private void HandleSignal(string signal)
    {
        if (signal == "opened_archive")
        {
            // Unlock a door, start a Timeline, grant an item, etc.
        }
    }
}
```

The runner is also available through `player.Runner`. Read decision state with `player.Runner.State.GetInt("trusted_mira")` or `GetBool(...)`.

## Saves

The included UI exposes **Save**, **Load**, and **Restart**. Saves use `PlayerPrefs` and include the current node ID plus all integer state. Changing node IDs by recreating nodes can invalidate an old save, so treat published graph assets as content with migration requirements.

For production, replace the player UI or persistence without replacing the graph runtime: `NovelGraphRunner` is a plain C# class and emits `PresentationChanged`, `SignalRaised`, `FunctionRequested`, `Completed`, and `Faulted` events.

## Tests

Open **Window > General > Test Runner**, select **EditMode**, and run `NovelGraph.Tests.Editor`. The tests cover dialogue pauses, character voice metadata, choice branches and signals, function requests, integer conditions, save restoration, and mandatory tooltip coverage.

## GitHub Actions

The Unity test workflow runs on pushes and pull requests targeting `main`. Because Unity requires an activated editor even for automated tests, configure either `UNITY_LICENSE`, or all of `UNITY_SERIAL`, `UNITY_EMAIL`, and `UNITY_PASSWORD`, under the repository's **Settings > Secrets and variables > Actions** page. Until valid credentials are present, the workflow finishes successfully with a notice and skips only the licensed Unity test step; local EditMode tests remain available through Unity's Test Runner.

## Repository

The repository ignores Unity-generated folders and tracks all source assets with their `.meta` files. It is published from `main` at [artuanDev/Novelify](https://github.com/artuanDev/Novelify). Local commits use the `artuangp <artuangp@gmail.com>` author identity, while `artuanDev` is the authenticated GitHub username and repository owner.
