# Publication preparation checks — 2026-10-30

- Built using .NET SDK 10.0.300 and local game references: success, zero compile errors.
- NuGet emitted NU1900 because its vulnerability-data endpoint was unreachable. This check does not establish dependency vulnerability status.
- Built HotLoad.dll SHA-256: `C3CD2945F00985E10AA803D72152F0842C3DC81C5F55A20232F1685237CA4B8F`.
- Runtime `.cs` files were copied without changing their contents. The project only replaces the machine-specific game path with a build parameter.
- No game installation, save modification or new in-game verification was performed during publication preparation.
- The distribution is source-only. The built DLL and intermediate build files are excluded from the source archive.
