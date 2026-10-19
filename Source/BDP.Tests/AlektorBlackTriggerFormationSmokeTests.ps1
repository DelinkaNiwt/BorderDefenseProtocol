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
$catalogPath = Join-Path $sourceRoot 'BDP.Content\Trigger\Black\Formation\Snapshot\BlackTriggerRecordCatalog.cs'

[xml]$triggerDocument = Get-Content -LiteralPath $triggerPath -Raw -Encoding UTF8
$catalogSource = Get-Content -LiteralPath $catalogPath -Raw -Encoding UTF8

$alektor = $triggerDocument.Defs.SelectSingleNode("ThingDef[defName='BDP_TriggerBody_Alektor']")
$alektorExtension = $alektor.SelectSingleNode("modExtensions/li[@Class='BDP.Content.Trigger.Black.Formation.BlackTriggerFormationExtension']")
$routes = @($alektorExtension.SelectNodes('formationRoutes/li'))

Assert-True ($null -ne $alektorExtension) '卵之冠必须声明黑触发器形成扩展。'
Assert-True ($routes.Count -eq 1) '卵之冠必须且只能声明一条形成路线。'

$route = $routes[0]
$allRecords = @($route.SelectNodes('allOf/li/record') | ForEach-Object { $_.'#text' })
$anySkills = @($route.SelectNodes('anyOf/li/skill') | ForEach-Object { $_.'#text' })

Assert-True ($route.emphasis -eq 'BDP_BlackTriggerEmphasis_Frontline') '卵之冠形成路线侧重必须是正面压制。'
Assert-True ($allRecords.Count -eq 2 -and $allRecords -contains 'ThingsDeconstructed' -and $allRecords -contains 'ShotsFired') `
    '卵之冠必须同时要求拆除物品数与开火射击次数进入前三。'
Assert-True ([int]$route.anyOfMinimum -eq 1) '卵之冠必须在手工与建造技能中至少命中一项。'
Assert-True ($anySkills.Count -eq 2 -and $anySkills -contains 'Crafting' -and $anySkills -contains 'Construction') `
    '卵之冠必须要求手工或建造技能进入自身最高三项。'
Assert-True ($null -eq $route.noneOf) '卵之冠形成路线不应增加排斥事实。'

$adaptedAbilities = @($alektorExtension.SelectNodes('adaptedAbilities/li'))
Assert-True ($adaptedAbilities.Count -eq 1 `
    -and $adaptedAbilities[0].abilityDefName -eq 'PsychicSlaughter' `
    -and $adaptedAbilities[0].sourcePackageId -eq 'ludeon.rimworld.anomaly') `
    '卵之冠必须声明心灵宰杀的可选能力适应权重。'

$eligibilityGroups = @($alektorExtension.SelectNodes('eligibilityGroups/li'))
Assert-True ($eligibilityGroups.Count -eq 1) '卵之冠必须声明唯一的适格排斥条件组。'
$eligibilityGroup = $eligibilityGroups[0]
$excludedConditions = @($eligibilityGroup.SelectNodes('noneOf/li'))
Assert-True ($null -eq $eligibilityGroup.allOf -and $null -eq $eligibilityGroup.anyOf) `
    '卵之冠适格条件不得保留医疗或社交正向筛选。'
Assert-True ($excludedConditions.Count -eq 1 `
    -and $excludedConditions[0].kind -eq 'SkillDisabled' `
    -and $excludedConditions[0].skill -eq 'Shooting') `
    '卵之冠只能排斥射击技能被完全禁用的人物。'

Assert-True ($catalogSource -match '\{\s*"ThingsDeconstructed"\s*,') '拆除物品数必须存在于形成记录白名单。'
Assert-True ($catalogSource -match '\{\s*"ShotsFired"\s*,') '开火射击次数必须存在于形成记录白名单。'

$fujin = $triggerDocument.Defs.SelectSingleNode("ThingDef[defName='BDP_TriggerBody_Fujin']")
$fujinExtension = $fujin.SelectSingleNode("modExtensions/li[@Class='BDP.Content.Trigger.Black.Formation.BlackTriggerFormationExtension']")
$fujinRoutes = @($fujinExtension.SelectNodes('formationRoutes/li'))
$fujinExcluded = @($fujinExtension.SelectNodes('eligibilityGroups/li/noneOf/li'))
Assert-True ($fujinRoutes.Count -eq 1 -and $fujinRoutes[0].emphasis -eq 'BDP_BlackTriggerEmphasis_Precision') `
    '本次卵之冠定稿不得修改风刃的精确猎杀路线。'
Assert-True ($fujinExcluded.Count -eq 1 `
    -and $fujinExcluded[0].kind -eq 'SkillDisabled' `
    -and $fujinExcluded[0].skill -eq 'Shooting') `
    '本次卵之冠定稿不得修改风刃的适格排斥条件。'

Write-Output 'AlektorBlackTriggerFormationSmokeTests PASS'
exit 0
