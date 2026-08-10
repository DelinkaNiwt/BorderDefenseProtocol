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
$presentationRoot = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Presentation'
$directionPath = Join-Path $presentationRoot 'CombatBodyTransformDirection.cs'
$providerPath = Join-Path $presentationRoot 'ICombatBodyTransformPresentationProvider.cs'
$registryPath = Join-Path $presentationRoot 'CombatBodyTransformPresentationRegistry.cs'

Assert-True (Test-Path -LiteralPath $directionPath -PathType Leaf) '缺少战斗体变换表现方向契约。'
Assert-True (Test-Path -LiteralPath $providerPath -PathType Leaf) '缺少战斗体变换表现提供器契约。'
Assert-True (Test-Path -LiteralPath $registryPath -PathType Leaf) '缺少战斗体变换表现注册表。'

$directionText = Get-Content -LiteralPath $directionPath -Raw -Encoding utf8
$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$registryText = Get-Content -LiteralPath $registryPath -Raw -Encoding utf8

Assert-True ($directionText -match 'public\s+enum\s+CombatBodyTransformDirection') 'Core必须公开进入/离开方向契约。'
Assert-True ($directionText -match '\bEnter\b' -and $directionText -match '\bExit\b') '方向契约必须只表达进入与离开。'
Assert-True ($providerText -match 'public\s+interface\s+ICombatBodyTransformPresentationProvider') 'Core必须公开中性表现提供器接口。'
Assert-True ($providerText -match 'void\s+Begin\s*\(\s*Pawn\s+pawn\s*,\s*CombatBodyTransformDirection\s+direction\s*\)') '表现提供器必须只接收Pawn和变换方向。'
Assert-True ($registryText -match 'public\s+static\s+class\s+CombatBodyTransformPresentationRegistry') 'Core必须提供表现注册表。'
Assert-True ($registryText -match 'Register\s*\(' -and $registryText -match 'Unregister\s*\(' -and $registryText -match 'NotifyBegin\s*\(') '表现注册表必须支持注册、反注册与开始通知。'
Assert-True ($registryText -match 'try[\s\S]*provider\.Begin[\s\S]*catch\s*\(\s*Exception') '注册表必须隔离单个表现提供器异常。'
Assert-True ($registryText -notmatch 'BDP_Mote_|BDP\.Content|\bMote\b|\bFleck\b') 'Core表现注册表不得认识具体视觉实现。'

Write-Output 'CombatBodyTransformPresentationBoundary PASS'
