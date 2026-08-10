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

function Get-MethodBlock {
    param(
        [string]$Text,
        [string]$SignaturePattern
    )

    $signature = [regex]::Match($Text, $SignaturePattern)
    if (-not $signature.Success) {
        return $null
    }

    $openBrace = $Text.IndexOf('{', $signature.Index + $signature.Length)
    if ($openBrace -lt 0) {
        return $null
    }

    $depth = 0
    for ($index = $openBrace; $index -lt $Text.Length; $index++) {
        if ($Text[$index] -eq '{') { $depth++ }
        if ($Text[$index] -eq '}') {
            $depth--
            if ($depth -eq 0) {
                return $Text.Substring($openBrace, $index - $openBrace + 1)
            }
        }
    }

    return $null
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$providerPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyTransformScanPresentationProvider.cs'
$motePath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\Mote_CombatBodyScan.cs'
$bootstrapPath = Join-Path $sourceRoot 'BDP.Content\ContentBootstrap.cs'
$moteDefPath = Join-Path $modRoot '1.6\Content\Defs\ThingDef\Mote_CombatBodyScan.xml'

Assert-True -Condition (Test-Path -LiteralPath $providerPath) -Message '缺少 Content 扫描表现提供器。'
Assert-True -Condition (Test-Path -LiteralPath $motePath) -Message '缺少扫描 Mote。'
Assert-True -Condition (Test-Path -LiteralPath $bootstrapPath) -Message '缺少 ContentBootstrap。'
Assert-True -Condition (Test-Path -LiteralPath $moteDefPath) -Message '缺少扫描 Mote Def。'

$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$moteText = Get-Content -LiteralPath $motePath -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding utf8
$moteDefText = Get-Content -LiteralPath $moteDefPath -Raw -Encoding utf8
$drawAtText = Get-MethodBlock -Text $moteText -SignaturePattern 'protected override void DrawAt\('

Assert-True -Condition ($providerText -match 'ICombatBodyTransformPresentationProvider') -Message '提供器必须实现 Core 表现接口。'
Assert-True -Condition ($providerText -match 'pawn\.Spawned[\s\S]*pawn\.Map[\s\S]*Find\.CameraDriver') -Message '提供器必须检查生成、地图和镜头状态。'
Assert-True -Condition ($providerText -match 'CurrentViewRect') -Message '镜头外 Pawn 不应创建 Mote。'
Assert-True -Condition ($providerText -match 'CombatBodyPawnVisualCapture\.Capture\(pawn\)') -Message '提供器必须复用原版完整人物视觉捕获器。'
Assert-True -Condition ($providerText -match 'GenSpawn\.Spawn') -Message '提供器必须生成一个扫描 Mote。'
Assert-True -Condition ($bootstrapText -match 'CombatBodyTransformPresentationRegistry\.Register\(new CombatBodyTransformScanPresentationProvider\(\)\)') -Message 'ContentBootstrap 必须注册扫描提供器。'
Assert-True -Condition ($bootstrapText -match 'CombatBodyScanMeshCache\.WarmUp\(\)') -Message '裁切网格必须在模组启动时预热，不能留到首次变身。'
Assert-True -Condition ($moteText -match 'internal const int DurationTicks = 10;') -Message '默认扫描时长必须为 10 tick，并供提供器共享。'
Assert-True -Condition ($moteText -match 'private const float HeadUpperBoundFactor = 0\.38f;') -Message '头顶可视边界系数必须为 0.38。'
Assert-True -Condition ($moteText -match 'headOffset\.z \+ headWidth \* HeadUpperBoundFactor') -Message '扫描上边界必须使用收紧后的头顶可视边界。'
Assert-True -Condition ($moteText -match 'private const float CoreThickness = 0\.075f;') -Message '核心光厚度必须为 0.075。'
Assert-True -Condition ($moteText -match 'private const float HaloThickness = 0\.24f;') -Message '柔光厚度必须为 0.24。'
Assert-True -Condition ($moteText -match 'CombatBodyScanMeshCache\.GetMesh') -Message '完整人物快照必须使用裁切网格缓存。'
Assert-True -Condition ($drawAtText -match 'DrawSnapshot\(outgoingSnapshot, outgoingKeepUpper') -Message '扫描必须绘制退场完整人物一侧。'
Assert-True -Condition ($drawAtText -match 'DrawSnapshot\(incomingSnapshot, !outgoingKeepUpper') -Message '扫描必须绘制互补的入场完整人物一侧。'
Assert-True -Condition ($moteText -match 'snapshot\.Material') -Message '完整人物快照必须复用捕获材质。'
Assert-True -Condition ($moteText -match 'HumanlikeMeshPoolUtility\.HumanlikeBodyWidthForPawn') -Message '扫描边界必须复用原版人形网格宽度。'
Assert-True -Condition ($moteText -match 'BaseHeadOffsetAt') -Message '头部衣物必须复用原版头部偏移。'
Assert-True -Condition ($moteText -notmatch 'DrawTrailGlow|trailAlpha|TrailMaterial') -Message '旧整块尾迹辉光必须移除。'
Assert-True -Condition (($providerText + $moteText) -notmatch 'FleckMaker|ThrowDustPuff|GenExplosion|AccessTools|BindingFlags') -Message '扫描链不得包含粒子、爆炸或反射。'
Assert-True -Condition ($null -ne $drawAtText) -Message '无法读取 DrawAt。'
Assert-True -Condition ($drawAtText -notmatch 'new\s+(Mesh|Material|List|RenderTexture)') -Message 'DrawAt 每帧路径不得创建 Mesh、Material、List 或 RenderTexture。'
Assert-True -Condition ($moteDefText -match '<texPath>BDP/Effects/LeakParticle</texPath>') -Message 'Mote Def 必须复用现有扫描光纹理。'
Assert-True -Condition ($moteDefText -match '<ThingDef ParentName="MoteBase">') -Message '短命扫描 Mote 必须继承原版 MoteBase 的不可存档设置。'
Assert-True -Condition ($moteDefText -notmatch '<saveable>') -Message 'ThingDef 不存在 saveable 字段，禁止写入无效 XML。'
Assert-True -Condition ($moteDefText -match '<realTime>false</realTime>') -Message '扫描动画必须使用游戏 tick。'

Write-Output 'CombatBodyTransformScanContent PASS'
