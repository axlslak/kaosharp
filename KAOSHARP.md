# KAOSharp 2.7.18

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
