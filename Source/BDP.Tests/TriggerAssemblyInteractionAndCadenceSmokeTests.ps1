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
$windowRoot = Join-Path $repoRoot 'Source\BDP.Content\Assembly\Window'
$windowPath = Join-Path $windowRoot 'Window_TriggerAssembly.cs'
$inventoryPath = Join-Path $windowRoot 'Panel_ChipInventory.cs'
$slotLayoutPath = Join-Path $windowRoot 'Panel_SlotLayout.cs'
$detailRendererPath = Join-Path $windowRoot 'ChipInformationDetailRenderer.cs'
$windowLanguagePath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Windows.xml'
$chipFunctionPath = Join-Path $repoRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$armamentFormPath = Join-Path $repoRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'

foreach ($path in @($windowPath, $inventoryPath, $slotLayoutPath, $detailRendererPath, $windowLanguagePath, $chipFunctionPath, $armamentFormPath)) {
    Assert-True (Test-Path -LiteralPath $path) "Required trigger assembly source/data must exist: $path"
}

$windowText = Get-Content -LiteralPath $windowPath -Raw -Encoding utf8
$inventoryText = Get-Content -LiteralPath $inventoryPath -Raw -Encoding utf8
$slotLayoutText = Get-Content -LiteralPath $slotLayoutPath -Raw -Encoding utf8
$detailRendererText = Get-Content -LiteralPath $detailRendererPath -Raw -Encoding utf8
$windowLanguageText = Get-Content -LiteralPath $windowLanguagePath -Raw -Encoding utf8
$chipFunctionXml = [xml](Get-Content -LiteralPath $chipFunctionPath -Raw -Encoding utf8)
$armamentFormXml = [xml](Get-Content -LiteralPath $armamentFormPath -Raw -Encoding utf8)

Assert-True ($windowText -match '(?m)draggable\s*=\s*true\s*;') `
    'Trigger assembly window must enable Verse.Window native dragging.'

$slotInput = [regex]::Match($windowText, '(?s)TriggerAssemblySlotHit slotHit = slotLayoutPanel\.HitAt\(.*?\n\s*\}\n\n\s*TriggerAssemblyInventoryHit').Value
Assert-True ($slotInput -match 'SelectSlot\(slotHit\.Slot\.Side') `
    'Clicking any slot, including an empty slot, must select that slot as the assembly target.'
Assert-True ($slotInput -notmatch 'LoadedChip\s*==\s*null') `
    'Clicking an empty slot must not be treated as clicking the blank area outside slot cells.'
Assert-True ($windowText -match 'slotLayoutPanel\.IsBlankAreaAt\(current\.mousePosition\)') `
    'Only blank space inside the slot board may clear the current slot selection.'
$clearSelection = [regex]::Match($windowText, '(?s)private void ClearSlotSelection\(\).*?\n\s*\}\n\n\s*///').Value
Assert-True ($clearSelection -match 'hasSelectedSlot\s*=\s*false') `
    'Blank slot-board space must clear the selected slot.'
Assert-True ($clearSelection -match 'selectedChip\s*=\s*null') `
    'Blank slot-board space must clear the selected chip card together with the slot selection.'
Assert-True ($clearSelection -match 'InvalidateSelectedChipInformation\(\)') `
    'Blank slot-board space must clear the right-side chip detail together with the selection.'
Assert-True ($slotLayoutText -match 'internal bool IsBlankAreaAt\(Vector2 mousePosition\)') `
    'Slot layout must expose a hit test for blank space inside the slot board.'
Assert-True ($slotLayoutText -match 'slotBoardRects') `
    'Slot layout must track the visible stage and special-slot board rectangles for blank-space hit testing.'

Assert-True ($inventoryText -match 'ChipCardTextHeight\s*=\s*37f') `
    'Chip card text height must be explicit for vertical centering.'
Assert-True ($inventoryText -match 'textTop\s*=\s*rect\.y\s*\+\s*\(rect\.height\s*-\s*ChipCardTextHeight\)\s*\*\s*0\.5f') `
    'Chip card text must be vertically centered from the card bounds.'
Assert-True ($inventoryText -match 'new Rect\(rect\.x \+ 53f,\s*textTop,') `
    'Chip card title/meta block must use the centered text top.'

Assert-True ($detailRendererText -match 'FeatureRendererRegistry' -and
    $detailRendererText -match 'RangedAttackFeatureRenderer' -or
    $detailRendererText -match 'RenderAll') `
    '详情渲染器必须使用语义功能展示器，不再从技术图表推导功能。'

$asteroid = $chipFunctionXml.SelectNodes('//*[contains(local-name(),"ChipManufacturingPresetDef")]') |
    Where-Object { $_.defName -eq 'BDP_ChipFunction_Asteroid' } |
    Select-Object -First 1
$trionCube = $armamentFormXml.SelectNodes('//*[contains(local-name(),"ChipArmamentFormDef")]') |
    Where-Object { $_.defName -eq 'BDP_ArmamentForm_TrionCube' } |
    Select-Object -First 1
Assert-True ($null -ne $asteroid -and $null -ne $trionCube) `
    'TrionCube plus asteroid final-result example must remain inspectable.'
Assert-True ($asteroid.config.Expression.Entries.li.Execution.Rhythm -eq 'Simultaneous') `
    'Asteroid must remain a simultaneous volley before TrionCube overlay.'
Assert-True ([int]$asteroid.config.Expression.Entries.li.VerbProps.burstShotCount -eq 1) `
    'Asteroid base burst count must be one before TrionCube overlay.'
Assert-True ([int]$trionCube.overrides.burstShotCount -eq 4) `
    'TrionCube must override only the final burst count to four.'

$finalWarmup = [double]$asteroid.config.Expression.Entries.li.VerbProps.warmupTime
$finalCooldown = [double]$asteroid.config.Expression.Entries.li.VerbProps.defaultCooldownTime
$finalBurst = [int]$trionCube.overrides.burstShotCount
$simultaneousCycle = $finalWarmup + $finalCooldown
$simultaneousRpm = $finalBurst * 60 / $simultaneousCycle
Assert-True ([math]::Abs($simultaneousRpm - 80) -lt 0.01) `
    'TrionCube plus asteroid must calculate 80 RPM while ignoring the stored interval.'

Write-Output 'TriggerAssemblyInteractionAndCadenceSmokeTests PASS'
