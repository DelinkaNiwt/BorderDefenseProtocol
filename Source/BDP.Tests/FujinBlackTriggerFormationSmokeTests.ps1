$ErrorActionPreference = 'Stop'

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
$triggerPath = Join-Path $repoRoot '1.6\Content\Defs\ThingDef\Equipment\Trigger.xml'
$formationRoot = Join-Path $sourceRoot 'BDP.Content\Trigger\Black\Formation'
$conditionPath = Join-Path $formationRoot 'Defs\BlackTriggerEligibilityCondition.cs'
$evaluatorPath = Join-Path $formationRoot 'Binding\BlackTriggerCompatibilityEvaluator.cs'
$validatorPath = Join-Path $formationRoot 'Validation\BlackTriggerFormationDefValidator.cs'

[xml]$triggerDocument = Get-Content -LiteralPath $triggerPath -Raw -Encoding UTF8
$conditionSource = Get-Content -LiteralPath $conditionPath -Raw -Encoding UTF8
$evaluatorSource = Get-Content -LiteralPath $evaluatorPath -Raw -Encoding UTF8
$validatorSource = Get-Content -LiteralPath $validatorPath -Raw -Encoding UTF8

$fujin = $triggerDocument.Defs.SelectSingleNode("ThingDef[defName='BDP_TriggerBody_Fujin']")
$fujinExtension = $fujin.SelectSingleNode("modExtensions/li[@Class='BDP.Content.Trigger.Black.Formation.BlackTriggerFormationExtension']")
$routes = @($fujinExtension.SelectNodes('formationRoutes/li'))

Assert-True ($null -ne $fujinExtension) '风刃必须声明黑触发器形成扩展。'
Assert-True ($routes.Count -eq 1) '风刃必须且只能声明一条形成路线。'

$route = $routes[0]
$allSkills = @($route.SelectNodes('allOf/li/skill') | ForEach-Object { $_.'#text' })
$anyRecords = @($route.SelectNodes('anyOf/li/record') | ForEach-Object { $_.'#text' })

Assert-True ($route.emphasis -eq 'BDP_BlackTriggerEmphasis_Precision') '风刃形成路线侧重必须是精确猎杀。'
Assert-True ($allSkills.Count -eq 1 -and $allSkills[0] -eq 'Melee') '风刃形成路线必须要求格斗进入最高三项技能。'
Assert-True ([int]$route.anyOfMinimum -eq 1) '风刃形成路线必须要求两项生涯记录至少命中一项。'
Assert-True ($anyRecords.Count -eq 2 -and $anyRecords -contains 'Headshots' -and $anyRecords -contains 'ShotsFired') `
    '风刃形成路线必须在爆头次数与开火射击次数中至少命中一项。'
Assert-True ($null -eq $route.noneOf) '风刃形成路线不应增加生涯记录排斥条件。'

$eligibilityGroups = @($fujinExtension.SelectNodes('eligibilityGroups/li'))
Assert-True ($eligibilityGroups.Count -eq 1) '风刃必须声明唯一的适格排斥条件组。'
$eligibilityGroup = $eligibilityGroups[0]
$excludedConditions = @($eligibilityGroup.SelectNodes('noneOf/li'))
Assert-True ($null -eq $eligibilityGroup.allOf -and $null -eq $eligibilityGroup.anyOf) `
    '风刃适格条件不得保留正向技能筛选。'
Assert-True ($excludedConditions.Count -eq 1 `
    -and $excludedConditions[0].kind -eq 'SkillDisabled' `
    -and $excludedConditions[0].skill -eq 'Shooting') `
    '风刃只能排斥射击技能被完全禁用的人物。'

Assert-True ($conditionSource -match '\bSkillDisabled\b') '适格条件枚举必须提供通用 SkillDisabled 条件。'
Assert-True ($evaluatorSource -match 'case\s+BlackTriggerEligibilityConditionKind\.SkillDisabled:[\s\S]*?TotallyDisabled') `
    '适格求值器必须按 SkillRecord.TotallyDisabled 判断技能被完全禁用。'
Assert-True ($validatorSource -match 'case\s+BlackTriggerEligibilityConditionKind\.SkillDisabled:[\s\S]*?condition\.skill') `
    '适格定义校验器必须要求 SkillDisabled 声明 SkillDef。'

Write-Output 'FujinBlackTriggerFormationSmokeTests PASS'
exit 0
