# 芯片信息模型消费者边界冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$accessPath = Join-Path $modRoot "Source\BDP\Core\Information\Access\ChipInformationSurfaceAccess.cs"
$readerPath = Join-Path $modRoot "Source\BDP\Core\Information\Access\IChipInformationReader.cs"
$modelRoot = Join-Path $modRoot "Source\BDP\Core\Information"

Assert-True (Test-Path -LiteralPath $accessPath) "缺少统一芯片信息读取面。"
Assert-True (Test-Path -LiteralPath $readerPath) "缺少芯片信息读取契约。"

$accessText = Get-Utf8Text $accessPath
$readerText = Get-Utf8Text $readerPath
$modelText = Get-ChildItem -LiteralPath $modelRoot -Recurse -Filter *.cs | ForEach-Object { Get-Utf8Text $_.FullName }
$modelText = $modelText -join "`n"
Assert-True ($readerText -match "Read\(") "读取契约必须提供统一 Read（读取）入口。"
Assert-True ($accessText -match "ChipInformationModel") "读取面必须返回统一信息模型。"
Assert-True ($modelText -notmatch "Window_ChipManufacturing|Window_TriggerAssembly|Panel_ChipDetail") "Core 信息模型不得引用具体界面。"
Assert-True ($modelText -notmatch "BDP_Content|BDP\.Content") "Core 信息模型不得依赖 Content。"
Assert-True ($modelText -notmatch "defName\s*==|Label\s*==|ThingID\s*==") "信息模型不得按芯片身份选择结构。"

Write-Host "PASS: 信息模型向三个消费者提供统一只读读取边界。"
