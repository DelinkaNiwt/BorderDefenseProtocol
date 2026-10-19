# 芯片功能 XML 扩展冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$fixturePath = Join-Path $modRoot "Source\BDP.Tests\Fixtures\ChipFeatureShieldPatch.xml"
$featurePath = Join-Path $modRoot "1.6\Content\Defs\ChipFeatureDef\Features.xml"

Assert-True (Test-Path -LiteralPath $fixturePath) "缺少 XML 属性槽位扩展示例。"
$fixtureText = Get-Utf8Text $fixturePath
$featureText = Get-Utf8Text $featurePath

Assert-True ($fixtureText -match "PatchOperationAdd") `
    "扩展示例必须使用 RimWorld 原生 PatchOperationAdd。"
Assert-True ($fixtureText -match "BDP_Feature_Shield") `
    "扩展示例必须向护盾语义追加属性槽位。"
Assert-True ($fixtureText -match "OtherMod_Property_ShieldReflectionRatio") `
    "扩展示例必须包含第三方新属性。"
Assert-True ($fixtureText -notmatch "ChipInformationExtensionRegistry|Register\(") `
    "XML 属性扩展不得依赖旧注册表或启动时 Register。"
Assert-True ($featureText -match "Properties") "BDP 护盾 Def 必须拥有可被 PatchOperationAdd 追加的列表。"

Write-Host "PASS: 芯片功能属性槽位支持纯 XML Patch 扩展。"
