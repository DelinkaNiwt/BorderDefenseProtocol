# BDP 正式内容与可选开发内容的选择性迁移边界测试。
# 旧候选测试模组已经退役，本测试只约束主模组内部当前仍有效的物理边界。

$ErrorActionPreference = "Stop"

$mainModRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$contentRoot = Join-Path $mainModRoot "Source\BDP.Content"
$developmentRoot = Join-Path $mainModRoot "Source\BDP.Development"
$contentDiagnosticsRoot = Join-Path $contentRoot "Trigger\Diagnostics"
$developmentDiagnosticsRoot = Join-Path $developmentRoot "Trigger\Diagnostics"
$contentBootstrapPath = Join-Path $contentRoot "ContentBootstrap.cs"
$developmentBootstrapPath = Join-Path $developmentRoot "DevelopmentBootstrap.cs"
$loadFoldersPath = Join-Path $mainModRoot "LoadFolders.xml"

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

Assert-True (-not (Test-Path -LiteralPath $contentDiagnosticsRoot)) `
    "正式 Content 不得继续持有纯开发画点诊断。"

$requiredDevelopmentFiles = @(
    "MapComponent_TriggerVisualMarkerOverlay.cs",
    "TriggerVisualMarkerGizmoProvider.cs",
    "TriggerVisualMarkerOverlayDrawer.cs",
    "TriggerVisualMarkerSettings.cs"
)
foreach ($fileName in $requiredDevelopmentFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $developmentDiagnosticsRoot $fileName)) `
        "Development 缺少迁入的画点诊断文件：$fileName"
}

$contentBootstrapText = Get-Content -Raw -Encoding UTF8 -LiteralPath $contentBootstrapPath
$developmentBootstrapText = Get-Content -Raw -Encoding UTF8 -LiteralPath $developmentBootstrapPath

Assert-True ($contentBootstrapText -notmatch "TriggerVisualMarkerGizmoProvider|BDP\.Development") `
    "正式 Content 启动入口不得引用开发画点诊断或 Development。"
Assert-True (
    ($contentBootstrapText | Select-String -AllMatches "TriggerExternalGizmoRegistry\.Register").Matches.Count -eq 2 -and
    ($contentBootstrapText -match "new\s+ChipModeGizmoProvider\s*\(\s*\)") -and
    ($contentBootstrapText -match "new\s+ChipStanceGizmoProvider\s*\(\s*\)")
) "正式 Content 必须且只能注册正式芯片形态与姿态按钮提供器。"
Assert-True ($developmentBootstrapText -match "TriggerExternalGizmoRegistry\.Register\s*\(\s*new\s+TriggerVisualMarkerGizmoProvider") `
    "Development 启动入口必须接管画点诊断注册。"

[xml]$loadFolders = Get-Content -Raw -Encoding UTF8 -LiteralPath $loadFoldersPath
$loadedFolders = @($loadFolders.loadFolders.'v1.6'.li | ForEach-Object { [string]$_ })
$expectedFolders = @("/", "1.6", "1.6/Content", "1.6/Development")
Assert-True ($loadedFolders.Count -eq $expectedFolders.Count) "主模组加载目录数量错误。"
for ($index = 0; $index -lt $expectedFolders.Count; $index++)
{
    Assert-True ($loadedFolders[$index] -eq $expectedFolders[$index]) `
        "主模组必须最后加载独立 Development 目录。"
}

Write-Host "PASS: BDP 正式 Content 与可选 Development 内容符合当前选择性迁移边界。"
