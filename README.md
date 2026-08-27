# Novelify

Novelify is a node-based narrative runtime and editor for Unity 6. It supports pausable dialogue, player decisions, persistent integer state, conditional branches, named gameplay events, endings, and local save/load.

## Requirements

- Unity `6000.3.10f1`
- A desktop Game view; the included player scales to common desktop and mobile aspect ratios

## Try the sample

1. Let Unity finish importing the project.
2. Select **Tools > Novelify > Create or Refresh Sample Story** if the sample scene is not present.
3. Open `Assets/Novelify/Samples/Scenes/DecisionEventsSample.unity`.
4. Enter Play Mode.
5. Use **Continue**, click a decision, or press `Space`, `Enter`, `1`, or `2`.

The sample, **The Last Beacon**, raises events after each decision. `NovelGraphSampleEventReceiver` reacts by changing the interface accent and writing the event to the Console. The player also shows a short `Event: event_name` notice.

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
| Dialogue | Shows a speaker and line, then waits for Continue. |
| Two Choices | Shows two decisions, stores the selected integer, raises its signal, and follows that output. |
| Four Choices | Shows four decisions and stores the selected zero-based index. |
| Set Integer | Stores a named integer immediately. |
| Integer Condition | Uses **Equal** or **Not Equal** according to current state. |
| Raise Signal | Sends a named event to gameplay code. |
| End | Completes the story and invokes the completion event. |

State keys and signal names are case-sensitive. Use stable, code-friendly names such as `trusted_mira` and `beacon_lit` because saves and gameplay listeners depend on them.

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

For production, replace the player UI or persistence without replacing the graph runtime: `NovelGraphRunner` is a plain C# class and emits `PresentationChanged`, `SignalRaised`, `Completed`, and `Faulted` events.

## Tests

Open **Window > General > Test Runner**, select **EditMode**, and run `NovelGraph.Tests.Editor`. The tests cover dialogue pauses, choice branches and signals, integer conditions, and save restoration.

## Repository

The repository ignores Unity-generated folders and tracks all source assets with their `.meta` files. Git is configured locally for the `artuangp` identity; authenticate GitHub CLI or GitHub Desktop before publishing, then create a repository and push the `main` branch.
