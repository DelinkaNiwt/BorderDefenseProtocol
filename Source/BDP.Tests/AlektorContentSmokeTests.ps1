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

# 测试脚本位于 Source\BDP.Tests，向上定位模组工程根目录。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$triggerPath = Join-Path $repoRoot 'Common\Content\Defs\ThingDef\Equipment\Trigger.xml'
$chipPath = Join-Path $repoRoot 'Common\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$projectilePath = Join-Path $repoRoot 'Common\Content\Defs\ThingDef\Projectiles\Projectiles.xml'

foreach ($path in @($triggerPath, $chipPath, $projectilePath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) ('Required Alektor definition file is missing: ' + $path)
}

$triggerDocument = [xml](Get-Content -LiteralPath $triggerPath -Raw -Encoding UTF8)
$chipDocument = [xml](Get-Content -LiteralPath $chipPath -Raw -Encoding UTF8)
$projectileDocument = [xml](Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8)

$triggerDef = $triggerDocument.Defs.SelectSingleNode("ThingDef[defName='BDP_TriggerBody_Alektor']")
$triggerProps = $triggerDef.SelectSingleNode("comps/li[@Class='BDP.Core.Trigger.CompProperties_TriggerBody']")
$chipDef = $chipDocument.Defs.SelectSingleNode("*[defName='BDP_ChipFunction_Alektor']")
$floatingChipDef = $chipDocument.Defs.SelectSingleNode("*[defName='BDP_ChipFunction_Floating']")
$entry = $chipDef.SelectSingleNode("config/Expression/Entries/li[Id='alektor_primary']")
$projectileDef = $projectileDocument.Defs.SelectSingleNode("ThingDef[defName='BDP_Projectile_BodyCollapse']")

Assert-True ($null -ne $triggerDef) 'Alektor trigger body definition must exist.'
Assert-True ($null -ne $triggerProps) 'Alektor trigger body must provide TriggerBody comp properties.'
Assert-True ($null -ne $chipDef) 'Alektor chip function definition must exist.'
Assert-True ($null -ne $floatingChipDef) 'Floating chip function definition must exist.'
Assert-True ($null -ne $entry) 'Alektor must provide a ranged primary expression entry.'
Assert-True ($null -ne $projectileDef) 'Alektor body-collapse projectile definition must exist.'

# 卵之冠是黑触发器，只提供6个特殊槽，不提供主副槽，并固定预载两枚芯片。
Assert-True ($triggerProps.triggerCategory -eq 'BDP_TriggerCategory_Black') `
    'Alektor trigger must use the Black trigger category.'
Assert-True ([int]$triggerProps.mainSlotCount -eq 0) 'Alektor trigger must provide zero Main slots.'
Assert-True ([int]$triggerProps.subSlotCount -eq 0) 'Alektor trigger must provide zero Sub slots.'
Assert-True ([int]$triggerProps.specialSlotCount -eq 6) 'Alektor trigger must provide six Special slots.'
$preloadEntries = @($triggerProps.SelectNodes("fixedLoadout/li[side='Special']"))
Assert-True ($preloadEntries.Count -eq 2) 'Alektor trigger must preload exactly two Special-slot chips.'
Assert-True ($preloadEntries[0].slotNumber -eq '1' -and
    $preloadEntries[0].chipDef -eq 'BDP_Chip_Custom' -and
    $preloadEntries[0].initializationKey -eq 'BDP_ChipFunction_Alektor') `
    'Alektor trigger must preload its chip function into Special slot 1.'
Assert-True ($preloadEntries[1].slotNumber -eq '2' -and
    $preloadEntries[1].chipDef -eq 'BDP_Chip_Custom' -and
    $preloadEntries[1].initializationKey -eq 'BDP_ChipFunction_Floating') `
    'Alektor trigger must preload the Floating chip function into Special slot 2.'

# 芯片只允许特殊槽固定使用，制作台不可见，激活切换即时完成。
Assert-True ($chipDef.showInManufacturing -eq 'false') 'Alektor chip must be hidden from manufacturing.'
Assert-True ($chipDef.config.Profile.Category -eq 'BDP_ChipCategory_Weapon') `
    'Alektor chip must use the Weapon chip category.'
Assert-True ($chipDef.config.Loadout.SlotRegion -eq 'Special') `
    'Alektor chip must declare the Special slot region.'
Assert-True ($chipDef.config.Loadout.SlotOccupancy -eq 'Single') `
    'Alektor chip must occupy one physical slot.'
Assert-True ([int]$chipDef.config.Loadout.ActivationDelayTicks -eq 0) `
    'Alektor chip activation delay must be zero.'
Assert-True ([int]$chipDef.config.Loadout.DeactivationDelayTicks -eq 0) `
    'Alektor chip deactivation delay must be zero.'

# 远程参数按需求固定，目标只开放Pawn，追踪和部位崩溃均复用现有模块。
Assert-True ($entry.EntryType -eq 'PrimaryVerb' -and $entry.WeaponMode -eq 'Ranged') `
    'Alektor expression must be a ranged primary verb.'
Assert-True ($entry.Execution.Rhythm -eq 'Sequential') `
    'Alektor expression must use sequential firing rhythm.'
Assert-True ($entry.VerbProps.defaultProjectile -eq 'BDP_Projectile_BodyCollapse') `
    'Alektor must use the body-collapse projectile.'
Assert-True ([double]$entry.VerbProps.range -eq 30.9) 'Alektor range must match the assault-rifle range preset.'
Assert-True ([double]$entry.VerbProps.warmupTime -eq 2.0) 'Alektor warmup time must be two seconds.'
Assert-True ([double]$entry.VerbProps.defaultCooldownTime -eq 2.0) 'Alektor ranged cooldown must be two seconds.'
Assert-True ([double]$entry.VerbProps.accuracyTouch -eq 0.8 -and
    [double]$entry.VerbProps.accuracyShort -eq 0.8 -and
    [double]$entry.VerbProps.accuracyMedium -eq 0.8 -and
    [double]$entry.VerbProps.accuracyLong -eq 0.8) `
    'Alektor accuracy must be 0.8 at every range band.'
Assert-True ($entry.VerbProps.targetParams.canTargetLocations -eq 'false' -and
    $entry.VerbProps.targetParams.canTargetBuildings -eq 'false' -and
    $entry.VerbProps.targetParams.canTargetPawns -eq 'true') `
    'Alektor must target direct Pawn targets only.'
$moduleNames = @($entry.SelectNodes('RangedModules/li/moduleDef') | ForEach-Object { $_.'#text' })
Assert-True ($moduleNames -contains 'BDP_HomingModule_Strong') `
    'Alektor must reuse the Hornet strong homing module.'
Assert-True ($moduleNames -contains 'BDP_RangedDebuffModule') `
    'Alektor must reuse the generic ranged-debuff host.'

# 投射物基础值按需求配置，伤害抑制由部位崩溃模块负责。
Assert-True ($projectileDef.projectile.damageDef -eq 'Bullet') 'Alektor projectile must use Bullet damage.'
Assert-True ([int]$projectileDef.projectile.damageAmountBase -eq 1) 'Alektor projectile damage must be one.'
Assert-True ([double]$projectileDef.projectile.armorPenetrationBase -eq 0) 'Alektor projectile armor penetration must be zero.'
Assert-True ([double]$projectileDef.projectile.speed -eq 20) 'Alektor projectile speed must be 20.'
Assert-True ($projectileDef.modExtensions.li.preset -eq 'BDP_TrailPreset_BrightMintShort') `
    'Alektor projectile must use the short trail preset.'

Write-Output 'AlektorContentSmokeTests PASS'
