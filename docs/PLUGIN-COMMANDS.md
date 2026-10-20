# Loading a plugin from the game

In **Plugin list**, right-click the plugin row and choose **Edit**. Give it a **Short name**, shown in the column immediately after Name. Examples: `sync`, `handler`, `map`, `dd`, `help`.

Names are unique across the library without regard to case. Use one word up to 32 characters, without spaces or control characters. Leave the Edit field blank to clear the short name. Changes save automatically in launcher.json; older entries start blank.

| Command | Result |
| --- | --- |
| `/load map` | Load map for this character in its running activity |
| `/unload map` | Unload map from this character |
| `/load dd` | Load the damage plugin assigned the short name dd |
| `/unload dd` | Unload that plugin |
| `/unload help all` | Unload help for connected, injected characters owned by this launcher |

`all` works with either command. Each character keeps its own running activity, even when characters are running different activities or the launcher is showing another tab. Case does not matter. A busy, disconnected or incompatible character is skipped with an explanation. Unknown or ambiguous names do not change any selection.

The requested choice saves like that character's checkbox. If a handover is rejected or fails, the saved choice remains staged for a later injection. Loading a registered library plugin which is outside its activity's pool also adds it to that pool, enabling it for this character only. Unloading keeps the pool and library entry. Other live DLL choices are carried forward from the live snapshot; unrelated staged checkbox changes remain staged. A later explicit Reinject uses those staged choices as usual.

These commands reuse the acknowledged managed handover queue. The whole managed plugin environment is unloaded and reloaded, so remaining plugins also restart and may reset transient state. Failed cleanup prevents loading the replacement; timeout/disconnect guards and restart requirements remain. If a requested plugin is already initialized, or already absent for unload, no reload is needed. A failed plugin can be retried with `/load`.

Unloading the final plugin leaves the native bootstrap and managed Core connected, with no plugins loaded. The activity stays connected and can accept a later `/load`. Initial injection still needs a plugin selection, and ordinary empty-selection Reinject retains its existing behavior.

The launcher must stay open. Use matching **2.7.25** launcher and bootstrap files and a fresh injection: protocol2 supports these commands and empty plugin handovers. While injected, AOSharp owns the `/load` and `/unload` command names. No extra control plugin or Manager Sync dependency is required.

Chat reports queue/skip/completion results. If saving fails, the command can still run in memory, but chat and the launcher report that the choice was not saved; use the existing Retry control. Chat output may be unavailable when managed Core fails, in which case the launcher retains the result.

Source review only: compilation, native command behavior, dialogs, empty handover and in-game operations have not been runtime verified.
