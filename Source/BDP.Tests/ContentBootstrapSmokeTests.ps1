# BDP 内容程序集正式启动入口冒烟测试。
# 本测试锁定正式内容入口只做补丁扫描和已确认业务注册。

$ErrorActionPreference = "Stop"

$sourceRoot = Split-Path -Parent $PSScriptRoot
$contentRoot = Join-Path $sourceRoot "BDP.Content"
$formalBootstrapPath = Join-Path $contentRoot "ContentBootstrap.cs"
$legacyBootstrapPath = Join-Path $contentRoot "DevHarnessBootstrap.cs"

function Assert-True
{
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

Assert-True (Test-Path -LiteralPath $formalBootstrapPath) "缺少正式内容程序集启动入口 ContentBootstrap.cs。"
Assert-True (-not (Test-Path -LiteralPath $legacyBootstrapPath)) "旧 DevHarnessBootstrap.cs 仍然存在。"

$bootstrapText = Get-Content -LiteralPath $formalBootstrapPath -Raw -Encoding utf8

Assert-True ($bootstrapText -match "namespace\s+BDP\.Content") "正式启动入口必须属于 BDP.Content 命名空间。"
Assert-True ($bootstrapText -match "\[StaticConstructorOnStartup\]") "正式启动入口必须由 RimWorld 在装载时自动运行。"
Assert-True ($bootstrapText -match "public\s+static\s+class\s+ContentBootstrap") "正式启动类必须命名为 ContentBootstrap。"
Assert-True ($bootstrapText -match "static\s+ContentBootstrap\s*\(\s*\)") "正式启动类必须使用静态构造函数接线。"
Assert-True ($bootstrapText -match "new\s+Harmony\s*\(\s*""niwt\.bdp\.content""\s*\)\.PatchAll\s*\(\s*\)") "Harmony 标识必须使用正式内容语义。"
Assert-True (
    ($bootstrapText | Select-String -AllMatches "TriggerExternalGizmoRegistry\.Register").Matches.Count -eq 2 -and
    ($bootstrapText -match "TriggerExternalGizmoRegistry\.Register\s*\(\s*new\s+ChipModeGizmoProvider\s*\(\s*\)\s*\)") -and
    ($bootstrapText -match "TriggerExternalGizmoRegistry\.Register\s*\(\s*new\s+ChipStanceGizmoProvider\s*\(\s*\)\s*\)")
) "Content 必须且只能注册正式通用的芯片形态与姿态按钮提供器，不得注册开发画点诊断。"
Assert-True ($bootstrapText -notmatch "TriggerVisualMarkerGizmoProvider|BDP\.Content\.Trigger\.Diagnostics") `
    "Content 启动入口不得引用开发画点诊断。"
Assert-True (
    ($bootstrapText | Select-String -AllMatches "TrionGizmoExtensionRegistry\.RegisterPanel").Matches.Count -eq 1 -and
    ($bootstrapText -match "TrionGizmoExtensionRegistry\.RegisterPanel\s*\(\s*new\s+TriggerLoadoutPanelProvider\s*\(\s*\)\s*\)")
) "事项 05 正式面板必须且只能注册一次。"
Assert-True ($bootstrapText -notmatch "BDP\.DevHarness") "正式内容入口不得依赖候选测试业务。"
Assert-True ($bootstrapText -notmatch "Prefs\.DevMode|DefDatabase|TryGetComp|DamageDef") "启动入口不得承载诊断开关、定义查询、组件查询或伤害业务。"

Write-Host "PASS: BDP 内容程序集启动入口符合当前正式接线契约。"
