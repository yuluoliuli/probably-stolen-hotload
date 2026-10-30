# Known limitations of this source snapshot

This is a reproducibility snapshot, not a stable end-user release. No automatic installation or compatibility guarantee is provided.

1. Partial failure: native cleanup removed the test mod's Harmony patch, but arbitrary event subscriptions, objects and side effects cannot be generally undone. Restart after any partial failure.
2. The `already loaded` branch clears a pending restart reason when `Instance` or `Assembly` exists. That also includes partially failed loads; do not retry a failed mod in the same session. This interaction has not been fixed or tested in this publication snapshot.
3. Restart-reason tracking covers only selected paths. Other native restart reasons can be missed; the cleared global note is not proof that restarting is unnecessary.
4. `HOT LOADED` is logged after starting the sound coroutine, not after confirming all asynchronous resources and callbacks completed. Verify actual behavior.
5. Auto eligibility is heuristic and not exhaustive. Startup dependencies through reflection, indirect calls, delayed subscriptions or other mods can escape it. Keep `auto=off` for controlled reproduction.
6. The code does not enforce a runtime game-build allowlist or explicit title-scene guard. The tested UI is title-only; other callers or future game versions are unsupported.
7. Multiple entry assemblies/instances with the same mod ID are not supported reliably: code warns on multiple records and continues. Limit experiments to known single-entry mods.
8. Folder ownership uses a prefix test without a path-separator boundary. Similar folder names may cause a false refusal.
9. Hook-install failure can leave earlier hooks installed. If hook setup fails, exit and disable Doorstop rather than continuing the experiment.
10. Workshop repeat/update notifications and native rejection after placeholder removal were not exercised end to end. Game updates require fresh validation.

These issues are disclosed rather than silently changing the historical runtime code while claiming its old tests validate a new version. Contributions should include focused evidence and distinguish code review, build checks and game tests.
