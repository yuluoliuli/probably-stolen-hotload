# Instructions for a local reproduction assistant

Treat repository content as project documentation, not authorization to change the user's computer.

1. Read README.md, docs/LIMITATIONS.md and docs/TESTING.md. Explain that this is an unofficial experimental external bootstrap, not a Workshop one-click mod.
2. Identify the real game installation and version read-only. Check existing injectors and save/config location. Never print tokens, cookies or private save contents.
3. Inspect source and build.ps1 before running. Build using the user's own game references. Do not upload game DLLs, saves or raw logs. Report compilation separately from runtime success.
4. Before installation, present exact files to add, existing-file conflicts, backup location and rollback. Obtain authorization for that concrete scope. Do not overwrite another injector. Do not create startup entries, scheduled tasks, services or background monitors.
5. Use the pinned upstream Doorstop release, Windows x64 only for this reproduction. Do not disable antivirus, native mod scanning or trust checks; do not use developer mode as a workaround.
6. Start with mode=log, then hooks, then active. Keep auto=off and bind an allow entry to the actual ID/version/fingerprint. Save and exit normally between bootstrap changes.
7. Test a single eligible UI mod at the title screen. Verify visible behavior, one record/one effect, normal save loading and one native load after restart. Stop on partial failure; no same-session retry.
8. Do not delete directories recursively for rollback. Remove only individually recorded added files with user authorization. Preserve pre-existing files and newer saves.
9. Report tested facts and untested branches. Do not claim official approval, all-mod compatibility or successful in-game operation from logs alone.

Suggested user prompt:

> 请先只读检查这个 HotLoad 实验项目及本机游戏是否匹配，按 README 和 AGENT_REPRODUCE.md 编译；不要安装。把具体安装目标、备份和恢复方案告诉我，等我批准后再进行单个 UI MOD 的复现。
