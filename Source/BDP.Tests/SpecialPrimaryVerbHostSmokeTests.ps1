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

# 测试脚本位于 Source\BDP.Tests，向上定位模组工程根目录。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot

$formalSlotPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\BdpFormalVerbHostSlot.cs'
$expressionSlotPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionVerbHostSlot.cs'
$formalHostsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.FormalHosts.cs'
$sourceCollectorPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionSourceCollector.cs'
$singleSideBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\SingleSideExpressionBuilder.cs'
$primarySelectorPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultPrimaryExpressionSelector.cs'
$hostManagerPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\TriggerBodyVerbHostManager.cs'
$snapshotBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionSnapshotBuilder.cs'

$paths = @(
    $formalSlotPath,
    $expressionSlotPath,
    $formalHostsPath,
    $sourceCollectorPath,
    $singleSideBuilderPath,
    $primarySelectorPath,
    $hostManagerPath,
    $snapshotBuilderPath
)

foreach ($path in $paths) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) ('Required SpecialPrimary source file is missing: ' + $path)
}

$formalSlotText = Get-Content -LiteralPath $formalSlotPath -Raw -Encoding UTF8
$expressionSlotText = Get-Content -LiteralPath $expressionSlotPath -Raw -Encoding UTF8
$formalHostsText = Get-Content -LiteralPath $formalHostsPath -Raw -Encoding UTF8
$sourceCollectorText = Get-Content -LiteralPath $sourceCollectorPath -Raw -Encoding UTF8
$singleSideBuilderText = Get-Content -LiteralPath $singleSideBuilderPath -Raw -Encoding UTF8
$primarySelectorText = Get-Content -LiteralPath $primarySelectorPath -Raw -Encoding UTF8
$hostManagerText = Get-Content -LiteralPath $hostManagerPath -Raw -Encoding UTF8
$snapshotBuilderText = Get-Content -LiteralPath $snapshotBuilderPath -Raw -Encoding UTF8

# 固定宿主契约：SpecialPrimary 只能追加，不能引入 SpecialSecondary。
Assert-True ($formalSlotText -match '\bSpecialPrimary\b') `
    'BdpFormalVerbHostSlot must define SpecialPrimary.'
Assert-True ($expressionSlotText -match 'SpecialPrimaryVerb\s*=\s*9') `
    'ExpressionVerbHostSlot must append SpecialPrimaryVerb with value 9.'
Assert-True ($formalHostsText -match 'ComboSecondary[\s\S]*SpecialPrimary') `
    'CompTriggerBody.FormalHostSlots must append SpecialPrimary after ComboSecondary.'
Assert-True ($formalSlotText -notmatch 'SpecialSecondary') `
    'BdpFormalVerbHostSlot must not define SpecialSecondary.'
Assert-True ($expressionSlotText -notmatch 'SpecialSecondary') `
    'ExpressionVerbHostSlot must not define SpecialSecondary.'
Assert-True ($formalHostsText -notmatch 'SpecialSecondary') `
    'CompTriggerBody.FormalHostSlots must not contain SpecialSecondary.'

# 表达来源与执行键：特殊槽只接受 Primary，禁止生成模糊的 Secondary 键。
Assert-True ($sourceCollectorText -match 'case TriggerSide\.Special:[\s\S]*SpecialPrimaryVerb') `
    'ExpressionSourceCollector must map Special Primary Verb to SpecialPrimaryVerb.'
Assert-True ($sourceCollectorText -match 'case TriggerSide\.Special:[\s\S]*VerbAttackRole\.Primary[\s\S]*ExpressionVerbHostSlot\.None') `
    'ExpressionSourceCollector must reject Special Secondary as a formal host.'
Assert-True ($singleSideBuilderText -match 'material\.Side == TriggerSide\.Special[\s\S]*SpecialPrimary') `
    'SingleSideExpressionBuilder must use the stable SpecialPrimary execution key.'
Assert-True ($singleSideBuilderText -notmatch 'TriggerSide\.Special[\s\S]*return material\.VerbAttackRole == VerbAttackRole\.Secondary \? "Secondary" : "Primary"') `
    'SingleSideExpressionBuilder must not keep the ambiguous Special Primary/Secondary fallback key.'

# 默认主攻击选择：特殊槽武器必须进入远程和近战的同一主攻击选择逻辑。
Assert-True ($primarySelectorText -match 'ExpressionOriginKind\.Special') `
    'DefaultPrimaryExpressionSelector must accept Special as a single-side primary source.'

# 投影绑定：结果标识必须进入唯一的 SpecialPrimary 正式宿主。
Assert-True ($hostManagerText -match 'result\.OriginKind == ExpressionOriginKind\.Special[\s\S]*result\.VerbAttackRole == VerbAttackRole\.Primary[\s\S]*BdpFormalVerbHostSlot\.SpecialPrimary') `
    'TriggerBodyVerbHostManager must bind Special Primary results to SpecialPrimary.'

# 既有业务规则不得被本次宿主修复移除：特殊槽仍只保留一条武器结果。
Assert-True ($snapshotBuilderText -match 'SpecialSet = ReplaceWeaponResults\(specialSet, KeepSingleWeaponResult\(specialSet\)\)') `
    'ExpressionSnapshotBuilder must retain the existing single Special weapon override rule.'

Write-Output 'SpecialPrimaryVerbHostSmokeTests PASS'
