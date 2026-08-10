$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreAssemblyRoot = Join-Path $repoRoot 'Source\BDP\Core\Assembly'
$contentAssemblyRoot = Join-Path $repoRoot 'Source\BDP.Content\Assembly'
$coreSnapshotPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\External\ChipDefinitionSnapshot.cs'
$coreSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Chips\External\ChipSnapshotAccess.cs'

$assemblyFileNames = @(
    'Building_TriggerAssembler.cs', 'CompChipContainer.cs', 'CompProperties_ChipContainer.cs',
    'ITab_ChipStorageContents.cs', 'DefaultAssemblerFacilityProvider.cs', 'IAssemblerFacilityProvider.cs',
    'TriggerAssemblyOperationResult.cs', 'TriggerAssemblyTransaction.cs', 'AssemblyJobDefs.cs',
    'JobDriver_HaulToChipStorage.cs', 'JobDriver_UseTriggerAssembler.cs', 'WorkGiver_HaulToChipStorage.cs',
    'Panel_ChipDetail.cs', 'Panel_ChipInventory.cs', 'Panel_SlotLayout.cs', 'TriggerAssemblyDragState.cs',
    'TriggerAssemblyPreviewService.cs', 'Window_TriggerAssembly.cs'
)

Assert-True (Test-Path -LiteralPath $coreSnapshotPath) 'Core must expose ChipDefinitionSnapshot.'
Assert-True (Test-Path -LiteralPath $coreSurfacePath) 'Core must expose ChipSnapshotAccess.'
$snapshotText = Get-Content -LiteralPath $coreSnapshotPath -Raw -Encoding utf8
$surfaceText = Get-Content -LiteralPath $coreSurfacePath -Raw -Encoding utf8
Assert-True ($snapshotText -match 'public bool IsValid' -and $snapshotText -match 'public ChipSlotRegion SlotRegion' -and $snapshotText -match 'public ChipSlotOccupancy SlotOccupancy') 'ChipDefinitionSnapshot must expose only neutral validity and slot fields.'
Assert-True ($snapshotText -match 'public float CapacityCost' -and $snapshotText -match 'public float ActivationCost') 'ChipDefinitionSnapshot must expose Trion costs used by the assembly preview.'
Assert-True ($snapshotText -match 'public int ActivationDelayTicks' -and $snapshotText -match 'public int DeactivationDelayTicks') 'ChipDefinitionSnapshot must expose neutral assembly delay fields used by the detail panel.'
Assert-True ($surfaceText -match 'public static ChipDefinitionSnapshot Read\(Thing chip\)' -and $surfaceText -match 'ChipSurfaceAccess\.Read') 'ChipSnapshotAccess must wrap the validated Core reader.'

foreach ($name in $assemblyFileNames) {
    $matches = @(Get-ChildItem -LiteralPath $contentAssemblyRoot -Recurse -File -Filter $name)
    Assert-True ($matches.Count -eq 1) "Content 缺少或重复装配源码：$name"
}

Assert-True (-not (Test-Path -LiteralPath $coreAssemblyRoot)) 'Core 仍保留装配业务目录。'

$contentDefPaths = @(
    (Join-Path $repoRoot '1.6\Content\Defs\ThingDef\Buildings\Assembly.xml'),
    (Join-Path $repoRoot '1.6\Content\Defs\JobDef\Assembly.xml'),
    (Join-Path $repoRoot '1.6\Content\Defs\WorkGiverDef\Assembly.xml')
)
foreach ($path in $contentDefPaths) {
    Assert-True (Test-Path -LiteralPath $path) "Content 缺少装配 Def：$path"
}

Assert-True (-not (Test-Path -LiteralPath (Join-Path $repoRoot '1.6\Defs\Buildings\Assembly'))) '旧 Buildings/Assembly Def 目录仍存在。'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $repoRoot '1.6\Defs\Jobs\Assembly'))) '旧 Jobs/Assembly Def 目录仍存在。'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $repoRoot '1.6\Defs\WorkGivers\Assembly'))) '旧 WorkGivers/Assembly Def 目录仍存在。'

$contentTexts = Get-ChildItem -LiteralPath $contentAssemblyRoot -Recurse -File -Filter '*.cs' |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }
Assert-True (($contentTexts -join "`n") -notmatch 'ChipSurfaceAccess|ChipDefinitionReadResult') 'Content 不得直接使用 Core 内部芯片读取类型。'
Assert-True (($contentTexts -join "`n") -match 'ChipSnapshotAccess') 'Content 装配业务必须使用公开芯片读取面。'

Write-Output 'TriggerAssemblyContentBoundary PASS'
