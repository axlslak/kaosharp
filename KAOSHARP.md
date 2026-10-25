# KAOSharp 2.7.27

## 2.7.27 — weapon-holder lookup on the new client

Character weapon queries now read the correct weapon-holder pointer for each client: classic offset `0x1D4`, new-client offset `0x1D8`. The shared accessor also supplies weapon enumeration, attacking state and weapon-holder state. `GetWeapon` returns null when no holder is available and uses the same holder for its native lookup and returned weapon wrapper.

This corrects a native access violation caused by passing a different character component to the weapon lookup on the new client. `AOSharp.Core.dll` advances to file version 1.0.1.0 / informational version 2.7.27; binding identities, native function signatures and weapon-item casting stay unchanged. Deploy the matching rebuilt AOSharp output. Static source, captured-memory, native-call and RTTI review only; no builds, tests, releases, CI or in-game verification of the repair. Broader new-client compatibility remains unconfirmed.

## 2.7.26 — Variant integer reads on both clients

Native settings integer reads now resolve the compatible `Variant.AsInt32` export from the loaded `Utils.dll` once per managed environment. The classic client exports the MSVC `long` return-type name; the new client exports the `int` name. Both use the same 32-bit calling contract. The resolver accepts these two known names and reports a missing module or unsupported export clearly.

The managed method signature and assembly binding identities stay compatible. The change is in `AOSharp.Common.dll` (file version 1.0.1.0, informational version 2.7.26), so deploy the matching rebuilt AOSharp output. Static source/export/disassembly review only; no build, tests, release, CI or in-game verification. This fixes the identified integer-export mismatch; broader new-client handler behavior still needs runtime confirmation.

Personal source fork of [AOSharp by never-knows-best](https://gitlab.com/never-knows-best/aosharp), initially imported from commit `d55eb12b5a763e5ed65851e69c50343de6c6d73c`. Version2.7.11 integrates the net source changes from [Knows Modded AOSharp](https://gitlab.com/aosharp-plus/aosp.knows-aosharp-mods), master commit `5eb65c1053748dad82d16490aca3eaaec01432dc`, also present at [sdts-group/knows-aosharp-mods](https://gitlab.com/sdts-group/knows-aosharp-mods). All upstream authorship and notices remain intact.

The upstream histories share `b45b7a05f9ffd9676d37e620f2f7d481b82ed212`: the modded branch has126 additional commits, and the original branch has one later README commit. Integration is a reconciled source snapshot on top of this fork's existing history, not a replay of those126 commits.

## Plugin startup status

The selected character's plugin rows show orange while loading, green when all entry points in that DLL initialize successfully, and red when loading or initialization fails. Hover the row for the exception or status details. Status is kept per character and cleared on eject; disconnects invalidate success.

The bootstrap reports results through the existing named pipe. Pipe writes happen on its worker thread, not the game thread. Plugin loading and initialization are serialized on the engine thread.

A detected load/initialization failure schedules one automatic clean retry after at least one second, on the next available engine update. The entire plugin AppDomain is torn down and unloaded before loading the selected plugins again, including plugins that succeeded. If cleanup throws or unloading fails, automatic recovery stops. A failed second attempt remains red. There is no infinite retry loop and no sleep on the game thread.

This retry covers reported assembly/core loading and plugin initialization failures after the bootstrap connects. Initial injection/connection failure is reported red but is not automatically retried. Green means initialization returned successfully; a plugin that catches its own error must report/rethrow it before the loader can recognize failure. Native side effects and plugin-created external resources cannot be proven reversible by unloading an AppDomain.

## Source-only validation

Changes were reviewed as source. No compilation, tests, injection, or release publishing was performed. In-game verification remains pending.

## 2.7.6

Remove an unused AOSharp.Bootstrap namespace import from TestPlugin/Main.cs that caused CS0234 when building the solution. The supplied 2.7.5 build log showed AOSharp.exe and Bootstrap compiled successfully; dependency-version warnings remain unresolved. No builds or tests were run for this source fix.

## 2.7.7

Fix startup ordering introduced in 2.7.5: loading the core and immediately initializing plugins allowed AOPluginEntry.Init to dereference DynelManager.LocalPlayer before the first core update populated it. Plugin assembly loading, constructors and initialization now wait until the core has a local player and is not zoning, on both the initial attempt and the clean retry. Core/game updates continue while waiting; waiting does not consume an attempt or report failure. Source review only; no builds or tests run.

## 2.7.8

Restore the injector's EasyHook reference and package path to 2.7.6789. The previous broad version replacement accidentally changed that dependency to 2.7.7789. Version updates now target the exact application version fields. No builds or tests run.


## 2.7.9 and2.7.10

Status text explicitly follows the selected character's row color. Plugin checkboxes stage the next injection while existing plugins keep running. Inject All skips already injected or inactive characters and empty selections; Eject All attempts each injected active character and aggregates failures.

## 2.7.11 — modded upstream integration

Restore named plugin profiles (Save/Load/Del), multi-select DLL addition, Remove All, saved window size/location, the single-instance launcher guard, modded grid styling, in-game item/nano/perk messages, and the chat-receive compatibility change. Remove the upstream-retired TestPlugin project and sources.

Keep this fork's readiness gate, per-character initialization status, clean retry, selected-cell status colors and next-injection staging. Resolve overlapping bulk controls to one Inject All and one Eject All button, both using the existing status-aware Profile methods. Loading a named profile stages selections for all stored characters; it does not eject or change running plugins. Profile files retain the upstream `userPluginProfiles.json` name and layout. Use the same read/write path, update the editable combo's Text binding, report profile file errors, avoid redundant DLL entries, and persist collection resets when removing all plugins.

Keep Console5.0.1/File5.0.0 package declarations matching existing project references instead of importing the modded manifest's inconsistent sink downgrades. DiagnosticSource redirects now target the referenced assembly7.0.0.2; Serilog3.1.1 uses assembly identity2.0.0.0, so the core redirect targets that identity. Application/bootstrap assembly identities remain1.0.0.0; file/informational/application versions and window title advance together.

Source comparison, conflict review, XML parsing and publication checks only. No builds, tests, injection, releases or automatic CI performed. Runtime confirmation remains pending.


## 2.7.12 — drag and drop plugin DLLs

Drop one or multiple DLLs anywhere on the launcher window to add them. The normal Add Plugin picker and drops share the same registration path: ignore non-DLLs and duplicate keys, reject obj paths, save added entries, and report per-file failures together while continuing the remaining files. Adding files stages configuration; injection remains explicit. Paths reference the original files, so keep them in their intended location. The Add Plugin tooltip advertises dropping. No new update-loop work. Source review/XML parsing only; no builds/tests/releases or live drag/drop validation.

## 2.7.13 — shared plugin library and activity tabs

The launcher opens on Plugin list, showing the global DLL catalogue with activity membership columns followed by version/source. Add Plugin, multiselect, window-wide DLL drops, sorting and multiple-row removal remain available. The library has no character, per-character enable, runtime status or injection controls.

The + at the end of the tab strip selects Plugin list and opens an empty activity-column header for typing. Enter commits a nonempty unique activity name; Escape cancels. Naming creates its activity tab while leaving setup visible so membership can be chosen. No default activity or plugin selection is supplied. Save is available; completed names, membership and character selections are also saved as they change.

Each activity shows only its plugin pool, with independent selections per character. Availability in a pool does not enable a plugin for all characters. A single shared client-discovery list and runtime Profile per character track injection ownership. Tab browsing changes the view only. A character already injected under another activity identifies that activity and cannot be injected a second time; eject it from its running activity first. Inject All skips active injections and empty selections. Eject All only targets characters running the current activity.

Selections continue to stage the next injection while the current snapshot runs. Display rows keep status separate from the catalogue, and failed/disconnected statuses remain associated with the activity that attempted injection. Existing pipe identity guards, status colors, bootstrap readiness and clean retry are retained. No changes to bootstrap lifecycle, plugin protocol, Core or plugin assemblies.

This is a fresh setup format in launcher.json beside the launcher, containing the library, activity pools and per-character selections. The earlier config.json and userPluginProfiles.json are neither loaded nor rewritten. It is not a profile migration. Activity rename/delete/reordering, reinject and in-game switching are outside this step.

Exact window/application/executable/bootstrap file and informational versions advance to2.7.13; assembly identities and dependency versions remain unchanged. Source review, XML parsing and publication checks only. No builds, tests, injection, releases or automatic CI; compilation, appearance and in-game behavior await user verification.

## 2.7.14 — retain the registered plugin catalogue

Version2.7.13 started reading launcher.json but ignored the existing DLL catalogue in config.json, making Plugin list appear empty. Restore only that catalogue when the new setup has not initialized its library. Merge missing plugin keys without replacing current entries or activity settings; do not import old character selections or named profiles. Save records the catalogue and initialized marker, so an intentional Remove All stays empty on later launches. Original config/profile files remain untouched. Actual read errors are shown at startup while the current setup remains available; failed recovery can be retried on a later launch.

Only setup loading, error reporting, documentation and exact2.7.14 version fields change. Activity behavior, bootstrap lifecycle, binding identities and dependencies are retained. Source/XML/publication review only; no builds/tests/releases/CI or runtime confirmation.

## 2.7.15 — consistent themed grids and readable headers

Inherit the installed MahApps1.6.5 grid/cell/header/row templates instead of falling back to native light surfaces. The whole grid viewport now uses the control background, including an empty library and space below rows. Headers pair the theme's GrayBrush8 background with TextBrush, replacing an unavailable brush key and fixed white text. Rows, borders and alternating backgrounds use the same palette. Selection uses a subtle themed fill and accent cell borders while preserving row status foregrounds.

The activity view uses the same grid styles. The character list retains its MahApps item template, and column-name editors use themed background/text/caret/border brushes. Remove excess padding inside the small + button so its symbol is visible. App.xaml still selects BaseDark; no Windows theme detection or dependency upgrade is introduced. Brush/style names were checked against the pinned MahApps1.6.5 source, including matching BaseLight palette definitions; this is not a rendered light-theme validation.

Exact application/window/executable/bootstrap versions advance to2.7.15. Configuration, activity selections, injection and bootstrap behavior are unchanged. Source/XML/publication review only; no builds/tests/releases/CI or post-change Windows rendering validation.

## 2.7.16 — Windows supplies the colours

Remove the forced Blue/BaseDark dictionaries. WindowsTheme fills the existing MahApps colour roles before the first window opens and refreshes them on Windows preference/high-contrast notifications, marshalled to the WPF dispatcher and detached at exit. Windows UISettings supplies application background/foreground, accent and available surface/border/disabled roles. A contrast scheme uses SystemColors directly, preserving its window/control/menu and highlight/text pairs. If modern APIs are unavailable, native system colours remain the fallback. No RGB light/dark presets, theme picker, package upgrade or new SDK reference is added.

The small UISettings interop boundary uses documented Windows Runtime activation and the Microsoft Windows ABI for IUISettings/IUISettings3; acquired interfaces, HSTRING and successful runtime initialization are balanced on every path. Optional newer colour roles can fall back to the Windows foreground/background pair. Accent selection text is chosen from those two Windows-supplied inks for contrast. Existing control templates still determine layout and some interactions: this uses Windows-reported colour roles, not a pixel-for-pixel reproduction of every Windows or third-party skin.

Register the Windows-supplied resource dictionary with the pinned toolkit so its existing message dialogs can detect the theme. Named solid brushes retain their identity and update in place, including references held by an already open dialog. The progress brush retains the toolkit's gradient brush type. Windows settings are read only; no personalization settings are written.

Both launcher windows, grid/header/editor/selection surfaces and toolkit brush roles follow these resources. Fixed red/green/orange row text is replaced by status words and typographic emphasis; initialized rows are semibold, failed/missing-file rows bold, loading italic, injected characters bold with their running activity still shown. Missing DLLs have an explicit tooltip. Selection uses its matched Windows text/background pair. Tab captions use ordinary readable text and emphasize the selected tab; the + header keeps its button content.

Exact title/application/injector/bootstrap file and informational versions2.7.16; assembly identities, dependencies, configuration and activity/injection behavior retained. Source/diff/XML/resource/ABI/publication review only. No builds/tests/releases/CI; Windows rendering, live colour-change notifications and injection remain unverified by this source-only delivery.

## 2.7.17 — clearly selected navigation tabs

Retain the existing WPF TabControl/TabItem and MahApps1.6.5 templates. Selected tabs use the Windows selection background/text pair and the toolkit's built-in SelectedTabItem underline. Hover over another tab does not imitate that selection marker; selection stays visible when focus moves or an activity has no plugins. Header text follows its TabItem foreground, and the selected weight uses the ControlsHelper.HeaderFontWeight property actually consumed by this toolkit version.

Selection marks the page being viewed; it does not switch an injected character's activity or represent runtime ownership. Existing + column-header naming, library startup, horizontal scrolling, staged selections and all injection/configuration behavior remain. No custom tab renderer, new package, close/drag/shrink behavior or automatic runtime tab colouring introduced.

Exact title/application/injector/bootstrap file/informational versions2.7.17, binding identities/dependencies unchanged. Source/diff/XML/pinned template/trigger and publication review only; no builds/tests/releases/CI or post-change Windows rendering confirmation.

## 2.7.18 — setup changes save automatically

Remove the redundant Save buttons from Plugin list and activity views. Existing completed-name, pool-membership, character-selection and library add/remove actions continue to save immediately. Enter or accepted focus loss commits an activity name; unfinished or invalid names remain drafts. Setup instructions no longer ask for a separate Save action.

A shared status line starts with Changes save automatically, reports Saved after a successful write, and keeps a failure message with Retry available on either view after a write error. The existing error dialog retains details; overlapping failures do not open duplicate dialogs. Closing retries a failed write and asks before discarding any still-unsaved changes, with keeping the window open as the default. Saving does not apply changes to running injections.

Exact title/application/injector/bootstrap file/informational versions2.7.18; assembly identities, dependencies, configuration format, theme resources and injection behavior unchanged. Static source/diff/XML/event-binding and publication review only; no builds, tests, releases, CI or Windows runtime verification.

## 2.7.19 — activity and plugin status colours

Activity tabs aggregate all online characters: green when any character is injected into that activity, red with priority when any character has a failed plugin status scoped to that activity, otherwise ordinary Windows colours. Failed bootloader attempts and loader disconnections can show red while the client remains online. Ejection or a later status snapshot clears the relevant cue; offline characters are excluded. Captions include Injected/Failed and tooltips name injected/failed characters. Green means injected, not proof that every plugin function works. Staged selections do not alter runtime ownership or these summaries.

Subscribe to status/property changes for every discovered character, independently of the selected character/tab. Reconcile subscriptions on collection changes and detach at window close. Coalesce dispatcher updates after a discovery pass so its temporary inactive flags do not render as tab flicker. The existing selected underline and header weight remain separate from runtime colour; selected/hover-selected underline ink follows the tab's matched foreground. Library and + remain navigation/setup controls.

Restore green Initialized rows and red Failed/missing-DLL rows. Calculate text shades for actual normal and selected Windows backgrounds, retaining at least4.5:1 text contrast, existing status words, tooltips and emphasis. Compact Initialized/Failed status labels retain the green/red fill even when selected name text needs a subtler shade. Semantic tab/status fills use paired white text on green/firebrick. Windows contrast schemes retain their own surface/text and selection pairs, with explicit status labels. Theme preference refresh updates these resources in place. Ordinary Windows colour inheritance remains.

Exact window/application/injector/bootstrap file/informational versions2.7.19; assembly identities/dependencies, configuration, loading and injection behavior retained. Static source/XML/resource/event-lifecycle/contrast calculations and exact publication review only. No builds, tests, releases, CI or Windows rendering/runtime verification.

## 2.7.20 — rename and delete activity tabs

Right-click an activity tab for Rename or Delete; the target is the clicked activity even when another page is selected. Plugin list and + remain permanent. Rename starts with the current name and shares creation's nonempty/reserved/unique name rules; changing only case is allowed. Preserve the activity ID, plugin pool, character choices and runtime/status ownership. Update its Plugin list column, coloured tab caption and any running-character display label; do not reload or switch injections.

Delete asks before removing the activity and its assignments, with Cancel as default. Keep the global DLL catalogue and files. Disable Delete while any character still owns an injection in that activity, and recheck ownership after the confirmation before removing anything. Select Plugin list before removing the selected activity, including the last one, so + cannot be selected automatically. Deleting an unselected tab leaves the selected page in place. Rename/delete use existing immediate saving and save-error/retry feedback.

Exact title/application/injector/bootstrap file/informational versions2.7.20; assembly identities, dependency versions, configuration format and injection lifecycle retained. Static source/diff/XML/pinned dialog API/event/identity review and exact publication checks only; no builds, tests, releases, CI or Windows runtime confirmation.


## 2.7.24 — queued activity handover

Reinject and Reinject All share a serial launcher queue with built-in bootstrap `/ao#` commands and a clickable activity list. Target plugin paths are captured and validated before unloading. The existing native bootstrap and pipe remain connected while the engine thread tears down and unloads the old managed domain, acknowledges cleanup, and loads the new domain. Plugin reports carry operation IDs; only the current generation updates the launcher. A capability/readiness handshake rejects older injected runtimes. Cleanup failure and unconfirmed timeouts block further operations until recovery; other characters may proceed. Browsing and staging remain available, and active/pending activity deletion stays guarded.

See [Switching activities](docs/PROFILE-SWITCHING.md) for commands, batch scope and limits. Unexpected loader disconnects also require a game-client restart because cleanup is unconfirmed. Binding identities/dependencies remain unchanged. Static source/XML/diff/publication review only; no builds, tests, releases, CI or in-game verification. Native effects beyond managed cleanup and plugin-reported initialization remain unproven.


## 2.7.25 — short plugin names and in-game toggles

Plugin list adds Short name immediately after Name. Right-click Edit assigns a unique case-insensitive single-word alias (up to 32 characters; blank clears it), saved with the plugin catalogue. Built-in `/load name` and `/unload name`, optionally followed by `all`, submit a one-plugin change to the existing queue. Each character keeps its live activity and other live DLL selection; unrelated staged choices wait for a later explicit Reinject. The requested checkbox saves automatically; loading a registered plugin outside that activity's pool adds membership for that activity only. Empty reloads retain Core/bootstrap and are reported as connected with no plugins. The managed environment reload restarts remaining plugins too.

The control protocol advances to2 and needs matching 2.7.25 launcher/bootstrap with a fresh injection. Native command interception reserves load/unload alongside ao# while injected. No plugin repository or dependency changes. See [Plugin commands](docs/PLUGIN-COMMANDS.md) for scope/failure behavior. Static source/XML/diff/publication review only; no builds/tests/releases/CI or Windows/game verification.


## 2.7.28 — optional plugin command interception

Core exposes `Chat.RegisterCommandOverride` for narrow native-command overrides. A callback consumes only syntax it handles; returning false leaves AO's command intact. Bootstrap dispatches an override once per slash input before native command lookup, after launcher controls. The delegate is optional for older Core versions; assembly binding identities remain unchanged. Handlers can use this for group `/pet wait`, `/pet follow`, `/pet attack`, and `/pet warp` while preserving other native pet commands. Deploy matching Bootstrap and Core output and restart the injector/game session for the native hook update. Source delivery only; runtime confirmation pending.
