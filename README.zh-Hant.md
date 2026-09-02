# Codex Token Quest

[English](README.md) | 繁體中文

一款適用於 Codex Desktop、唯讀的原生 C# 像素 RPG HUD。在 Windows 上，它會以不可啟用的工具視窗執行，跟隨 ChatGPT／Codex 主視窗並自動重新整理。

- 顯示每個可用額度視窗及已使用／剩餘百分比；
- 以本地時間顯示下一次重設時間；
- 帳號提供資料時，顯示累積與近期每日 Token 活動；
- 顯示可用的已取得重設次數。

此伴隨程式不會向 Codex 應用程式注入程式碼，也不會修改 Codex。它只會觀察最上層的 ChatGPT／Codex 主視窗，並固定在其右下角內側。

本專案不會讀取或儲存 ChatGPT Access Token 或 API Key。

## 系統需求

- .NET 10 SDK 或更新版本
- 可透過 `codex` 命令使用 Codex CLI
- Codex CLI 已登入 ChatGPT 帳號

僅使用 API Key 或 Amazon Bedrock 的驗證方式不會提供 ChatGPT Token 活動摘要。

## 方案結構

- `CodexTokenQuest.Core`：包含 Codex App Server 用戶端、用量模型、解析器與本地 Token 讀取器的類別庫。
- `CodexTokenQuest.Desktop`：沒有主控台視窗的原生 WinForms RPG HUD（`WinExe`）。
- `CodexTokenQuest.Tests`：解析器與本地 Token 的迴歸測試。

## 執行

```powershell
.\scripts\start-desktop.ps1
```

## Codex Desktop

此伴隨程式是獨立的 Windows UI，不會安裝 Codex Skill 或 MCP Server。視窗功能如下：

- 只有桌面主程式明確處於 Codex 模式時，才會自動顯示在右下角；
- 主程式關閉、最小化、切換到 ChatGPT 模式或被其他應用程式遮住時會隱藏，Codex 模式再次回到前景後便會恢復；
- Codex 移動或調整大小時會跟著重新定位；
- 提供像素風 RPG 營地，顯示英雄肖像、等級、經驗值、今日任務經驗與每週耐力；
- 將累積 Token 轉換為 1 到 99 級的指數式 RPG 經驗等級，並可設定 `1K`～`1T` 的經驗值基數來調整升級速度；
- 提供四名可選擇的像素英雄，分別於 1、10、25、50 級解鎖，並記住所選英雄；
- 將 HUD 分成可記憶的 `CAMP`、`QUESTS`、`HISTORY` 面板，分別呈現角色進度、耐力模組與七日任務經驗圖表；
- 提供 Pixel Dungeon、Arcane Glass、Guild Ledger、Code Terminal 四種可持續保存的視覺主題，可透過樣式控制切換；
- 支援可持續保存的英文與繁體中文視窗文字，可從遊戲選項切換；
- 提供可持續保存的 HUD 透明度設定，可在 20% 到 100% 之間調整；
- 預設每 5 分鐘重新整理帳號用量，每秒更新重設倒數；
- 可從通知區選單或視窗底部的更新文字，設定並保存 1–1440 分鐘的重新整理間隔；
- 可持續從 Windows 通知區存取。

今日即時 Token 數值會優先使用帳號資料。由於帳號 API 的每日資料可能延遲數日才發佈，伴隨程式會改為加總今日的本地 Codex 工作階段增量，避免錯誤顯示為零。

此外掛使用 Codex 原生的 `SessionStart` 與 `UserPromptSubmit` Hook，在 Codex 開啟、恢復工作或收到第一個提示時啟動或復原伴隨程式。Hook 命令會透過 Codex 可攜式 `${PLUGIN_ROOT}` 變數解析 `scripts\start-desktop.ps1`，因此不會寫死使用者目錄或安裝路徑。Codex 主程序關閉後，伴隨程式會在五秒寬限時間後結束。生命週期診斷會寫入 `%LocalAppData%\CodexTokenQuest\lifecycle.log`。本程式不使用 Windows 登入 Registry、工作排程器、Skill 或 MCP Server。

為了維持相容性，`scripts\install-autostart.ps1` 現在會移除舊版登入／工作排程啟動項目，並為目前工作階段啟用現行的 Hook 啟動方式。

## 驗證

```powershell
dotnet build .\CodexTokenQuest.slnx --configuration Release
dotnet run --project .\tests\CodexTokenQuest.Tests\CodexTokenQuest.Tests.csproj --configuration Release
```

## 資料來源

應用程式使用官方 Codex App Server JSONL 通訊協定：

- `account/rateLimits/read`
- `account/usage/read`

此外掛刻意維持唯讀，絕不呼叫使用重設次數的端點。
