# 芯片激活音效生命周期与配置传播冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$coreRoot = Join-Path $modRoot "Source\BDP\Core"
$contentRoot = Join-Path $modRoot "Source\BDP.Content"
$presetPath = Join-Path $modRoot "1.6\Content\Defs\ChipActionPresetDef\Presets.xml"

$configPath = Join-Path $coreRoot "Chips\Config\ChipActivationAudioConfig.cs"
$contractPath = Join-Path $coreRoot "Chips\Contract\ChipActivationAudioContract.cs"
$loadoutConfigPath = Join-Path $coreRoot "Chips\Config\ChipLoadoutConfig.cs"
$loadoutContractPath = Join-Path $coreRoot "Chips\Contract\ChipLoadoutContract.cs"
$resolverPath = Join-Path $coreRoot "Chips\Contract\ChipDefinitionContractResolver.cs"
$readerPath = Join-Path $coreRoot "Chips\Access\ChipDefinitionReaderSurface.cs"
$mergePath = Join-Path $contentRoot "Assembly\ChipManufacturing\Resolution\ChipConfigurationMergeService.cs"
$controllerPath = Join-Path $coreRoot "Trigger\Runtime\TriggerActivationAudioController.cs"
$bodyAudioPath = Join-Path $coreRoot "Trigger\State\CompTriggerBody.ActivationAudio.cs"
$coordinatorPath = Join-Path $coreRoot "Trigger\Runtime\TriggerRuntimeCoordinator.cs"
$integrityPath = Join-Path $coreRoot "Trigger\State\CompTriggerBody.Integrity.cs"
$contextPath = Join-Path $coreRoot "Trigger\State\CompTriggerBody.Contexts.cs"

foreach ($path in @(
    $configPath,
    $contractPath,
    $controllerPath,
    $bodyAudioPath
))
{
    Assert-True (Test-Path -LiteralPath $path) "缺少激活音效设施文件：$path"
}

$configText = Get-Utf8Text $configPath
$contractText = Get-Utf8Text $contractPath
$loadoutConfigText = Get-Utf8Text $loadoutConfigPath
$loadoutContractText = Get-Utf8Text $loadoutContractPath
$resolverText = Get-Utf8Text $resolverPath
$readerText = Get-Utf8Text $readerPath
$mergeText = Get-Utf8Text $mergePath
$controllerText = Get-Utf8Text $controllerPath
$bodyAudioText = Get-Utf8Text $bodyAudioPath
$coordinatorText = Get-Utf8Text $coordinatorPath
$integrityText = Get-Utf8Text $integrityPath
$contextText = Get-Utf8Text $contextPath
$presetText = Get-Utf8Text $presetPath

Assert-True ($loadoutConfigText -match 'ChipActivationAudioConfig\s+ActivationAudio') '装载配置必须承载可选激活音效块。'
Assert-True ($loadoutContractText -match 'ChipActivationAudioContract\s+ActivationAudio') '装载契约必须承载激活音效结果。'
Assert-True ($configText -match 'SoundDef\s+ActivationWarmupStartSound') '激活音效配置必须声明开始音效。'
Assert-True ($configText -match 'SoundDef\s+ActivationWarmupLoopSound') '激活音效配置必须声明持续音效。'
Assert-True ($configText -match 'SoundDef\s+ActivationWarmupEndSound') '激活音效配置必须声明完成音效。'
Assert-True ($contractText -match 'SoundDef\s+WarmupStartSound') '激活音效契约必须声明开始音效。'
Assert-True ($contractText -match 'SoundDef\s+WarmupLoopSound') '激活音效契约必须声明持续音效。'
Assert-True ($contractText -match 'SoundDef\s+WarmupEndSound') '激活音效契约必须声明完成音效。'

Assert-True ($resolverText -match 'TranslateActivationAudio') '静态 Def 契约解释器必须传播激活音效。'
Assert-True ($readerText -match 'ActivationAudio') '动态成品契约读取必须传播激活音效。'
Assert-True ($mergeText -match 'ActivationAudio') '制造复制/合并必须保留激活音效声明。'

Assert-True ($controllerText -match 'SwitchPhase\.Activating') '音效控制器只能在 Activating（启用前摇）阶段工作。'
Assert-True ($controllerText -match 'SwitchPhase\.WaitingForConflicts') '音效控制器必须识别等待互斥冲突阶段。'
Assert-True ($controllerText -match 'TrySpawnSustainer') '持续音效必须使用 Sustainer（持续音效维持器）。'
Assert-True ($controllerText -match '\.Maintain\(\)') '持续音效每次运行时推进都必须 Maintain（维持）。'
Assert-True ($controllerText -match '\.End\(\)') '取消或生命周期结束时必须结束持续音效。'
Assert-True ($controllerText -match 'PlayOneShot') '开始和完成音效必须使用一次性播放。'
Assert-True ($controllerText -match 'Clear|Stop') '音效控制器必须提供统一清理路径。'

Assert-True ($bodyAudioText -match 'SyncActivationAudioForRuntimeTick') 'TriggerBody 必须提供运行时音效同步入口。'
Assert-True ($bodyAudioText -match 'NotifyActivationAudioCommitted') 'TriggerBody 必须在正式激活提交时通知音效控制器。'
Assert-True ($bodyAudioText -match 'ClearActivationAudio') 'TriggerBody 必须提供脱离装备清理入口。'
Assert-True ($coordinatorText -match 'SyncActivationAudioForRuntimeTick') '运行时协调器必须推进激活音效。'
Assert-True ($integrityText -match 'NotifyActivationAudioCommitted') '正式激活提交路径必须触发完成音效。'
Assert-True ($contextText -match 'ActivationAudio') 'Trigger 上下文或运行时读取链必须能取得音效声明。'

Assert-True ($presetText -match '<ActivationWarmupStartSound>Power_OnSmall</ActivationWarmupStartSound>') '变色龙必须配置不依赖 DLC 的开始音效。'
Assert-True ($presetText -match '<ActivationWarmupLoopSound>GasReleasing</ActivationWarmupLoopSound>') '变色龙必须配置不依赖 DLC 的持续音效。'
Assert-True ($presetText -match '<ActivationWarmupEndSound>Power_OffSmall</ActivationWarmupEndSound>') '变色龙必须配置不依赖 DLC 的完成音效。'
Assert-True ($controllerText -notmatch 'Psycast|Royalty') 'BDP Core 激活音效设施不得依赖皇权灵能实现。'

Write-Host 'PASS: 芯片激活音效配置传播与生命周期边界已固定。'
