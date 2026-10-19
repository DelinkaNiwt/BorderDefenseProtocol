$ErrorActionPreference = 'Stop'

# 每个测试进程只加载明确指定的版本，禁止引用目录隐式回退。
function Initialize-RangedVersionTest {
    param([Parameter(Mandatory)][ValidateSet('1.6','1.7')][string]$RimWorldVersion)
    $script:RangedModRoot = Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
    [xml]$local = Get-Content -LiteralPath (Join-Path $script:RangedModRoot 'Source/Build/Bdp.Local.props') -Raw
    $group = $local.Project.PropertyGroup
    $referenceDir = if ($RimWorldVersion -eq '1.6') { $group.RimWorld16ReferenceDir } else { $group.RimWorld17ReferenceDir }
    $harmony = if ($RimWorldVersion -eq '1.6') { $group.Harmony16AssemblyPath } else { $group.Harmony17AssemblyPath }
    $references = if ($RimWorldVersion -eq '1.6') { @('UnityEngine.dll','UnityEngine.CoreModule.dll') } else { @('UnityStubs.dll','Waylith.dll','Waylith.UnityCompat.dll') }
    foreach ($file in $references + @('Assembly-CSharp.dll')) {
        $path = Join-Path $referenceDir $file
        if (-not (Test-Path -LiteralPath $path)) { throw "缺少目标版本引用：$path" }
        [void][Reflection.Assembly]::LoadFrom($path)
    }
    [void][Reflection.Assembly]::LoadFrom($harmony)
    $script:RangedCore = [Reflection.Assembly]::LoadFrom((Join-Path $script:RangedModRoot "$RimWorldVersion/Assemblies/BDP.Core.dll"))
    $script:RangedContent = [Reflection.Assembly]::LoadFrom((Join-Path $script:RangedModRoot "$RimWorldVersion/Assemblies/BDP.Content.dll"))
    $script:RangedReferenceDir = $referenceDir
}

# 失败必须指向具体行为，不能因错误的历史路径冒充回归。
function Assert-Ranged {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}
