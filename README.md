# KAOSharp

Personal source fork of [AOSharp by never-knows-best](https://gitlab.com/never-knows-best/aosharp), integrating [Knows Modded AOSharp](https://gitlab.com/aosharp-plus/aosp.knows-aosharp-mods) and its contributors. Upstream authorship and notices remain intact.

This tree uses the classic Windows/.NET Framework 4.8 WPF launcher and EasyHook. It is not the separate .NET8 launcher.

Version **2.7.11** includes named plugin profiles (Save/Load/Del), multi-select DLL addition, individual and bulk injection/ejection, saved window geometry, and plugin initialization status with one clean retry. Plugin choices can be staged while the current set is running; loaded profiles take effect on the next injection.

See [KAOSHARP.md](KAOSHARP.md) for source provenance, behavior and validation limits.

Upstream related projects: [aosharp.bots](https://gitlab.com/never-knows-best/aosharp.bots) and [aosharp-automation](https://gitlab.com/never-knows-best/aosharp-automation).
