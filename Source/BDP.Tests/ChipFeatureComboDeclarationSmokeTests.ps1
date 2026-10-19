$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$comboRoot = Join-Path $repoRoot 'Common\Content\Defs\ComboDef'
$expectedRoots = @{
    Gimlet = @('BDP_Feature_RangedAttack')
    Hornet = @('BDP_Feature_RangedAttack')
    Tomahawk = @('BDP_Feature_RangedAttack')
    BounceSlash = @('BDP_Feature_GenericAbility')
    LightSoulPropulsion = @('BDP_Feature_GenericAbility')
    SenkuKogetsu = @('BDP_Feature_GenericAbility')
}
$expectedAbilityChildren = @{
    BounceSlash = @('BDP_Feature_Movement', 'BDP_Feature_MeleeAttack')
    LightSoulPropulsion = @('BDP_Feature_Movement')
    SenkuKogetsu = @('BDP_Feature_MeleeAttack')
}

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

function Get-Elements($node, [string]$name) {
    if ($null -eq $node) { return @() }
    return @($node.ChildNodes | Where-Object {
        $_.NodeType -eq 'Element' -and ($name -eq '*' -or $_.LocalName -eq $name)
    })
}

function Get-ContainerItems($node, [string]$containerName) {
    if ($null -eq $node) { return @() }
    return Get-Elements $node.SelectSingleNode($containerName) 'li'
}

function Assert-SourceClass($owner, [string]$path, [string[]]$allowedSuffixes) {
    $sources = @(Get-ContainerItems $owner 'Sources')
    Assert-True ($sources.Count -gt 0) "$path 缺少明确来源。"
    $matched = $false
    foreach ($source in $sources) {
        $sourceClass = [string]$source.GetAttribute('Class')
        Assert-True ($sourceClass.Contains('FeatureSourceConfig')) "$path 使用了非功能来源配置。"
        foreach ($suffix in $allowedSuffixes) {
            if ($sourceClass.EndsWith($suffix, [StringComparison]::Ordinal)) {
                $matched = $true
            }
        }
    }
    Assert-True $matched "$path 的来源类型与已声明功能语义不匹配：$($allowedSuffixes -join ', ')"
}

function Assert-FeatureSources($features, [string]$path) {
    foreach ($feature in $features) {
        $featureId = [string]$feature.Id
        $featureName = [string]$feature.Feature
        Assert-True (-not [string]::IsNullOrWhiteSpace($featureId)) "$path 存在空功能 Id。"
        Assert-True (-not [string]::IsNullOrWhiteSpace($featureName)) "$path/$featureId 缺少 FeatureDef。"

        switch ($featureName) {
            'BDP_Feature_RangedAttack' {
                Assert-SourceClass $feature "$path/$featureId" @('RangedAttackFeatureSourceConfig')
            }
            'BDP_Feature_GenericAbility' {
                Assert-SourceClass $feature "$path/$featureId" @('AbilityLifecycleFeatureSourceConfig')
            }
        }

        foreach ($group in (Get-ContainerItems $feature 'Groups')) {
            $groupName = [string]$group.Group
            switch ($groupName) {
                'BDP_FeatureGroup_MovementMethod' {
                    Assert-SourceClass $group "$path/$featureId/$($group.Id)" @('MovementMethodFeatureSourceConfig')
                }
                'BDP_FeatureGroup_MeleeAttackMethod' {
                    Assert-SourceClass $group "$path/$featureId/$($group.Id)" @(
                        'MeleeAttackMethodFeatureSourceConfig',
                        'AbilityMeleeAttackFeatureSourceConfig')
                }
            }
        }

        Assert-FeatureSources (Get-ContainerItems $feature 'Children') "$path/$featureId/children"
    }
}

foreach ($name in $expectedRoots.Keys) {
    $path = Join-Path $comboRoot ($name + '.xml')
    Assert-True (Test-Path -LiteralPath $path) "缺少组合技 XML：$name"
    $document = [xml](Get-Content -LiteralPath $path -Raw -Encoding UTF8)
    $combo = @(Get-Elements $document.Defs '*')[0]
    $features = @(Get-ContainerItems $combo 'Features')
    $actualRoots = @($features | ForEach-Object { [string]$_.Feature })
    Assert-True (($actualRoots -join '|') -eq ($expectedRoots[$name] -join '|')) `
        "$name 的根功能树不符合已审议语义。"
    Assert-FeatureSources $features $name

    if ($expectedAbilityChildren.ContainsKey($name)) {
        $ability = $features[0]
        $actualChildren = @(Get-ContainerItems $ability 'Children' | ForEach-Object { [string]$_.Feature })
        Assert-True (($actualChildren -join '|') -eq ($expectedAbilityChildren[$name] -join '|')) `
            "$name 的能力结果树不符合已审议语义。"
    }
}

Write-Host 'ChipFeatureComboDeclaration PASS'
