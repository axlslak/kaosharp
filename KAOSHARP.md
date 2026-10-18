# KAOSharp 2.7.12

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
