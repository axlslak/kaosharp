# KAOSharp

Personal source fork of [AOSharp by never-knows-best](https://gitlab.com/never-knows-best/aosharp), integrating [Knows Modded AOSharp](https://gitlab.com/aosharp-plus/aosp.knows-aosharp-mods) and its contributors. Upstream authorship and notices remain intact.

This tree uses the classic Windows/.NET Framework 4.8 WPF launcher and EasyHook. It is not the separate .NET8 launcher.

Version **2.7.24** starts on **Plugin list**, your shared plugin library. Add DLLs together with Add Plugin or drag and drop. Use **+** to create an activity column, type its name in the header and press Enter, then tick the plugins available in that activity. Open its new tab to choose plugins for each character and inject. Right-click an activity tab to Rename or Delete it. Renaming keeps its assignments and updates its Plugin list column and running-character label. Deleting asks first and keeps the shared plugin library; eject characters using that activity before deleting it.

Different characters can run different activities. Choices can be prepared while plugins are running; Reinject applies the staged selection through an acknowledged unload/load handover. Reinject All queues injected characters connected to this launcher. Switching tabs leaves running clients alone. Eject All affects the current activity only. Initialization status, the one clean retry, and saved window geometry remain available.

Activity captions use green when all participating characters report successful plugin initialization, red when all participating attempts fail, and orange for mixed, pending or uncertain results. Neutral captions use normal Windows text (white in dark mode); all tabs share the same surface and keep their activity names. Plugin list stays blue. Tooltips explain the character results. The selected tab keeps its own underline and header emphasis, including on a coloured tab. Browsing tabs still changes only the page being viewed.

Setup is stored in `launcher.json` beside the launcher. On first use of this format, registered DLLs are recovered from the existing `config.json`; current library entries and activities are kept. Completed setup changes save automatically, including the recovered catalogue. A shared status line confirms successful writes and offers Retry if writing fails. Closing retries failed writes and asks before discarding changes that remain unsaved. Old character selections and named profiles are not imported, and the older files are not changed. Keep your DLLs in their intended folders; registration references those files.

Appearance uses colours reported by Windows, including app surfaces, text, accents and selection. Windows contrast schemes take priority. Preference notifications refresh the colours while the launcher is open; older systems use their native system colours. The existing MahApps controls remain in use. Initialized plugin rows are green; failed plugins and missing DLLs are red. Name text adjusts for the actual normal and selected backgrounds; compact green/red status labels keep the colour visible on selected rows too. Words and font emphasis remain. Windows contrast schemes use their own paired colours and the same status labels.

Use `/ao#` in game to list activity choices, `/ao# db1` to switch this character, or `/ao# db1 all` to queue connected, injected characters. No separate control plugin is needed. Use a matching 2.7.24 launcher/bootstrap with a fresh injection; older injected runtimes cannot accept the new protocol. See [Switching activities](docs/PROFILE-SWITCHING.md) for queue behavior and recovery.

See [KAOSHARP.md](KAOSHARP.md) for source provenance, behavior and validation limits.

Upstream related projects: [aosharp.bots](https://gitlab.com/never-knows-best/aosharp.bots) and [aosharp-automation](https://gitlab.com/never-knows-best/aosharp-automation).
