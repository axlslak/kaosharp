# Working on kaosharp

- Personal fork of https://gitlab.com/never-knows-best/aosharp. Preserve upstream credit and history.
- Push source changes only. Do not build, run tests, publish releases, or enable automatic CI builds unless the user explicitly changes this instruction.
- Increment the patch version for every change delivered: 2.7.4 -> 2.7.5 -> 2.7.6. Update the window title, executable/bootstrap file and informational versions, and application version together.
- Keep assembly identity versions stable for plugin binary compatibility unless an actual compatibility change requires otherwise.
- Never claim that successful initialization proves a plugin is operational: plugins can swallow their own failures.
