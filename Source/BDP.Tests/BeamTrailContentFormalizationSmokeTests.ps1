# BDP 光束拖尾正式化边界冒烟测试。
# 本测试锁定“公共系统与正式预设由 Content 提供，候选应用只引用正式预设”的准确范围。

$ErrorActionPreference = "Stop"

function Assert-True
{
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

function Read-Source
{
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path))
    {
        return ""
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$mainModRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$modsRoot = Split-Path -Parent $mainModRoot
$candidateModRoot = Join-Path $modsRoot "BorderDefenseProtocol.DevHarness"

$mainBeamTrailRoot = Join-Path $mainModRoot "Source\BDP.Content\Projectiles\BeamTrail"
$candidateBeamTrailRoot = Join-Path $candidateModRoot "Source\BDP.DevHarness\Projectiles\BeamTrail"
$mainPresetPath = Join-Path $mainModRoot "1.6\Content\Defs\Projectiles\BeamTrail\BeamTrailPresetDefs.xml"
$legacyMainPresetPath = Join-Path $mainModRoot "1.6\Defs\Projectiles\BeamTrail\BeamTrailPresetDefs.xml"
$candidatePresetPath = Join-Path $candidateModRoot "1.6\Defs\Things\Projectiles\Test\BeamTrailPresetDefs.xml"
$candidateChipPath = Join-Path $candidateModRoot "1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml"
$mainTexturePath = Join-Path $mainModRoot "1.6\Content\Textures\Things\Projectile\BDP_BeamTrail.png"
$legacyMainTexturePath = Join-Path $mainModRoot "Textures\Things\Projectile\BDP_BeamTrail.png"
$candidateTexturePath = Join-Path $candidateModRoot "Textures\Things\Projectile\BDP_BeamTrail.png"
$bootstrapPath = Join-Path $mainModRoot "Source\BDP.Content\ContentBootstrap.cs"
$loadFoldersPath = Join-Path $mainModRoot "LoadFolders.xml"

$formalSourceNames = @(
    "BeamTrailAppearanceSnapshot.cs",
    "BeamTrailAttachment.cs",
    "BeamTrailExtension.cs",
    "BeamTrailMapComponent.cs",
    "BeamTrailPresetDef.cs",
    "BeamTrailSegment.cs"
)

Assert-True (Test-Path -LiteralPath $mainBeamTrailRoot) "主模组缺少正式光束拖尾源码目录。"

foreach ($sourceName in $formalSourceNames)
{
    $sourcePath = Join-Path $mainBeamTrailRoot $sourceName
    Assert-True (Test-Path -LiteralPath $sourcePath) "主模组缺少正式光束拖尾源码：$sourceName"
}

Assert-True (-not (Test-Path -LiteralPath $candidateBeamTrailRoot)) "候选模组仍保留重复光束拖尾源码目录。"

$formalSourceText = @(
    Get-ChildItem -LiteralPath $mainBeamTrailRoot -File -Filter "*.cs" |
        Sort-Object Name |
        ForEach-Object { Read-Source $_.FullName }
) -join "`n"

Assert-True ($formalSourceText -match "namespace\s+BDP\.Content\.Projectiles\.BeamTrail") "正式拖尾源码缺少 Content 命名空间。"
Assert-True ($formalSourceText -match "class\s+BeamTrailExtension\s*:\s*DefModExtension,\s*IProjectileVisualAttachmentProvider") "正式拖尾扩展类型不正确。"
Assert-True ($formalSourceText -match "BeamTrailPresetDef\s+preset\s*;") "正式拖尾扩展缺少 preset 字段。"
Assert-True ($formalSourceText -notmatch "BDP\.DevHarness") "正式拖尾源码仍残留 DevHarness 语义。"
Assert-True ($formalSourceText -notmatch "ChipBeamTrailExtension") "正式拖尾源码仍残留芯片专属扩展名。"
Assert-True ($formalSourceText -notmatch "BeamTrailConfig") "正式拖尾源码仍残留旧直接配置入口。"
Assert-True ($formalSourceText -notmatch "\bbeamTrailPreset\b") "正式拖尾源码仍残留旧预设字段名。"
Assert-True ($formalSourceText -notmatch "public\s+bool\s+enabled") "正式拖尾源码仍残留重复 enabled 开关。"

$mapComponentText = Read-Source (Join-Path $mainBeamTrailRoot "BeamTrailMapComponent.cs")
Assert-True (
    ($mapComponentText -match "Mathf\.Min\(segment\.Start\.x,\s*segment\.End\.x\)") -and
    ($mapComponentText -match "Mathf\.Max\(segment\.Start\.x,\s*segment\.End\.x\)") -and
    ($mapComponentText -match "Mathf\.Min\(segment\.Start\.z,\s*segment\.End\.z\)") -and
    ($mapComponentText -match "Mathf\.Max\(segment\.Start\.z,\s*segment\.End\.z\)") -and
    ($mapComponentText -match "segment\.TrailWidth") -and
    ($mapComponentText -notmatch "visibleRect\.Contains\(midpoint\.ToIntVec3\(\)\)")
) "正式拖尾必须按整段覆盖范围与镜头相交判断，不能只检查中点。"

Assert-True (
    ($mapComponentText -match 'Scribe_Collections\.Look\(ref activeSegments,\s*"activeSegments",\s*LookMode\.Deep\)') -and
    ($mapComponentText -notmatch "Scribe_Collections\.Look\(ref liveSegmentsByProjectileId")
) "历史余痕必须存档，活动头段不得重复存档。"

$bootstrapText = Read-Source $bootstrapPath
Assert-True ($bootstrapText -notmatch "BeamTrail") "ContentBootstrap 不得注册具体拖尾业务。"

Assert-True (Test-Path -LiteralPath $mainTexturePath) "主模组缺少正式通用拖尾贴图。"
Assert-True (-not (Test-Path -LiteralPath $legacyMainTexturePath)) "主模组根级目录仍保留重复拖尾贴图。"
Assert-True (-not (Test-Path -LiteralPath $candidateTexturePath)) "候选模组仍保留重复拖尾贴图。"

$mainPresetText = Read-Source $mainPresetPath
Assert-True (Test-Path -LiteralPath $mainPresetPath) "主模组缺少正式拖尾预设 XML。"
Assert-True (-not (Test-Path -LiteralPath $legacyMainPresetPath)) "主模组 Core Def 目录仍保留 Content 拖尾预设。"
Assert-True (($mainPresetText | Select-String -AllMatches "BDP_TrailPreset_BrightMintLong").Matches.Count -eq 1) "主模组必须且只能定义一次亮薄荷绿长拖尾。"
Assert-True (($mainPresetText | Select-String -AllMatches "BDP_TrailPreset_BrightMintShort").Matches.Count -eq 1) "主模组必须且只能定义一次亮薄荷绿短拖尾。"
Assert-True (($mainPresetText | Select-String -AllMatches "\(0\.855,\s*0\.992,\s*0\.898,\s*1\.0\)").Matches.Count -eq 2) "两个正式预设必须使用首版亮薄荷绿色值。"
Assert-True ($mainPresetText -notmatch "BDP_TrailPreset_HotRed") "主模组不得迁入候选炽红预设。"
Assert-True ($mainPresetText -notmatch "BDP_TrailPreset_ColdBlue|BDP_TrailPreset_TrackingShortBlue") "主模组不得保留旧蓝色预设名称。"

$candidatePresetText = Read-Source $candidatePresetPath
Assert-True (
    (-not (Test-Path -LiteralPath $candidatePresetPath)) -and
    [string]::IsNullOrEmpty($candidatePresetText)
) "候选模组不得保留已经退役的本地拖尾预设文件。"

$candidateChipText = Read-Source $candidateChipPath
Assert-True (($candidateChipText | Select-String -AllMatches 'Class="BDP\.Content\.Projectiles\.BeamTrail\.BeamTrailExtension"').Matches.Count -eq 8) "候选八处芯片拖尾扩展必须全部机械对接正式类型。"
Assert-True (($candidateChipText | Select-String -AllMatches "<preset>BDP_TrailPreset_HotRed</preset>").Matches.Count -eq 0) "候选芯片不得继续引用已经退役的炽红预设。"
Assert-True (($candidateChipText | Select-String -AllMatches "<preset>BDP_TrailPreset_BrightMintLong</preset>").Matches.Count -eq 5) "候选长拖尾引用必须保持五处(原三处 + 两种追踪芯片按用户需求改长)。"
Assert-True (($candidateChipText | Select-String -AllMatches "<preset>BDP_TrailPreset_BrightMintShort</preset>").Matches.Count -eq 3) "候选短拖尾引用必须保持三处(追踪芯片改长后剩余三处)。"
Assert-True ($candidateChipText -notmatch "\bbeamTrailPreset\b|<enabled>true</enabled>") "候选拖尾扩展仍残留旧字段或重复开关。"

[xml]$loadFolders = Get-Content -LiteralPath $loadFoldersPath -Raw
$loadedFolders = @($loadFolders.loadFolders.'v1.6'.li | ForEach-Object { [string]$_ })
Assert-True ($loadedFolders.Count -eq 3) "主模组必须加载三个正式根目录。"
Assert-True (
    $loadedFolders[0] -eq "/" -and
    $loadedFolders[1] -eq "1.6" -and
    $loadedFolders[2] -eq "1.6/Content"
) "主模组加载顺序必须为 /、1.6、1.6/Content。"

Write-Output "BeamTrailContentFormalizationSmokeTests PASS"
