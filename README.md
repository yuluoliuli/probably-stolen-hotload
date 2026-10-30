# Probably Stolen HotLoad — experimental source PoC

《深空当铺》标题界面免重启激活 MOD 的实验性源码。不是官方功能，也不是订阅即用的工坊 MOD。尚未取得开发者对外部分发或工坊接入的认可。

Already-tested idea: after the native enable/approval flow, queue a main-thread call into the native folder loader, refresh the mod list and load resources. Newly installed Workshop items can be registered without restarting. Native approval and load-time trust evaluation remain in use; **HotLoad itself is an external bootstrap and is not covered by the native mod scanner**.

## Scope and evidence

Reported test environment: Windows x64, game application version `0.46D`, Unity `2021.3.45f2` Mono, game-supplied Harmony `2.4.2.0`, UnityDoorstop `4.5.0`. This is not a promise of support for every game build. See [test summary](docs/TESTING.md) and [known limitations](docs/LIMITATIONS.md).

- Supports experiments with title-menu activation and Workshop discovery.
- Does not hot-disable mods, unload assemblies, or update already loaded code.
- Does not provide a generic mid-save activation mechanism. The tested game's mod menu is in the title screen.
- No game assemblies, third-party mods, saves, raw player logs, or Doorstop binaries are distributed here.
- This source snapshot includes changes newer than some recorded game tests. Building it is not a new end-to-end game test.

## Build (no installation)

Requirements: a legitimate local game installation matching the tested APIs, PowerShell, and a .NET SDK supporting the SDK-style project (publication build checked with SDK 10.0.300). References come from your own game installation. The output must not contain copies of game libraries.

```powershell
./build.ps1 -GameDir 'X:\SteamLibrary\steamapps\common\Probably Stolen'
```

Output: `out/HotLoad.dll`. The build command does not copy anything into the game, launch it, or change saves. Do not upload referenced game DLLs to solve another user's build failure.

## Controlled manual installation

1. Exit the game after using its normal save mechanism. Back up the game's save/config directory under `%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen` and record the game-root file list. Preserve this backup locally; do not publish it.
2. Check for an existing `winhttp.dll`, `doorstop_config.ini`, `HotLoad` directory or another injector. If present, stop and resolve compatibility; do not overwrite another mod loader.
3. Download **UnityDoorstop 4.5.0 Windows x64 release** from [the upstream release](https://github.com/NeighTools/UnityDoorstop/releases/tag/v4.5.0), not a third-party mirror. Follow its directory layout to select the x64 `winhttp.dll`. Keep upstream license/source information. This repository does not bundle or automatically download it.
4. Place that `winhttp.dll` and this repository's `config/doorstop_config.ini` beside the game executable. Create `HotLoad` there, then copy `out/HotLoad.dll` and `config/hotload.cfg` into it. These are explicit application modifications; an assisting agent must first show the target paths and rollback plan to its user.
5. Start with `mode=log`; check `HotLoad/hotload.log`, game startup and native mod behavior. Exit normally, change to `mode=hooks`, restart and verify hook installation and native behavior.
6. For activation testing, exit normally, use `mode=active`, keep `auto=off`, and add `allow=ID|version|fingerprint` for a selected UI mod using your own native audit fingerprint. A mod already loaded at startup is not a fresh hot-load test. Disable the target and restart first, then enable it at the title screen and verify its actual game effect.
7. `auto=on` is an optional, unproven heuristic: absence of certain startup-only content/hooks does not establish compatibility. Do not treat it as approval to enable every mod.

Installing the bootstrap requires an initial game restart. Subsequent supported mod activation is the feature being tested. Do not enable developer mode, disable native scanning, or hide loader access to make a test pass.

## Disable / rollback

Exit the game. Set `enabled=false` in the Doorstop configuration to disable injection. For complete removal, remove only the individually recorded files you added (`winhttp.dll`, `doorstop_config.ini`, `HotLoad/HotLoad.dll`, `HotLoad/hotload.cfg`, and any generated `HotLoad/hotload.log`), then remove `HotLoad` only if empty. Never delete a pre-existing loader's files. Restore your own backup only if necessary, understanding that restoring it discards newer progress. Steam file verification may leave added files in place.

## Agent-assisted reproduction

Give DSH/Codex/another local agent [AGENT_REPRODUCE.md](AGENT_REPRODUCE.md). Read and build first; installation is a separate, explicitly reviewed step. Agents should not create tasks, services or startup entries. Successful compilation/logging does not prove the feature works in the game.

## Developer collaboration

We would prefer an official title-screen activation API or an explicitly approved manager integration over external injection. See [the request](docs/DEVELOPER_REQUEST.md). No endorsement should be inferred unless a developer reply is linked there.

## License

HotLoad's original source and documentation in this repository are MIT licensed; see LICENSE. Game code, assets and libraries are not included or relicensed. [UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) is a separate LGPL-2.1 project and must be obtained under its own terms.
