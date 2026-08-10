$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message) if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$mainGenePath = Join-Path $root '1.6\Defs\Genes\Trion\GeneDefs_TrionGland.xml'
$statPartPath = Join-Path $root 'Source\BDP\Core\Genes\StatPart_TrionCapacityPotential.cs'
$eligibilityPath = Join-Path $root 'Source\BDP\Core\Genes\TrionGlandEligibility.cs'
$statDefsPath = Join-Path $root '1.6\Defs\Stats\Trion\StatDefs_Trion.xml'
Assert-True (Test-Path $mainGenePath) '主模组缺少正式 Trion 腺体基因。'
Assert-True (Test-Path $statPartPath) '缺少潜在容量 StatPart。'
Assert-True (Test-Path $eligibilityPath) '缺少有效腺体资格判定。'
$mainGeneDef = Get-Content -Raw $mainGenePath
$statPartText = Get-Content -Raw $statPartPath
$eligibilityText = Get-Content -Raw $eligibilityPath
$statDefsText = Get-Content -Raw $statDefsPath
Assert-True ($mainGeneDef -match '<selectionWeight>0</selectionWeight>') '正式基因不得进入随机基因池。'
Assert-True ($mainGeneDef -match '<canGenerateInGeneSet>false</canGenerateInGeneSet>') '正式基因不得随机进入基因组。'
Assert-True ($mainGeneDef -notmatch '700|测试用|验证') '正式基因不得保留测试语义。'
Assert-True ($mainGeneDef -match '<BDP_TrionRecoveryRate>500</BDP_TrionRecoveryRate>') '恢复应为 500/天。'
Assert-True ($statPartText -match 'TrionCapacityPotential') '容量属性必须读取潜在容量。'
Assert-True ($eligibilityText -match 'gene\.Active') '资格必须要求基因有效。'
Assert-True ($statDefsText -match '<defName>BDP_TrionIntensity</defName>') '正式腺体必须让原版角色属性页承接 Trion 释放力。'
Write-Host 'PASS: 正式 Trion 腺体解锁容量、恢复与释放力显示。'
