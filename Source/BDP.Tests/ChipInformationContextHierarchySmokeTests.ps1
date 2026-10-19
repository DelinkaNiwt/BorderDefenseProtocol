$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$coreInformationRoot = Join-Path $sourceRoot 'BDP\Core\Information'
$contentRendererPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs'

$factPath = Join-Path $coreInformationRoot 'Model\ChipInformationContextFact.cs'
$nodeProjectorPath = Join-Path $coreInformationRoot 'Model\ChipInformationNodeProjector.cs'
$adapterPath = Join-Path $coreInformationRoot 'Input\ChipInformationDefinitionInputAdapter.cs'
$parserPath = Join-Path $coreInformationRoot 'ChipInformationParser.cs'
$composerPath = Join-Path $coreInformationRoot 'Input\ChipInformationInputComposer.cs'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw "FAIL: $Message"
    }
}

function Read-Source([string]$Path) {
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$factText = Read-Source $factPath
$nodeProjectorText = Read-Source $nodeProjectorPath
$adapterText = Read-Source $adapterPath
$parserText = Read-Source $parserPath
$composerText = Read-Source $composerPath
$rendererText = Read-Source $contentRendererPath

# 信息上下文必须显式保留父级，不能只依赖 mode:stance 复合字符串。
Assert-True ($factText -match 'ParentContextKey') '信息上下文事实必须保存父级上下文键。'
Assert-True ($adapterText -match 'parentContextKey:\s*mode\.ModeKey') '姿态上下文必须把所属形态作为父级传入。'
Assert-True ($parserText -match 'fact\.ParentContextKey') '信息解析器必须保留上下文父级关系。'
Assert-True ($parserText -match 'NestContexts') '信息解析器必须把上下文父键组装成节点树。'
Assert-True ($nodeProjectorText -match 'ParentContextPropertyKey') '上下文树组装必须使用共享父键契约。'
Assert-True ($composerText -match 'context\.ParentContextKey') '信息输入合并器必须保留上下文父级关系。'

# 详情界面必须按父级分组并默认绘制完整上下文树。
Assert-True ($rendererText -match 'ReadDirectContexts') '详情界面必须读取直接上下文节点。'
Assert-True ($rendererText -match 'DrawContexts') '详情界面必须提供上下文区块。'
Assert-True ($rendererText -match 'DrawContextTree') '详情界面必须递归绘制上下文树。'
Assert-True ($rendererText -match 'GetContextLabel') '详情界面必须使用上下文玩家向名称。'

Write-Output 'ChipInformationContextHierarchy PASS'
