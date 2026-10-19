# KAOSharp

Personal source fork of [AOSharp by never-knows-best](https://gitlab.com/never-knows-best/aosharp), integrating [Knows Modded AOSharp](https://gitlab.com/aosharp-plus/aosp.knows-aosharp-mods) and its contributors. Upstream authorship and notices remain intact.

This tree uses the classic Windows/.NET Framework 4.8 WPF launcher and EasyHook. It is not the separate .NET8 launcher.

Version **2.7.14** starts on **Plugin list**, your shared plugin library. Add DLLs together with Add Plugin or drag and drop. Use **+** to create an activity column, type its name in the header and press Enter, then tick the plugins available in that activity. Save and open its new tab to choose plugins for each character and inject.

Different characters can run different activities. Choices can be prepared while plugins are running; they apply to the next injection. Switching tabs leaves running clients alone. Eject All affects the current activity only. Initialization status, the one clean retry, and saved window geometry remain available.

Setup is stored in `launcher.json` beside the launcher. On first use of this format, registered DLLs are recovered from the existing `config.json`; current library entries and activities are kept. Save retains the recovered catalogue. Old character selections and named profiles are not imported, and the older files are not changed. Keep your DLLs in their intended folders; registration references those files.

See [KAOSHARP.md](KAOSHARP.md) for source provenance, behavior and validation limits.

Upstream related projects: [aosharp.bots](https://gitlab.com/never-knows-best/aosharp.bots) and [aosharp-automation](https://gitlab.com/never-knows-best/aosharp-automation).
