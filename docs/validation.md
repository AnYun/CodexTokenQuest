# Cross-platform validation

Validation on 2026-09-05 for the shared Avalonia migration. Automated checks do not substitute for desktop interaction tests.

| Check | macOS Apple Silicon | Windows 11 ARM (Parallels) |
| --- | --- | --- |
| Same solution builds with .NET 10 | Passed | Passed |
| Core/parser and new platform policy tests | Passed | Passed |
| Interprocess lease and crash recovery | Passed | Passed |
| JSONL protocol, authentication error and unsupported usage method | Passed with local test server | Passed with local test server |
| 128 theme/language/panel/data-state combinations render | Passed | Passed |
| Native HUD launches | Passed, local `.app` | Passed, PowerShell background bootstrap |
| Host identity / foreground detection | Observed after replacing stale Accessibility entry: permission true, foreground transitions and bounds on a display with negative X | Observed Codex package, foreground and background transitions, 250% DPI |
| Five concurrent bootstrap invocations | Passed; entries returned in 0.02s, one HUD | Passed; entries returned in 740ms, one HUD |
| Live account usage | Passed: rate limits and daily usage returned | Passed: rate limits returned; daily history was unavailable |
| Accessibility introduction | Passed | Not applicable |
| Real multi-monitor / mixed DPI moves | Not yet exercised | Not yet exercised |
| Full-screen / Spaces transitions | Not yet exercised | Not yet exercised |
| Focus retention while typing in Codex | Not yet exercised | Not yet exercised |

The computer-use tool cannot operate the Codex host application. Host state can be verified in `lifecycle.log` while a person moves, minimizes or switches away from Codex. Permission checks and grace-period behavior also have deterministic policy tests, but these do not verify macOS permission revocation or window-server behavior.

A locally ad-hoc-signed Mac rebuild may invalidate Accessibility authorization. Authorize the final `.app` at `~/Library/Application Support/CodexTokenQuest/Codex Token Quest.app` and restart if macOS requests it.

The rebased HUD size (new 100% = previous 80%) passed both local builds and all 128 render combinations on both systems. Settings checks cover converting a saved 80% to 100%, preserving other settings, and preventing another conversion after save/reload.

The original WinForms source uses Consolas Bold. Both shared builds now resolve that same family and weight; on this Mac, the existing Microsoft Office Consolas files were installed into the user's Fonts directory. No font files are distributed with the plugin. A fixed HUD label line height keeps the Mac Chinese fallback from pushing today's tokens outside the stats panel when a warning is present. Both systems passed all 128 render cases after the change, including checks that the full reset label is not ellipsized. Countdown regressions cover English/Traditional Chinese day, hour and minute boundaries, unknown reset time, zero and expired values. Both installations were updated to `1.0.2+codex.20260905051643`; live logs confirm visible native HUD windows and refreshed usage with 3 quota windows and 94 daily buckets. The rebuilt Mac app's existing Accessibility entry was re-registered and tracking resumed.

Mac interaction exposed two additional issues: replacing the exported tray menu while saving settings threw a native exception, and first-show resize notifications could leave the HUD at 0 × 0. The menu now retains its identity, and placement restores dimensions after show and checks actual dimensions on subsequent tracking ticks. A simulated native resize regression passes on both systems. The user confirmed the repaired Mac HUD appeared; its log reports a 330 × 296 client size. The Mac settings file subsequently saved `HudScalePercent: 100` and `HudScaleVersion: 1`, with host tracking continuing after the save. Both installed copies were updated to `1.0.2+codex.20260905050438`.

The Windows npm CLI returned `-32600` with a 2,317-character unknown-method message for `account/usage/read`. The shared reader now tries other installed CLIs when PATH lacks this capability. A live Windows check selected the desktop-bundled CLI and recovered lifetime usage, all 94 daily buckets and 3 quota windows without warnings. Regression tests cover this response and standard `-32601`, compatible PATH preference, fallback reuse, failed alternatives, authentication failures and transient recovery. HUD layout tests include long warnings and errors across all themes, languages and panels, checking notice bounds, footer separation and today's token visibility.

The subsequent typography update increases HUD, chart and settings text by one logical unit, centers button content vertically, and reduces the header row from 42 to 36 units. Both systems passed the 128 render combinations and regression suite, and installed version `1.0.2+codex.20260905062541` was restarted successfully. Native logs show visible HUDs and live usage on both systems after the Mac Accessibility entry was refreshed.

The visibility policy was subsequently restored to the original Windows behavior: focus loss alone does not hide the HUD or start a hide timer. A usable host window is still required; minimized, hidden or off-desktop windows, permission loss and manual hiding remain hide conditions. Mac tracking now reads background window bounds and uses normal window level when the host is inactive. Both platform regression suites passed. Installed version `1.0.2+codex.20260905063316` produced native logs with `foreground=False` and `visible=True` on both Mac and Windows, while live usage continued to refresh.

A follow-up corrects two native issues: the Mac HUD now retains floating level 3 when inactive (the prior visible flag did not prove it was above the host), and Windows candidates exclude menu classes, tool windows and owned popups and require a 640 × 480 logical-pixel main window. DPI and popup classification regressions pass on both systems, as do all 128 layout combinations. Version `1.0.2+codex.20260905064347` is installed on both systems. Windows startup retains the real main-window bounds and refreshes usage; actual menu interaction remains to be confirmed. After the user re-added the rebuilt Mac app to Accessibility, native verification observed Chrome (`com.google.Chrome`) as the foreground application while the HUD remained in the on-screen window list at floating layer 3 with alpha 1 and bounds X=-379, Y=738, Width=363, Height=326. The host tracker simultaneously reported permission granted and foreground false with valid Codex bounds. This verifies native background stacking rather than only the Avalonia visible flag.

Settings now use the shared pixel frame and palette, themed input and button states, plus/minus adjustments and a fixed Save/Cancel footer. Eight theme/language settings views pass rendering, default-size field visibility, draft retention across live theme changes and saving checks on both systems, alongside the 128 HUD cases. Saving merges current HUD theme/panel/hero/mode so changing themes while options are open does not restore stale selections. Version `1.0.2+codex.20260905070545` was installed on both systems; Mac Accessibility was renewed after user authentication. Native verification again shows the HUD on screen at floating layer 3 while Chrome is foreground; Windows startup and live refresh also succeeded. The settings controls themselves were validated through rendered views and automated save interactions, rather than a full native click-through.

## Repeat automated checks

```sh
dotnet build CodexTokenQuest.slnx -c Release
dotnet run --project tests/CodexTokenQuest.Tests -c Release --no-build -- --render
```

Generated previews are in `artifacts/qa` and are not tracked. The GitHub Actions workflow runs the same build and tests on both OS runners; remote CI has not been run from this working tree.

When using a shared Mac/Windows checkout, `scripts/validate-windows.ps1 -Source <checkout>` copies the source to a dedicated Windows temporary directory before building, excluding Git metadata and prior outputs.

## Desktop acceptance checklist

- Start through both Hook entry points; launch several copies concurrently and confirm only one HUD appears.
- Confirm the Codex window's bottom-right attachment and move it across monitors with different scaling.
- Minimize/restore Codex, switch apps, switch Spaces/desktops and enter/leave full-screen.
- Confirm showing/refreshing the HUD does not interrupt typing; buttons, hero selection, tray and settings remain interactive.
- Hide the HUD manually, change foreground apps, and confirm it stays hidden until explicitly restored.
- Deny, grant and revoke Mac Accessibility; no repeated prompt loops or false process-exit handling.
- Close all Codex windows while the process remains running; HUD waits. Quit Codex; HUD exits after five seconds.
- Change every saved setting, restart, and confirm persistence; verify an existing Windows settings file survives migration.
