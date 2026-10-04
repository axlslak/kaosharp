# KAOSharp 2.7.7

Personal source fork of [AOSharp by never-knows-best](https://gitlab.com/never-knows-best/aosharp), based on upstream commit d55eb12b5a763e5ed65851e69c50343de6c6d73c. All upstream authorship and notices remain intact.

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
