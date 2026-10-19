$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$developmentRoot = Join-Path $repoRoot 'Source\BDP.Development'
$featureXml = Join-Path $repoRoot '1.6\Development\Defs\ChipFeatureDef\ExtensionSample.xml'
$propertyXml = Join-Path $repoRoot '1.6\Development\Defs\ChipFeaturePropertyDef\ExtensionSample.xml'
$patchXml = Join-Path $repoRoot '1.6\Development\Patches\ChipFeatureExtensionSample.xml'
$workerPath = Join-Path $developmentRoot 'Information\Samples\CustomBarrierFeatureSourceWorker.cs'
$configPath = Join-Path $developmentRoot 'Information\Samples\CustomBarrierFeatureSourceConfig.cs'
$ruleDefPath = Join-Path $developmentRoot 'Information\Samples\CustomBarrierRuleDef.cs'
$ruleXmlPath = Join-Path $repoRoot '1.6\Development\Defs\CustomBarrierRuleDef\ExtensionSample.xml'
$ruleLanguagePath = Join-Path $repoRoot '1.6\Development\Languages\ChineseSimplified (简体中文)\DefInjected\BDP.Development.Information.Samples.CustomBarrierRuleDef\ExtensionSample.xml'
$coreSourcePath = Join-Path $repoRoot 'Source\BDP\Core\Information\Features\Config\ChipFeatureSourceConfig.cs'
$featureMergePath = Join-Path $repoRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipFeatureMergeService.cs'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True (Test-Path $featureXml) '缺少纯 XML 新功能语义样例。'
Assert-True (Test-Path $propertyXml) '缺少纯 XML 新属性样例。'
Assert-True (Test-Path $patchXml) '缺少 XML Patch 扩展示例。'
Assert-True ((Get-Content $patchXml -Raw -Encoding UTF8) -match 'BDP_Feature_Shield') 'Patch 必须向既有护盾追加属性槽位。'
Assert-True ((Get-Content $patchXml -Raw -Encoding UTF8) -match 'WorkerClass') 'Patch 必须通过 XML WorkerClass 挂接自定义实现。'
Assert-True ((Get-Content $configPath -Raw -Encoding UTF8) -match 'ChipFeatureSourceConfig') '自定义来源配置必须继承 Core 中性契约。'
Assert-True ((Get-Content $configPath -Raw -Encoding UTF8) -match 'RuleDefName') '来源配置应只引用权威业务 Def 身份。'
Assert-True ((Get-Content $configPath -Raw -Encoding UTF8) -notmatch 'ReflectionRatio\s*=|PropertyDefName\s*=') '来源配置不得复制业务值或属性身份。'
Assert-True (Test-Path $ruleDefPath) '自定义来源业务值必须有独立 Development 权威 Def。'
Assert-True ((Get-Content $ruleDefPath -Raw -Encoding UTF8) -match ': Def') '权威业务规则必须是 Development 自己的 Def 类型。'
Assert-True (Test-Path $ruleXmlPath) '缺少权威业务规则的 Development XML Def。'
Assert-True (Test-Path $ruleLanguagePath) '权威业务 Def 的玩家向文本必须进入简体中文语言包。'
Assert-True ((Get-Content $workerPath -Raw -Encoding UTF8) -match 'IChipFeatureSourceWorker') '自定义来源必须实现 Worker 契约。'
Assert-True ((Get-Content $workerPath -Raw -Encoding UTF8) -match 'DefDatabase<CustomBarrierRuleDef>') 'Worker 必须从权威 Def 读取规则值。'
Assert-True ((Get-Content $workerPath -Raw -Encoding UTF8) -match 'ChipInformationSourceKind\.Extension') '自定义来源必须保留 Extension 来源类型。'
Assert-True ((Get-Content $patchXml -Raw -Encoding UTF8) -notmatch '<EntryId>') '无需表达条目的自定义来源不应被迫填写 EntryId。'
Assert-True ((Get-Content $patchXml -Raw -Encoding UTF8) -match '<RuleDefName>') 'XML 来源声明必须只引用权威规则 Def。'
Assert-True ((Get-Content $patchXml -Raw -Encoding UTF8) -notmatch '<ReflectionRatio>') 'XML 功能来源声明不得重复维护业务规则值。'
Assert-True ((Get-Content $coreSourcePath -Raw -Encoding UTF8) -match 'MemberwiseClone') '统一来源复制必须保留自定义派生字段。'
Assert-True ((Get-Content $featureMergePath -Raw -Encoding UTF8) -match 'CloneForRemap\(') '制造合并必须通过统一来源复制入口。'

[xml]$feature = Get-Content $featureXml -Raw -Encoding UTF8
[xml]$property = Get-Content $propertyXml -Raw -Encoding UTF8
[xml]$rule = Get-Content $ruleXmlPath -Raw -Encoding UTF8
$patch = New-Object System.Xml.XmlDocument
$patch.Load($patchXml)
Assert-True ($feature.Defs.ChildNodes.Count -gt 0) '新功能 Def XML 不得为空。'
Assert-True ($property.Defs.ChildNodes.Count -gt 0) '新属性 Def XML 不得为空。'
Assert-True ($rule.Defs.ChildNodes.Count -gt 0) '权威业务规则 Def XML 不得为空。'
Assert-True ((Get-Content $ruleLanguagePath -Raw -Encoding UTF8) -match 'BDP_Development_CustomBarrierRule_Experimental\.label') '权威业务规则的本地化名称缺失。'

$shieldWorkerEntries = @($patch.SelectNodes('/Patch/Operation/value/li[Feature="BDP_Feature_Shield" and .//WorkerClass="BDP.Development.Information.Samples.CustomBarrierFeatureSourceWorker"]'))
Assert-True ($shieldWorkerEntries.Count -eq 1) '同义护盾扩展示例必须把自定义 Worker 实际绑定到 BDP_Feature_Shield。'
$shieldRuleDefName = $shieldWorkerEntries[0].SelectSingleNode('.//RuleDefName')
Assert-True ($shieldRuleDefName -ne $null -and $shieldRuleDefName.InnerText -eq 'BDP_Development_CustomBarrierRule_Experimental') `
    '护盾扩展 Worker 必须读取示例中唯一权威业务规则 Def。'

Write-Host 'ChipFeatureDevelopmentExtensionSample PASS'
