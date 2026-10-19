$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Utf8 {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

function Assert-NearlyEqual {
    param(
        [double]$Actual,
        [double]$Expected,
        [string]$Message
    )

    if ([Math]::Abs($Actual - $Expected) -gt 0.0001) {
        throw "$Message：实际值 $Actual，期望值 $Expected"
    }
}

function Split-TrionPool {
    param(
        [double]$CombinedCurrent,
        [double]$PawnMax,
        [double]$TriggerMax
    )

    $pawnCurrent = [Math]::Min($CombinedCurrent, $PawnMax)
    $triggerCurrent = [Math]::Min(
        [Math]::Max($CombinedCurrent - $PawnMax, 0),
        $TriggerMax)
    $lostCurrent = [Math]::Max($CombinedCurrent - $PawnMax - $TriggerMax, 0)
    return @($pawnCurrent, $triggerCurrent, $lostCurrent)
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'

$capacityContributorPath = Join-Path $coreRoot 'Core\Trion\Capacity\ITrionCapacityContributor.cs'
$capacityRegistryPath = Join-Path $coreRoot 'Core\Trion\Capacity\TrionCapacityContributorRegistry.cs'
$capacityStatPartPath = Join-Path $coreRoot 'Core\Genes\StatPart_TrionCapacityContributors.cs'
$capacityBindingPath = Join-Path $coreRoot 'Core\Trigger\Runtime\ITriggerTrionCapacityBinding.cs'
$capacityBindingRegistryPath = Join-Path $coreRoot 'Core\Trigger\Runtime\TriggerTrionCapacityBindingRegistry.cs'
$compTrionPath = Join-Path $coreRoot 'Core\Trion\CompTrion.cs'
$trionReaderPath = Join-Path $coreRoot 'Core\Trion\ITrionReader.cs'
$trionCommandsPath = Join-Path $coreRoot 'Core\Trion\ITrionCommands.cs'
$trionServicePath = Join-Path $coreRoot 'Core\Trion\TrionService.cs'
$triggerBodyPath = Join-Path $coreRoot 'Core\Trigger\State\CompTriggerBody.cs'
$triggerLifecyclePath = Join-Path $coreRoot 'Core\Trigger\State\CompTriggerBody.Lifecycle.cs'
$genePath = Join-Path $coreRoot 'Core\Genes\Gene_TrionGland.cs'
$combatCommandsPath = Join-Path $coreRoot 'Core\CombatBody\Access\Contracts\ICombatBodyCommands.cs'
$activationTransactionPath = Join-Path $coreRoot 'Core\CombatBodySession\CombatBodyActivationTransaction.cs'
$contentBindingPath = Join-Path $contentRoot 'Trigger\Black\BlackTriggerTrionCapacityBinding.cs'
$contentCalculatorPath = Join-Path $contentRoot 'Trigger\Black\BlackTriggerTrionCapacityCalculator.cs'
$bootstrapPath = Join-Path $contentRoot 'ContentBootstrap.cs'
$trionDefPath = Join-Path $repoRoot '1.6\Defs\StatDef\Trion.xml'
$triggerDefPath = Join-Path $repoRoot '1.6\Content\Defs\ThingDef\Equipment\Trigger.xml'
$messagesPath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Messages.xml'

foreach ($path in @(
        $capacityContributorPath,
        $capacityRegistryPath,
        $capacityStatPartPath,
        $capacityBindingPath,
        $capacityBindingRegistryPath,
        $contentBindingPath,
        $contentCalculatorPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少黑触发器 Trion 容量实现文件：$path"
}

$capacityContributorText = Read-Utf8 $capacityContributorPath
$capacityRegistryText = Read-Utf8 $capacityRegistryPath
$capacityStatPartText = Read-Utf8 $capacityStatPartPath
$capacityBindingText = Read-Utf8 $capacityBindingPath
$capacityBindingRegistryText = Read-Utf8 $capacityBindingRegistryPath
$compTrionText = Read-Utf8 $compTrionPath
$trionReaderText = Read-Utf8 $trionReaderPath
$trionCommandsText = Read-Utf8 $trionCommandsPath
$trionServiceText = Read-Utf8 $trionServicePath
$triggerBodyText = Read-Utf8 $triggerBodyPath
$triggerLifecycleText = Read-Utf8 $triggerLifecyclePath
$geneText = Read-Utf8 $genePath
$combatCommandsText = Read-Utf8 $combatCommandsPath
$activationTransactionText = Read-Utf8 $activationTransactionPath
$contentBindingText = Read-Utf8 $contentBindingPath
$contentCalculatorText = Read-Utf8 $contentCalculatorPath
$bootstrapText = Read-Utf8 $bootstrapPath
$trionDefText = Read-Utf8 $trionDefPath
$messagesText = Read-Utf8 $messagesPath
$triggerDefText = Read-Utf8 $triggerDefPath

Assert-True ($capacityContributorText -match 'interface\s+ITrionCapacityContributor') 'Core 必须提供中性的 Trion 容量贡献接口。'
Assert-True ($capacityContributorText -match 'GetAdditionalCapacity\(Pawn pawn\)') '容量贡献接口必须按 Pawn 返回额外容量。'
Assert-True ($capacityRegistryText -match 'class\s+TrionCapacityContributorRegistry') 'Core 必须提供 Trion 容量贡献注册表。'
Assert-True ($capacityRegistryText -match 'ResolveAdditionalCapacity\(Pawn pawn\)') '容量注册表必须提供统一读取入口。'
Assert-True ($capacityStatPartText -match 'TrionCapacityContributorRegistry\.ResolveAdditionalCapacity') '容量 StatPart 必须读取容量贡献注册表。'
Assert-True ($capacityBindingText -match 'interface\s+ITriggerTrionCapacityBinding') 'Core 必须提供触发体容量绑定接口。'
Assert-True ($capacityBindingText -match 'NotifyEligibilityLost') '绑定接口必须覆盖腺体资格失效。'
Assert-True ($capacityBindingRegistryText -match 'class\s+TriggerTrionCapacityBindingRegistry') 'Core 必须提供触发体容量绑定注册表。'
Assert-True ($trionReaderText -match 'IsBoundToExternalPool') 'Trion 读取面必须暴露外部合并绑定状态。'
Assert-True ($trionCommandsText -match 'SetBoundToExternalPool\(bool') 'Trion 请求面必须允许设置外部合并绑定状态。'
Assert-True ($compTrionText -match 'boundToExternalPool') 'CompTrion 必须持久化外部合并绑定标记。'
Assert-True ($triggerBodyText -match 'TriggerTrionCapacityBindingRegistry') 'CompTriggerBody 必须接入容量绑定注册表。'
Assert-True ($triggerLifecycleText -match 'TriggerCategoryDef') 'CompTriggerBody 必须暴露中性类别读取口。'
Assert-True ($geneText -match 'HandleGlandEligibilityLost|HandleTrionGlandLost|NotifyEligibilityLost') '腺体失效必须先通知触发体容量绑定。'
Assert-True ($combatCommandsText -match 'ForceReleaseForEligibilityLoss') '战斗体命令面必须提供资格失效强制解除。'
Assert-True ($activationTransactionText -match 'HasActiveTrionGland') '战斗体激活事务必须再次检查腺体资格。'
Assert-True ($contentBindingText -match 'BDP_TriggerCategory_Black') 'Content 黑绑定必须识别黑触发器类别。'
Assert-True ($contentBindingText -match 'TrionSurfaceAccess\.ResolveReader') '黑绑定必须从 Trion 正式读取面读取容量。'
Assert-True ($contentCalculatorText -match 'triggerCurrent|lostCurrent') '黑绑定计算器必须显式返回触发器余量和丢失量。'
Assert-True ($bootstrapText -match 'TrionCapacityContributorRegistry\.Register') 'Content 必须注册 Trion 容量贡献器。'
Assert-True ($bootstrapText -match 'TriggerTrionCapacityBindingRegistry\.Register') 'Content 必须注册触发体容量绑定器。'
Assert-True ($trionDefText -match 'StatPart_TrionCapacityContributors') 'Trion 容量 StatDef 必须接入容量贡献 StatPart。'
Assert-True ($messagesText -match 'BDP_Message_Trigger_RequiresTrionGland') '语言包必须提供无腺体装备拒绝文案。'
$thingDefBlocks = [regex]::Matches($triggerDefText, '<ThingDef[\s\S]*?</ThingDef>')
$blackThingDefCount = 0
foreach ($thingDefBlock in $thingDefBlocks) {
    $blockText = $thingDefBlock.Value
    if ($blockText -match '<triggerCategory>BDP_TriggerCategory_Black</triggerCategory>') {
        $blackThingDefCount++
        Assert-True ($blockText -match '<li Class="BDP.Core.Trion.CompProperties_Trion">[\s\S]*?<baseMax>[^<]+</baseMax>[\s\S]*?<startPercent>1</startPercent>[\s\S]*?<recoveryPerDay>0</recoveryPerDay>') '黑触发器 Def 必须声明容量、满值初始化和零独立恢复。'
    }
    elseif ($blockText -match '<triggerCategory>BDP_TriggerCategory_(Border|Neighbor)</triggerCategory>') {
        Assert-True ($blockText -notmatch 'BDP.Core.Trion.CompProperties_Trion') 'Border/Neighbor 触发体不得意外声明黑触发器容量组件。'
    }
}
Assert-True ($blackThingDefCount -eq 2) '当前应有且仅有两个黑触发器测试 Def（风刃与卵之冠）。'
Assert-True ($triggerDefText -match '<defName>BDP_TriggerBody_Fujin</defName>[\s\S]*?<triggerCategory>BDP_TriggerCategory_Black</triggerCategory>[\s\S]*?<baseMax>1000</baseMax>') '风刃触发体改为黑触发器后，容量上限必须为 1000。'
Assert-True ($triggerDefText -match '<defName>BDP_TriggerBody_Alektor</defName>[\s\S]*?<triggerCategory>BDP_TriggerCategory_Black</triggerCategory>[\s\S]*?<baseMax>1000</baseMax>') '卵之冠触发体改为黑触发器后，容量上限必须为 1000。'

$splitA = Split-TrionPool -CombinedCurrent 100 -PawnMax 100 -TriggerMax 50
Assert-NearlyEqual $splitA[0] 100 '合并值 100 时 Pawn 当前值错误。'
Assert-NearlyEqual $splitA[1] 0 '合并值 100 时黑触发器当前值错误。'
Assert-NearlyEqual $splitA[2] 0 '合并值 100 时丢失值错误。'

$splitB = Split-TrionPool -CombinedCurrent 140 -PawnMax 100 -TriggerMax 50
Assert-NearlyEqual $splitB[0] 100 '合并值 140 时 Pawn 当前值错误。'
Assert-NearlyEqual $splitB[1] 40 '合并值 140 时黑触发器当前值错误。'
Assert-NearlyEqual $splitB[2] 0 '合并值 140 时丢失值错误。'

$splitC = Split-TrionPool -CombinedCurrent 120 -PawnMax 0 -TriggerMax 50
Assert-NearlyEqual $splitC[0] 0 '失去腺体后自身上限为 0 时 Pawn 当前值错误。'
Assert-NearlyEqual $splitC[1] 50 '失去腺体后黑触发器保存值错误。'
Assert-NearlyEqual $splitC[2] 70 '失去腺体后超出总容量的丢失值错误。'

Write-Output 'BlackTriggerTrionCapacitySmokeTests PASS'
