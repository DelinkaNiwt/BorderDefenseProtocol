param([Parameter(Mandatory)][ValidateSet('1.6','1.7')][string]$RimWorldVersion)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
. (Join-Path $PSScriptRoot 'TestSupport/RangedVersionTestSupport.ps1')
Initialize-RangedVersionTest -RimWorldVersion $RimWorldVersion
$coreAssembly = $script:RangedCore

$snapshotType = $coreAssembly.GetType("BDP.Core.Combos.ComboSourceAdmissionSnapshot", $true)
$scopeType = $coreAssembly.GetType("BDP.Core.Combos.ComboSourceScopeContract", $true)
$admissionType = $coreAssembly.GetType("BDP.Core.Combos.ComboSourceAdmissionContract", $true)
$indexType = $coreAssembly.GetType("BDP.Core.Expressions.Runtime.ComboRuntimeIndex", $true)
$flags = [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Static
$matchesMethod = $indexType.GetMethod("MatchesAssignmentWithScope", $flags)
Assert-True ($null -ne $matchesMethod) "Combo 索引缺少带形态姿态范围的来源分配求值方法。"

function Set-Field
{
    param($Target, [string]$Name, $Value)
    $Target.GetType().GetField($Name, [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Instance).SetValue($Target, $Value)
}

function New-StringList
{
    param([string[]]$Values)
    $list = [Collections.Generic.List[string]]::new()
    foreach ($value in $Values) { $list.Add($value) }
    return ,$list.AsReadOnly()
}

function New-Snapshot
{
    param(
        [string]$Profession,
        [string]$Category,
        [string]$SourceVariant,
        [string]$ModeKey = $null,
        [string]$StanceKey = $null,
        [bool]$HasRuntimeState = $false)
    $snapshot = [Activator]::CreateInstance($snapshotType, $true)
    Set-Field $snapshot "ProfessionKey" $Profession
    Set-Field $snapshot "CategoryKey" $Category
    Set-Field $snapshot "TagKeys" (New-StringList @())
    Set-Field $snapshot "SourceVariantKey" $SourceVariant
    Set-Field $snapshot "ModeKey" $ModeKey
    Set-Field $snapshot "StanceKey" $StanceKey
    Set-Field $snapshot "HasRuntimeState" $HasRuntimeState
    return $snapshot
}

function New-Admission
{
    $admission = [Activator]::CreateInstance($admissionType, $true)
    Set-Field $admission "AllowedProfessions" (New-StringList @())
    Set-Field $admission "DeniedProfessions" (New-StringList @())
    Set-Field $admission "AllowedCategories" (New-StringList @())
    Set-Field $admission "DeniedCategories" (New-StringList @())
    Set-Field $admission "AllowedTags" (New-StringList @())
    Set-Field $admission "RequiredTags" (New-StringList @())
    Set-Field $admission "DeniedTags" (New-StringList @())
    Set-Field $admission "AllowedSourceVariants" (New-StringList @())
    Set-Field $admission "DeniedSourceVariants" (New-StringList @())
    return $admission
}

function New-Scope
{
    param([string]$ModeKey, [string]$StanceKey = $null)
    $scope = [Activator]::CreateInstance($scopeType, $true)
    Set-Field $scope "ModeKey" $ModeKey
    Set-Field $scope "StanceKey" $StanceKey
    return $scope
}

$emptyAdmission = New-Admission
$defenseScope = New-Scope "defense"
$heavyBladeScope = New-Scope "attack" "heavy_blade"
$firstIdentity = "LightSoul"
$secondIdentity = "Thruster"
$defense = New-Snapshot "Profession" "Category" "variant" "defense" $null $true
$heavyBlade = New-Snapshot "Profession" "Category" "variant" "attack" "heavy_blade" $true
$punch = New-Snapshot "Profession" "Category" "variant" "attack" "punch" $true
$thruster = New-Snapshot "Profession" "Category" "variant"

function Invoke-ScopeMatch
{
    param($Snapshot, $Scope)
    return [bool]$matchesMethod.Invoke($null, @(
        $firstIdentity,
        $secondIdentity,
        $Snapshot,
        $thruster,
        $firstIdentity,
        $secondIdentity,
        $emptyAdmission,
        $emptyAdmission,
        $Scope,
        $null,
        $true))
}

Assert-True (Invoke-ScopeMatch $defense $defenseScope) "防护形态必须匹配光魂推进。"
Assert-True (-not (Invoke-ScopeMatch $heavyBlade $defenseScope)) "进攻形态的重刃姿态必须拒绝匹配光魂推进。"
Assert-True (-not (Invoke-ScopeMatch $punch $defenseScope)) "进攻形态的冲拳姿态必须拒绝匹配光魂推进。"
Assert-True (Invoke-ScopeMatch $heavyBlade $heavyBladeScope) "指定进攻姿态时，对应重刃必须通过。"
Assert-True (-not (Invoke-ScopeMatch $punch $heavyBladeScope)) "指定进攻姿态时，冲拳必须拒绝。"
Write-Host "PASS: $RimWorldVersion 组合来源范围允许防护并拒绝进攻两姿态，姿态范围仍准确。"
