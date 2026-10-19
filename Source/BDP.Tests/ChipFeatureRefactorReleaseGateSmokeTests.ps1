$ErrorActionPreference = 'Stop'

$testRoot = $PSScriptRoot
$testNames = @(
    'ChipFeatureInformationGroupContractSmokeTests.ps1',
    'ChipFeatureFeatureLevelSubgroupSmokeTests.ps1',
    'ChipFeatureInformationGroupResolutionSmokeTests.ps1',
    'ChipFeatureSourceExtensionReliabilitySmokeTests.ps1',
    'ChipFeatureDefCatalogSmokeTests.ps1',
    'ChipFeatureStandardSourceSmokeTests.ps1',
    'ChipFeatureReviewedCatalogSmokeTests.ps1',
    'ChipFeatureReviewedValueProjectionSmokeTests.ps1',
    'ChipFeatureEnumLocalizationSmokeTests.ps1',
    'ChipFeatureContextIsolationSmokeTests.ps1',
    'ChipFeatureParserContextMergeSmokeTests.ps1',
    'ChipFeatureSemanticProjectionSourceSmokeTests.ps1',
    'ChipFeaturePresetDeclarationSmokeTests.ps1',
    'ChipFeatureReviewedDeclarationMigrationSmokeTests.ps1',
    'ChipFeatureMigrationCoverageSmokeTests.ps1',
    'ChipFeatureComboDeclarationSmokeTests.ps1',
    'ChipFeatureComboInformationAdapterSmokeTests.ps1',
    'ChipFeatureComboResolutionSmokeTests.ps1',
    'AbilityMeleeAttackFeatureSourceSmokeTests.ps1',
    'ChipCombinationResolverSmokeTests.ps1',
    'ChipInformationExtensionReferenceContractSmokeTests.ps1',
    'ComboChipInformationReferenceProjectionSmokeTests.ps1',
    'ChipFeatureInformationGroupRendererSmokeTests.ps1',
    'ChipFeatureAuthoringGuideSmokeTests.ps1',
    'ChipFeatureExecutionIsolationSmokeTests.ps1',
    'ChipFeatureContractSmokeTests.ps1',
    'ChipFeatureDefinitionValidationSmokeTests.ps1',
    'ChipFeatureContributionMergeSmokeTests.ps1',
    'ChipFeatureContextProjectionSmokeTests.ps1',
    'ChipFeatureManufacturingCloneSmokeTests.ps1',
    'ChipFeatureRendererFallbackSmokeTests.ps1',
    'ChipFeatureManufacturingPreviewSmokeTests.ps1',
    'ChipFeatureTriggerAssemblyPreviewSmokeTests.ps1',
    'NeutralChartToolsContractSmokeTests.ps1',
    'ChipFeatureDevelopmentExtensionSampleSmokeTests.ps1',
    'DevelopmentAssemblyFormalIndependenceSmokeTests.ps1',
    'DevelopmentBoundaryVerifierSmokeTests.ps1'
)

for ($index = 0; $index -lt $testNames.Count; $index++) {
    $testPath = Join-Path $testRoot $testNames[$index]
    if (-not (Test-Path -LiteralPath $testPath)) {
        throw "FAIL: 发布门禁引用了缺失测试：$testPath"
    }

    Write-Host "[$($index + 1)/$($testNames.Count)] $($testNames[$index])"
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $testPath
    if ($LASTEXITCODE -ne 0) {
        throw "FAIL: 发布门禁子测试失败：$($testNames[$index])，退出码 $LASTEXITCODE。"
    }
}

Write-Host "ChipFeatureRefactorReleaseGate PASS: $($testNames.Count) 项测试全部通过。"
