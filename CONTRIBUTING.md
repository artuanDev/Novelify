# Contributing

Use Unity `6000.3.10f1` to avoid unnecessary project serialization changes.

Before opening a pull request:

1. Run the Edit Mode test suite `NovelGraph.Tests.Editor`.
2. Open and play `DecisionEventsSample.unity` through every branch.
3. Confirm new Unity assets include their `.meta` files.
4. Keep runtime code inside `NovelGraph.Runtime`; editor-only APIs belong in `Scripts/Editor`.

Commit focused changes and describe any graph or save compatibility impact in the pull request.
