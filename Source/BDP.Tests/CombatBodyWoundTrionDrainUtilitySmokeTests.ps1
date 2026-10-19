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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot

$utilityPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionDrainUtility.cs'
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'

Assert-True (Test-Path -LiteralPath $utilityPath) 'CombatBodyWoundTrionDrainUtility.cs must exist.'
Assert-True (Test-Path -LiteralPath $bindingPath) 'CombatBodyWoundTrionBinding.cs must exist.'

$utilityText = Get-Content -LiteralPath $utilityPath -Raw -Encoding utf8
$bindingText = Get-Content -LiteralPath $bindingPath -Raw -Encoding utf8

Assert-True ($utilityText -match 'internal\s+static\s+class\s+CombatBodyWoundTrionDrainUtility') 'Wound drain query must live in a small internal static utility.'
Assert-True ($utilityText -match 'TryResolveWoundDrainPerSecond\s*\(\s*Hediff\s+wound,\s*float\s+effectiveDamage,\s*out\s+float\s+drainPerSecond\s*\)') '计算入口必须接收最终伤口及本次实际新增损伤。'
Assert-True ($utilityText -match 'GetMaxHealth\(wound.pawn\)' -and $utilityText -match 'Math.Min\(maximum') '过量攻击必须受原版部位上限限制。'
Assert-True ($utilityText -match 'HasMissingAncestor') '缺失祖先下的递归记录不能再计费。'
Assert-True ($utilityText -match 'CombatBodyWoundPolicy\.IsCombatBodyWoundRuntimeApplicable\(wound.pawn\)') '伤口运行时继续适用于激活及崩溃阶段。'
Assert-True ($bindingText -match 'CombatBodyWoundTrionDrainUtility\.TryResolveWoundDrainPerSecond') '独立伤口登记必须使用共享计算入口。'
Assert-True ($bindingText -notmatch 'private\s+static\s+float\s+ResolveDrainPerSecond') 'Wound drain binding must not keep a duplicate private drain calculation.'

Write-Output 'CombatBodyWoundTrionDrainUtilitySmokeTests PASS'
