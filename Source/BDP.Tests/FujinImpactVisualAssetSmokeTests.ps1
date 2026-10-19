$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Xml {
    param([string]$Path)

    Assert-True (Test-Path -LiteralPath $Path) ("缺少风刃命中特效资源或 Def：" + $Path)
    return [xml](Get-Content -LiteralPath $Path -Raw -Encoding utf8)
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$effectPath = Join-Path $repoRoot '1.6\Content\Defs\Effects\ImpactBladeBurst.xml'
$moteDefPath = Join-Path $repoRoot '1.6\Content\Defs\ThingDef\Mote_ImpactBlade.xml'
$texturePath = Join-Path $repoRoot '1.6\Textures\Effects\ImpactBlade\ImpactBlade.png'
$effectDocument = Read-Xml $effectPath
$moteDocument = Read-Xml $moteDefPath
$moteDefs = @($moteDocument.Defs.ThingDef)
$baseMote = @($moteDefs | Where-Object { $_.Name -eq 'BDP_Mote_ImpactBladeBase' })[0]
$concreteMotes = @($moteDefs | Where-Object { [string]$_.defName -match '^BDP_Mote_ImpactBlade_(Back|Front)$' })
$effecter = @($effectDocument.Defs.EffecterDef | Where-Object {
    $_.defName -eq 'BDP_Effecter_ImpactBladeBurst'
})[0]

Assert-True (Test-Path -LiteralPath $texturePath) '缺少风刃命中刀刃纹理。'
Assert-True ($null -ne $effecter) '缺少风刃命中 Effecter。'
Assert-True ($null -ne $baseMote) '缺少风刃自定义刀刃 Mote 公共 Def。'
Assert-True ($concreteMotes.Count -eq 2) '风刃命中特效必须包含前后两个 Mote Def。'
Assert-True (@($effectDocument.Defs.ChildNodes | Where-Object { $_.Name -eq 'FleckDef' }).Count -eq 0) '方案 3 不应继续使用 Fleck 刀刃 Def。'

Assert-True ([string]$baseMote.thingClass -eq 'BDP.Content.RangedModules.ImpactVisual.Mote_ImpactBlade') `
    '刀刃公共 Def 必须使用自定义 Mote 类。'
Assert-True ([string]$baseMote.graphicData.graphicClass -eq 'Graphic_Single') '刀刃 Mote 必须提供 Graphic_Single 材质入口。'
Assert-True ([string]$baseMote.graphicData.texPath -eq 'Effects/ImpactBlade/ImpactBlade') '刀刃 Mote 必须复用透明刀刃纹理。'
Assert-True ([string]$baseMote.graphicData.shaderType -eq 'MoteGlow') '刀刃 Mote 必须使用 MoteGlow 着色器。'
Assert-True ([string]$baseMote.mote.realTime -eq 'false') '刀刃 Mote 必须使用游戏 Tick 驱动。'
Assert-True ([double]$baseMote.mote.solidTime -ge 0.20) '刀刃生命周期必须覆盖最晚延迟刀刃的完整轮廓。'
Assert-True ([double]$baseMote.mote.fadeOutTime -ge 0.10) '刀刃必须保留短暂淡出阶段。'

foreach ($layerName in @('Back', 'Front')) {
    $defName = 'BDP_Mote_ImpactBlade_' + $layerName
    $mote = @($concreteMotes | Where-Object { $_.defName -eq $defName })[0]
    Assert-True ($null -ne $mote) ("缺少风刃前后层 Mote Def：" + $defName)
    Assert-True ([string]$mote.ParentName -eq 'BDP_Mote_ImpactBladeBase') '前后层 Mote 必须继承公共配置。'
    if ($layerName -eq 'Back') {
        Assert-True ([string]$mote.altitudeLayer -eq 'Projectile') '后景刀刃必须位于 Projectile 层。'
    } else {
        Assert-True ([string]$mote.altitudeLayer -eq 'MoteOverhead') '前景刀刃必须位于 MoteOverhead 层。'
    }
}

$children = @($effecter.children.li)
Assert-True ($children.Count -eq 2) '风刃命中特效必须只保留前后两个动态刀刃阵列子项。'
Assert-True ([int]$effecter.maintainTicks -ge 20) '动态刀刃阵列维护窗口必须覆盖最晚刀刃生命周期。'
Assert-True (@($children | Where-Object { [string]$_.moteDef -match '_Back$' }).Count -eq 1) `
    '风刃命中特效必须包含一个后景动态刀刃阵列。'
Assert-True (@($children | Where-Object { [string]$_.moteDef -match '_Front$' }).Count -eq 1) `
    '风刃命中特效必须包含一个前景动态刀刃阵列。'

foreach ($child in $children) {
    Assert-True ($null -eq $child.fleckDef) '方案 3 的刀刃子项不得继续引用 Fleck。'
    Assert-True ([string]$child.moteDef -match '^BDP_Mote_ImpactBlade_(Back|Front)$') `
        '动态刀刃阵列子项必须引用前后层 Mote Def。'
    Assert-True ([string]$child.subEffecterClass -eq 'BDP.Content.RangedModules.ImpactVisual.SubEffecter_ImpactBladeBurst') `
        '动态刀刃阵列子项必须使用自定义子特效器。'
    Assert-True ($null -eq $child.soundDef) '风刃命中特效不应附带声音子效果。'
}

Assert-True ($effectDocument.OuterXml -notmatch 'AirPuff|SparkThrown|Smoke|Debris') '风刃命中特效不应混入烟雾或碎屑。'

Write-Output 'FujinImpactVisualAssetSmokeTests PASS'
