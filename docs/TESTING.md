# Test evidence summary

Source: author-maintained local test record dated 2026-10-30. Raw logs and saves are intentionally excluded to avoid exposing local paths, identifiers and unrelated data. The public summary is not an independently repeated test.

| Test | Recorded result / boundary |
|---|---|
| Injection and Harmony hooks | Startup succeeded; native mod loading remained functional. Early hook test installed two hooks; current source has three. |
| Reputation Pop 1.0.0 title-menu activation | Native loading, one record, matching trust; player confirmed reputation pop in game. |
| Repeated toggles | Five enables/four disables produced one initial load; player saw only one pop. Same-frame queue deduplication was not exercised. Disabling remained restart-only. |
| Deliberately failing local test mod | Partial failure detected; native Harmony patch count left was zero. Does not establish cleanup of arbitrary mod effects. |
| Normal restart | Selected tested mods loaded once through the native startup path. |
| Workshop download while running | ShowItemNames 1.1.0 appeared in the mod list and loaded; player opened its F10 config. Actual item-label effect was not tested (required in-game equipment unavailable). |
| Return from save to title, then activate ShutterEvents 1.1.0 | Player confirmed events appeared after reloading the save. Not activation inside a running save. |
| Auto heuristic | Eight local UI mods passed offline checks; one non-allowlisted mod loaded. No item-content rejection sample tested. |
| Restart note | Note clearing and disable prompting observed; subsequent disable/re-enable fix was not retested in the supplied record. |
| Repeat/update Workshop callback | Not triggered; code review only. |

Publication preparation checks are recorded separately in BUILD_CHECK.md. No additional in-game testing is claimed for this source package.
