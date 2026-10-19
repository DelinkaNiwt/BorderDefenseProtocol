$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Source {
    param([string]$RelativePath)

    $sourceRoot = Split-Path -Parent $PSScriptRoot
    $path = Join-Path $sourceRoot $RelativePath
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "缺少 Core 外观契约文件：$RelativePath"
    return Get-Content -LiteralPath $path -Raw -Encoding utf8
}

$entry = Read-Source 'BDP\Core\CombatBody\External\CombatBodyFrontAppearanceEntry.cs'
$plan = Read-Source 'BDP\Core\CombatBody\External\CombatBodyFrontAppearancePlan.cs'
$provider = Read-Source 'BDP\Core\CombatBody\External\ICombatBodyFrontAppearanceProvider.cs'
$registry = Read-Source 'BDP\Core\CombatBody\External\CombatBodyFrontAppearanceRegistry.cs'

Assert-True ($entry -match 'namespace\s+BDP\.Core\.CombatBody\.External') '外观条目必须属于 Core 外观扩展命名空间。'
Assert-True ($entry -match 'class\s+CombatBodyFrontAppearanceEntry') '必须存在 Core 外观条目类型。'
Assert-True ($entry -match 'ThingDef') '外观条目必须保存服装和材料 Def。'
Assert-True ($entry -match 'Color') '外观条目必须保存颜色值。'
Assert-True ($entry -match 'HasColor') '外观条目必须保存颜色有效标记。'
Assert-True ($entry -match 'StyleDef|ThingStyleDef') '外观条目必须保存风格 Def。'

Assert-True ($plan -match 'class\s+CombatBodyFrontAppearancePlan') '必须存在 Core 外观解析计划。'
Assert-True ($plan -match 'CombatBodyFrontMode') '外观解析计划必须保存前台模式。'
Assert-True ($plan -match 'Entries|Apparel') '外观解析计划必须保存服装条目集合。'
Assert-True ($plan -match 'Dynamic|Resolved|Valid') '外观解析计划必须能区分动态解析结果。'

Assert-True ($provider -match 'interface\s+ICombatBodyFrontAppearanceProvider') '必须存在 Core 外观提供器接口。'
Assert-True ($provider -match 'TryResolve') '外观提供器必须提供按 Pawn 解析入口。'

Assert-True ($registry -match 'class\s+CombatBodyFrontAppearanceRegistry') '必须存在 Core 外观注册表。'
Assert-True ($registry -match 'Register') '外观注册表必须支持提供器注册。'
Assert-True ($registry -match 'TryResolve') '外观注册表必须支持安全解析。'
Assert-True ($registry -match 'catch\s*\(') '外观注册表必须隔离单个提供器异常。'

Assert-True ($entry -notmatch 'BDP\.Content|BDPContent') 'Core 外观条目不得依赖 Content。'
Assert-True ($plan -notmatch 'BDP\.Content|BDPContent') 'Core 外观计划不得依赖 Content。'
Assert-True ($provider -notmatch 'BDP\.Content|BDPContent') 'Core 外观接口不得依赖 Content。'
Assert-True ($registry -notmatch 'BDP\.Content|BDPContent') 'Core 外观注册表不得依赖 Content。'

Write-Output 'CombatBodyFrontAppearanceCoreBoundarySmokeTests PASS'
