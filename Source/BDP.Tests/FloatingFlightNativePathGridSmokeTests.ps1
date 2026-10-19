$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Read-RequiredFile {
    param([string]$Path)
    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空根因修复文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$floatingRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight'
$pathingRoot = Join-Path $floatingRoot 'Pathing'
$patchRoot = Join-Path $floatingRoot 'Patches'
$customizerPath = Join-Path $pathingRoot 'FloatingFlightPathGridCustomizer.cs'
$pathFinderPatchPath = Join-Path $patchRoot 'Patch_PathFinderMapData_ParameterizeGridJob_FloatingFlight.cs'
$customizerText = Read-RequiredFile $customizerPath
$pathFinderPatchText = Read-RequiredFile $pathFinderPatchPath

Assert-True ($customizerText -match 'IPathGridCustomizer') '浮空路径必须使用原版 PathRequest.IPathGridCustomizer 缓存隔离入口。'
Assert-True ($customizerText -match 'Hybrid|Always') '浮空路径缓存身份必须区分 Hybrid 与 Always。'
Assert-True ($customizerText -match 'inner|customizer') '浮空路径必须保留已有第三方路径网格定制器。'
Assert-True ($pathFinderPatchText -match 'CreateRequest') '浮空路径必须在 PathFinder.CreateRequest 返回后接入路径身份。'
Assert-True ($pathFinderPatchText -match 'ParameterizeGridJob') '浮空路径必须接入实际 PathFinder 网格作业参数化入口。'
Assert-True ($pathFinderPatchText -match 'Grid_Unsafe|pathGridDirect') '实际路径网格作业必须使用 BDP 浮空 PathGrid 的原生网格数组。'
Assert-True ($pathFinderPatchText -match 'customizer|Customizer') '路径网格补丁必须保留已有定制器兼容性。'

Write-Output 'FloatingFlightNativePathGridSmokeTests PASS'
