# Codex Token Quest

English | [繁體中文](README.zh-Hant.md)

A read-only native C# pixel RPG HUD for Codex Desktop. On Windows it runs as a non-activating tool window that follows the ChatGPT/Codex host window and refreshes automatically.

- each available quota window and percentage used/remaining;
- the next reset time in local time;
- lifetime and recent daily token activity when the account provides it;
- the number of available earned reset credits.

The companion does not inject code into or modify the Codex application. It observes the top-level ChatGPT/Codex host window and stays anchored inside its lower-right corner.

No ChatGPT access token or API key is read or stored by this project.

## Requirements

- .NET 10 SDK or newer
- Codex CLI available as `codex`
- Codex CLI signed in with a ChatGPT-backed account

API-key-only and Amazon Bedrock authentication do not expose ChatGPT token-activity summaries.

## Solution structure

- `CodexTokenQuest.Core`: class library containing the Codex App Server client, usage models, parsers, and local token reader.
- `CodexTokenQuest.Desktop`: native WinForms RPG HUD (`WinExe`), with no console window.
- `CodexTokenQuest.Tests`: parser and local-token regression tests.

## Run

```powershell
.\scripts\start-desktop.ps1
```

## Codex Desktop

The companion is a standalone Windows UI and does not install a Codex skill or MCP server. The window:

- appears automatically in the lower-right corner only while the desktop host is visibly in Codex mode;
- hides while the desktop host is closed, minimized, switched to ChatGPT mode, or behind another application, then returns when Codex mode is in the foreground again;
- follows Codex when it moves or resizes;
- provides a pixel-art RPG camp with a hero portrait, level, EXP, today's quest EXP, and weekly stamina;
- turns lifetime tokens into exponential RPG experience levels from 1 to 99, with a configurable EXP base (`1K`–`1T`) for pacing;
- includes four selectable pixel heroes unlocked at levels 1, 10, 25, and 50, and remembers the selected hero;
- separates the HUD into remembered `CAMP`, `QUESTS`, and `HISTORY` panels for character progress, stamina modules, and the seven-day quest EXP chart;
- includes four persistent visual themes—Pixel Dungeon, Arcane Glass, Guild Ledger, and Code Terminal—cycled from the style control;
- supports persistent English and Traditional Chinese window text, selectable from the game options;
- refreshes account usage every 5 minutes by default and reset countdowns every second;
- lets you set a persistent 1–1440 minute refresh interval from the notification-area menu or the update text at the bottom of the window;
- stays available from the Windows notification area.

Today's live token value uses the account bucket when available. Because the account API can publish daily buckets several days late, the companion falls back to summing today's local Codex session increments instead of incorrectly displaying zero.

The plugin uses native Codex `SessionStart` and `UserPromptSubmit` hooks to launch or recover the companion when Codex opens, resumes a task, or receives the first prompt. Hook commands resolve `scripts\start-desktop.ps1` from Codex's portable `${PLUGIN_ROOT}` variable, so no user profile or installation path is hard-coded. When the Codex host process closes, the companion exits after a five-second grace period. Lifecycle diagnostics are written to `%LocalAppData%\CodexTokenQuest\lifecycle.log`. It does not use the Windows login registry, Task Scheduler, a skill, or an MCP server.

For compatibility, `scripts\install-autostart.ps1` now removes legacy login/task startup entries and enables the current hook-driven launch for the active session.

## Verify

```powershell
dotnet build .\CodexTokenQuest.slnx --configuration Release
dotnet run --project .\tests\CodexTokenQuest.Tests\CodexTokenQuest.Tests.csproj --configuration Release
```

## Data source

The app uses the official Codex App Server JSONL protocol:

- `account/rateLimits/read`
- `account/usage/read`

The plugin is intentionally read-only and never calls the reset-consumption endpoint.
