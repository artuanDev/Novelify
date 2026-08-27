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

## Repository

The repository ignores Unity-generated folders and tracks all source assets with their `.meta` files. Git is configured locally for the `artuangp` identity; authenticate GitHub CLI or GitHub Desktop before publishing, then create a repository and push the `main` branch.
