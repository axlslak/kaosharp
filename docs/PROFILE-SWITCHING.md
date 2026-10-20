# Switching activities

AOSharp 2.7.24 adds a queued handover between activity profiles. Use a matching launcher and AOSharp.Bootstrap.dll, starting with a fresh injection. A character injected with an older bootstrap cannot use the new handover protocol. Initial plugin loading and its automatic retry must finish first.

Select an activity tab and a character, then choose **Reinject**. The loader tears down the old managed plugin environment, confirms cleanup, and loads that character's selected plugins for the destination activity. **Reinject All** queues currently injected characters connected to this launcher. Characters without an injection still use **Inject** or **Inject All**. Reinject also applies staged changes to the activity a character already runs.

The target and plugin paths are captured when queued. Browsing other tabs and editing future plugin choices remain available. Another injection, ejection or handover for that character is blocked while its request is pending. Activities involved in a pending handover cannot be deleted. Empty selections and missing DLLs are skipped before unloading anything.

## In game

| Command | Action |
| --- | --- |
| `/ao#` | Show the current activity and clickable activity choices |
| `/ao# default` | Move this character to default |
| `/ao# db1` | Move this character to db1 |
| `/ao# db2 all` | Queue connected, injected characters for db2 |

Commands belong to the bootstrap, so no control plugin or Manager Sync is required. The launcher must remain open. Profile names match without case sensitivity. Existing names are preserved, including names containing spaces; an exact name match takes precedence over the trailing `all` option. Clickable choices use stable activity IDs and survive renames. A macro can send the same command, for example `/macro DB1 /ao# db1`.

## Results and recovery

Pending and uncertain results use the existing orange activity status. Successful initialization uses green; failed and mixed plugin results keep their existing red/orange reporting and error details. Switching tabs alone does not switch a running character.

The queue waits for loader acknowledgments rather than simulating rapid Eject/Inject clicks. It retains the native bootstrap, hooks and launcher connection while replacing the managed plugin domain. Failed cleanup prevents loading the new selection. An unconfirmed handover times out after 30 seconds and blocks further operations for that character until a conclusive late reply arrives or the game client is restarted. Other queued characters can then proceed. A zoning character is rejected before unloading.

Chat replies need a functioning managed Core and may be unavailable after cleanup or Core failure; the launcher retains the result. The ordinary manual Eject/Inject path still uses its existing native teardown behavior.

This is a source-only delivery. Windows/game execution, literal `#` handling by the client, clickable links and macros have not been runtime verified.


Since 2.7.25, [short plugin commands](PLUGIN-COMMANDS.md) can change one plugin inside the running activity, including unloading the last plugin while retaining Core/bootstrap for future commands. These use protocol2; use the matching current launcher and bootstrap with a fresh injection. Ordinary empty-selection Reinject still reports no plugins selected.
