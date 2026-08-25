$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$contentRoot = Join-Path $sourceRoot 'BDP.Content'
$providerPath = Join-Path $contentRoot 'Trigger\UI\ChipStances\ChipStanceGizmoProvider.cs'
$commandPath = Join-Path $contentRoot 'Trigger\UI\ChipStances\Command_ChipStance.cs'
$bootstrapPath = Join-Path $contentRoot 'ContentBootstrap.cs'
$languagePath = Join-Path (Split-Path -Parent $sourceRoot) 'Languages\ChineseSimplified (简体中文)\Keyed\Commands.xml'

foreach ($path in @($providerPath, $commandPath, $bootstrapPath, $languagePath)) {
    Assert-True (Test-Path -LiteralPath $path) ('姿态按钮设施缺少文件：' + $path)
}

$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding UTF8
$commandText = Get-Content -LiteralPath $commandPath -Raw -Encoding UTF8
$bootstrapText = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding UTF8
$languageText = Get-Content -LiteralPath $languagePath -Raw -Encoding UTF8

Assert-True (
    ($providerText -match 'sealed\s+class\s+ChipStanceGizmoProvider\s*:\s*ITriggerExternalGizmoProvider') -and
    ($providerText -match 'GetChipStanceOptions') -and
    ($providerText -match 'stanceOptions\.Count\s*<=\s*1') -and
    ($providerText -match 'RequestCycleChipStance') -and
    ($providerText -match 'RequestSwitchChipStance')
) '通用姿态按钮必须只对当前形态的多个姿态工作，并连接左键轮换和右键直切。'

Assert-True (
    ($commandText -match 'sealed\s+class\s+Command_ChipStance\s*:\s*Command_Action') -and
    ($commandText -match 'RightClickFloatMenuOptions')
) '姿态命令只在原版动作按钮上增加动态右键菜单。'

Assert-True ($providerText -match 'groupable\s*=\s*false') `
    '每枚芯片的姿态按钮必须关闭原版聚合，避免一次输入同时切换多个实例。'

$registrationCount = [regex]::Matches(
    $bootstrapText,
    'TriggerExternalGizmoRegistry\.Register\(new\s+ChipStanceGizmoProvider\(\)\)').Count
Assert-True ($registrationCount -eq 1) 'ContentBootstrap 必须且只能注册一次通用姿态按钮提供器。'

Assert-True (
    ($languageText -match '<BDP_Command_ChipStance_Switch>') -and
    ($languageText -match '<BDP_Command_ChipStance_Desc>') -and
    ($languageText -match '<BDP_Command_ChipStance_CurrentOption>')
) '所有新增姿态按钮文本必须来自简体中文语言包。'

Write-Output 'ChipStanceGizmoContentSmokeTests PASS'
