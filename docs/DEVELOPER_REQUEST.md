# Request: supported title-screen mod activation / Workshop manager integration

Submitted request: https://github.com/dragonpan2/probably-stolen-modding/issues/1 (2026-10-30). No developer approval received. The submitted message also links to the public source repository.

Hello Questing Goose Studio team,

I have an experimental, source-available proof of concept for activating selected mods from the title-screen mod menu without restarting Probably Stolen. I would like to ask for an officially supported integration path before presenting it as a normal Workshop mod.

The local test environment reports application 0.46D, Unity 2021.3.45f2 Mono, with the game's own Harmony. The prototype uses UnityDoorstop as an external bootstrap, queues work on the main thread after native enabling, and calls the native folder-loading path. It retains native approval and load-time trust evaluation, but the bootstrap itself is outside the native scanner; I do not want to disguise that distinction or evade loader-target restrictions.

Recorded tests include title-screen activation of Reputation Pop, registration of a newly downloaded Workshop item without restarting, and activation of ShutterEvents after returning to title followed by reloading the save. Normal restart loaded these selected mods once. This is a limited PoC: no hot-disable, no in-place DLL updates, no promise for content mods or arbitrary late initialization. Known failure/restart-notification edge cases are documented.

Would you consider either:

1. An official API to refresh installed Workshop items and activate eligible mods at the title screen after the existing approval flow; or
2. A documented, explicitly approved integration mechanism for a reviewed manager, including how it may be distributed on Workshop?

Could you also clarify whether distributing the current source-only external-bootstrap PoC on GitHub is acceptable under your modding policy, and whether a Workshop companion is permitted? I am not assuming that passing the scanner or a verified label grants an exemption.

The source can be shared for review, with no game DLLs, assets, saves or third-party mod binaries included. I would be happy to adapt it to a supported API, or retire the bootstrap if the feature becomes native.

Thank you for your time.
