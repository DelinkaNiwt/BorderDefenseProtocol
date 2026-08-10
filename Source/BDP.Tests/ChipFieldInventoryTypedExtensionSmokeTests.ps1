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
$inventoryPath = Join-Path $repoRoot 'docs\需求说明\2026-04-24-BDP标准芯片XML字段全表.md'

$inventoryText = Get-Content -LiteralPath $inventoryPath -Raw -Encoding utf8

Assert-True (
    ($inventoryText -notmatch '<BlockName>') -and
    ($inventoryText -notmatch '<TargetSystem>') -and
    ($inventoryText -notmatch '<PayloadText>') -and
    ($inventoryText -notmatch 'ChipExtensionBlockConfig')
) 'The standard chip field inventory must remove the legacy free-text extension schema.'

Assert-True (
    $inventoryText -match
        'Class="BDP\.Content\.具体业务\.具体芯片扩展"'
) 'The field inventory must demonstrate RimWorld Class-based typed extension authoring.'

Assert-True (
    $inventoryText -match
        '`ChipProfileConfig（芯片画像块）`：1'
) 'The field inventory must reflect the single registered chip category field.'

Assert-True (
    ($inventoryText -match '共 \*\*75 个\*\*') -and
    ($inventoryText -match '\*\*合计：75\*\*') -and
    ($inventoryText -match '`ChipDefinitionConfig（芯片主扩展）`：6') -and
    ($inventoryText -match '`ChipTrionConfig（芯片资源块）`：2') -and
    ($inventoryText -match '`TrionIntensityRequirement（释放力条件）`：1') -and
    ($inventoryText -match '`SkillLevelRequirement（技能等级条件）`：2') -and
    ($inventoryText -match '`ExpressionSustainCostBySourceCountConfig（持续费用档位单项）`：2') -and
    ($inventoryText -match '`ChipExpressionModeConfig（形态块）`：4') -and
    ($inventoryText -notmatch '72 个字段')
) 'The standard chip schema must report 75 fixed BDP fields or nodes after adding activation requirements.'

Assert-True (
    ($inventoryText -match '<ActivationRequirements>') -and
    ($inventoryText -match 'Class="BDP\.Core\.Requirements\.TrionIntensityRequirement"') -and
    ($inventoryText -match '<Minimum>1</Minimum>') -and
    ($inventoryText -match 'Class="BDP\.Core\.Requirements\.SkillLevelRequirement"') -and
    ($inventoryText -notmatch '<PowerRequirement>')
) 'The field inventory must document ordered activation requirements and remove the obsolete power field.'

Assert-True (
    ($inventoryText -match '<DefaultModeKey>') -and
    ($inventoryText -match '<DisplayLabel>替代形态</DisplayLabel>') -and
    ($inventoryText -match '<GizmoIconTexPath>') -and
    ($inventoryText -match '<ActiveEntryIds>') -and
    ($inventoryText -notmatch '<InitialModeKey>') -and
    ($inventoryText -notmatch '<Operations>') -and
    ($inventoryText -notmatch 'ChipExpressionModeOperationConfig')
) 'The field inventory must document expression-owned default mode, presentation metadata, and ActiveEntryIds only.'

Assert-True (
    ($inventoryText -match '<SlotRegion>MainSub</SlotRegion>') -and
    ($inventoryText -notmatch '<SidePolicy>') -and
    ($inventoryText -notmatch 'HandsOnly|SpecialOnly')
) 'The standard chip field inventory must document the required SlotRegion schema only.'

Assert-True (
    ($inventoryText -match '<SlotOccupancy>Single</SlotOccupancy>') -and
    ($inventoryText -match 'PairedHands（成对主副槽）') -and
    ($inventoryText -notmatch '<IsDualWieldBinding>')
) 'The standard chip field inventory must document the required SlotOccupancy schema only.'

Assert-True (
    ($inventoryText -match '<ActivationExclusionGroups>') -and
    ($inventoryText -match '<li>BDP_ExampleExclusionGroup</li>') -and
    ($inventoryText -match 'ChipExclusionGroupDef（芯片互斥组定义）') -and
    ($inventoryText -match '只限制芯片能否同时处于启用状态，不限制它们一起装入触发体') -and
    ($inventoryText -match '空项或重复引用属于定义错误') -and
    ($inventoryText -notmatch '<ExclusionTags>')
) 'The standard chip field inventory must document controlled activation exclusion group references only.'

Write-Output 'ChipFieldInventoryTypedExtensionSmokeTests PASS'
