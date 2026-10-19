$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$patchPath = Join-Path $sourceRoot 'BDP.Content\FloatingFlight\Patches\Patch_PawnPathFollower_Steps_FloatingFlight.cs'
Assert-True (Test-Path -LiteralPath $patchPath -PathType Leaf) "缺少浮空路径跟随补丁：$patchPath"
$patchText = Get-Content -LiteralPath $patchPath -Raw -Encoding utf8

# 兼容性契约：必须精确定位原版通行方法，而不是按方法名模糊替换。
Assert-True ($patchText -match 'AccessTools\.Method\(\s*typeof\(GenGrid\)') '必须通过原版 GenGrid 类型精确定位通行调用。'
Assert-True ($patchText -match 'Walkable.*By') '必须保留对 WalkableBy 原版锚点的精确识别。'

# 兼容性契约：浮空只插入旁路，不能把原版调用的 opcode/operand 换成 BDP 方法。
Assert-True ($patchText -match 'Insert.*Bypass|Insert.*旁路|CanBypassWalkableCheck') '必须提供浮空旁路插入逻辑。'
Assert-True (-not ($patchText -match 'codes\[index\]\.opcode\s*=\s*OpCodes\.Call')) '不得直接改写原版通行调用的 opcode。'
Assert-True (-not ($patchText -match 'codes\[index\]\.operand\s*=\s*replacement')) '不得把原版通行调用 operand 替换成 BDP 方法。'

# 锚点被未知模组改写时必须安全降级，不能以 Error 终止补丁链。
Assert-True (-not ($patchText -match '未找到逐格通行调用')) '缺失锚点时不得继续使用旧的 Error 失败路径。'
Assert-True ($patchText -match 'Log\.Warning') '缺失或不兼容结构必须使用可搜索的兼容性警告。'

Write-Output 'FloatingFlightPathCompatibilitySmokeTests PASS'
