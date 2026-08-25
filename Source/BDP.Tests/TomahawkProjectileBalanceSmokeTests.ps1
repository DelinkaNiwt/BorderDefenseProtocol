$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-DefNode {
    param(
        [xml]$Xml,
        [string]$DefName
    )

    return $Xml.SelectSingleNode("//*[defName='$DefName']")
}

function Get-LanguageNode {
    param(
        [xml]$Xml,
        [string]$Key
    )

    return $Xml.SelectSingleNode("//*[local-name()='$Key']")
}

$modRoot = Get-BdpModRoot
$projectilePath = Join-Path $modRoot "1.6\Content\Defs\ThingDef\Projectiles\Projectiles.xml"
$armamentPath = Join-Path $modRoot "1.6\Content\Defs\ChipArmamentFormDef\Presets.xml"
$comboPath = Join-Path $modRoot "1.6\Content\Defs\ComboDef\Tomahawk.xml"
$thingLanguagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\ThingDef\BDP_Things.xml"
$comboLanguagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\ComboDef\Tomahawk.xml"
$gameplayLanguagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml"
$comboEntryConfigPath = Join-Path $modRoot "Source\BDP\Core\Combos\Config\ComboExpressionEntryConfig.cs"

foreach ($path in @($projectilePath, $armamentPath, $comboPath, $thingLanguagePath, $comboLanguagePath, $gameplayLanguagePath, $comboEntryConfigPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少战斧相关文件：$path"
}

$projectileXml = [xml](Get-Utf8Text $projectilePath)
$armamentXml = [xml](Get-Utf8Text $armamentPath)
$comboXml = [xml](Get-Utf8Text $comboPath)
$thingLanguageXml = [xml](Get-Utf8Text $thingLanguagePath)
$comboLanguageXml = [xml](Get-Utf8Text $comboLanguagePath)
$gameplayLanguageXml = [xml](Get-Utf8Text $gameplayLanguagePath)
$comboEntryConfigText = Get-Utf8Text $comboEntryConfigPath

$expectedDamage = @{
    'BDP_Projectile_Normal' = 13
    'BDP_Projectile_Variable' = 8
    'BDP_Projectile_Tracking' = 10
    'BDP_Projectile_Explosive' = 13
    'BDP_Projectile_ArmorPiercing' = 26
}

foreach ($defName in $expectedDamage.Keys) {
    $projectile = Get-DefNode $projectileXml $defName
    Assert-True ($null -ne $projectile) "缺少投射物 Def：$defName"
    Assert-True ([int]$projectile.projectile.damageAmountBase -eq $expectedDamage[$defName]) "投射物伤害不正确：$defName"
}

$tomahawkProjectile = Get-DefNode $projectileXml 'BDP_Projectile_Variable_Explosive'
Assert-True ($null -ne $tomahawkProjectile) '缺少变化炸裂弹投射物 Def。'
Assert-True ($tomahawkProjectile.thingClass -eq 'BDP.Core.Projectiles.BdpProjectile') '变化炸裂弹必须使用统一 BDP 投射物宿主。'
Assert-True ($tomahawkProjectile.projectile.damageDef -eq 'Bomb') '变化炸裂弹必须使用 Bomb 伤害类型。'
Assert-True ([int]$tomahawkProjectile.projectile.damageAmountBase -eq 8) '变化炸裂弹伤害必须为 8。'
Assert-True ([math]::Abs(([double]$tomahawkProjectile.projectile.armorPenetrationBase) - 0.05) -lt 0.0001) '变化炸裂弹护甲穿透必须沿用毒蛇的 0.05。'
Assert-True ([math]::Abs(([double]$tomahawkProjectile.projectile.stoppingPower) - 1) -lt 0.0001) '变化炸裂弹停止力必须沿用毒蛇的 1。'
Assert-True ([int]$tomahawkProjectile.projectile.speed -eq 140) '变化炸裂弹速度必须为毒蛇的 70%。'
Assert-True ([math]::Abs(([double]$tomahawkProjectile.projectile.explosionRadius) - 2.9) -lt 0.0001) '变化炸裂弹爆炸半径必须沿用美特拉的 2.9。'

$trionCube = Get-DefNode $armamentXml 'BDP_ArmamentForm_TrionCube'
Assert-True ($null -ne $trionCube) '缺少 Trion 立方体默认武装构型。'
Assert-True ([int]$trionCube.overrides.burstShotCount -eq 4) 'Trion 立方体必须保留四发覆盖。'
Assert-True ($null -eq $trionCube.SelectSingleNode('./projectileOverrides/damageMultiplier')) 'Trion 立方体不得继续覆盖子弹伤害。'

$combo = Get-DefNode $comboXml 'BDP_Combo_Tomahawk'
Assert-True ($null -ne $combo) '缺少战斧组合技 Def。'
Assert-True ($combo.firstSourceActionDefName -eq 'BDP_Preset_Viper') '战斧第一来源必须是毒蛇。'
Assert-True ($combo.secondSourceActionDefName -eq 'BDP_Preset_Meteora') '战斧第二来源必须是美特拉。'

foreach ($admissionName in @('FirstSourceAdmission', 'SecondSourceAdmission')) {
    $admission = $combo.SelectSingleNode("./$admissionName")
    Assert-True ($null -ne $admission) "战斧缺少来源准入：$admissionName"
    Assert-True (@($admission.AllowedProfessions.li) -contains 'BDP_ChipProfession_Shooter') "战斧 $admissionName 必须允许射手。"
    Assert-True (@($admission.AllowedCategories.li) -contains 'BDP_ChipCategory_Weapon') "战斧 $admissionName 必须要求武装分类。"
    Assert-True (@($admission.AllowedProfessions.li) -notcontains 'BDP_ChipProfession_Gunner') "战斧 $admissionName 不得允许枪手。"
}

$entry = $combo.SelectSingleNode("./Expression/Entries/li[Id='tomahawk_primary']")
Assert-True ($null -ne $entry) '缺少战斧主远程表达条目。'
Assert-True ($entry.VerbProps.defaultProjectile -eq 'BDP_Projectile_Variable_Explosive') '战斧必须使用变化炸裂弹。'
Assert-True (@($entry.RangedModules.li.moduleDef) -contains 'BDP_RoutePathModule') '战斧必须沿用毒蛇路线模块。'
Assert-True ($entry.VerbPropsResolve.RangeResolve -eq 'FollowFirstSource') '战斧射程必须跟随毒蛇。'
Assert-True ($entry.VerbPropsResolve.WarmupTimeResolve -eq 'FollowFirstSource') '战斧预热必须跟随毒蛇。'
Assert-True ($entry.TrionResolve.UseCostResolve -eq 'FollowFirstSource') '战斧 Trion 使用费用必须跟随毒蛇。'

$tomahawkLabel = Get-LanguageNode $comboLanguageXml 'BDP_Combo_Tomahawk.label'
Assert-True ($null -ne $tomahawkLabel -and $tomahawkLabel.InnerText -eq '战斧') '战斧必须使用简体中文名称。'
$tomahawkEntry = $combo.SelectSingleNode("./Expression/Entries/li[Id='tomahawk_primary']")
Assert-True ($tomahawkEntry.DisplayLabelKey -eq 'BDP_Expression_Tomahawk') '战斧表达条目必须使用稳定的显示名称键。'
$tomahawkExpressionLabel = Get-LanguageNode $gameplayLanguageXml 'BDP_Expression_Tomahawk'
Assert-True ($null -ne $tomahawkExpressionLabel -and $tomahawkExpressionLabel.InnerText -eq '战斧') '战斧表达条目必须显示简体中文名称。'
Assert-True ($comboEntryConfigText -match 'DisplayLabelKey\s*=\s*DisplayLabelKey') '组合表达映射必须传递显示名称键。'
$projectileLabel = Get-LanguageNode $thingLanguageXml 'BDP_Projectile_Variable_Explosive.label'
Assert-True ($null -ne $projectileLabel -and $projectileLabel.InnerText -eq '变化炸裂弹') '变化炸裂弹必须使用简体中文名称。'

Write-Output 'TomahawkProjectileBalanceSmokeTests PASS'
