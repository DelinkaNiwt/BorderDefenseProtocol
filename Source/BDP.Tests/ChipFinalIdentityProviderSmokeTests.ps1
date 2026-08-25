$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$interfaceText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Chips\External\IChipSourceReferenceProvider.cs")
$snapshotText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Chips\External\ChipSourceReferenceSnapshot.cs")
$surfaceText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Chips\External\ChipInstanceSurfaceAccess.cs")
$manufacturedText = Get-Utf8Text (Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Thing\CompManufacturedChip.cs")

Assert-True ($interfaceText -match "SourceProfessionKey") "实例来源接口必须公开最终职业键。"
Assert-True ($snapshotText -match "SourceProfessionKey") "来源快照必须保存最终职业键。"
Assert-True ($surfaceText -match "SourceProfessionKey\s*=\s*provider\.SourceProfessionKey") "来源读取面必须复制最终职业键。"
Assert-True ($manufacturedText -match "SourceProfessionKey\s*=>\s*combinationRecord\?\.ProfessionDefName") "制造芯片必须直接公开成品最终职业。"
Assert-True ($manufacturedText -notmatch "SourceProfessionKey[\s\S]{0,200}acceptedActionProfessions") "最终职业不得通过兼容职业推导。"

Write-Host "PASS: 制造成品直接公开最终职业身份。"
