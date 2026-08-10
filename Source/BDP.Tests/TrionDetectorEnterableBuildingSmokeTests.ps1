$ErrorActionPreference = 'Stop'

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$buildingPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\Building_TrionDetector.cs'
$eligibilityPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\TrionTalentAssessmentEligibility.cs'
$defPath = Join-Path $root '1.6\Content\Defs\Buildings\Trion\ThingDefs_TrionDetector.xml'

$building = Get-Content -Raw $buildingPath
$eligibility = Get-Content -Raw $eligibilityPath
$def = Get-Content -Raw $defPath

Assert-True ($building -match 'Building_TrionDetector\s*:\s*Building_Enterable') '固定检测仪必须继承原版 Building_Enterable。'
Assert-True ($building -match 'Pawn\s+Occupant') '固定检测仪必须公开当前舱内受检者。'
Assert-True ($building -match 'override\s+AcceptanceReport\s+CanAcceptPawn') '固定检测仪必须实现原版受检资格入口。'
Assert-True ($building -match 'override\s+void\s+TryAcceptPawn') '固定检测仪必须实现原版入舱入口。'
Assert-True ($building -match 'void\s+EjectContents') '固定检测仪必须能安全弹出舱内受检者。'
Assert-True ($building -notmatch 'TrionTalentAssessmentFloatMenuUtility') '固定检测仪不得继续使用旧操作员右键检测入口。'
Assert-True ($eligibility -match 'CanSelectSubject\s*\(\s*Pawn\s+subject') '受检者资格必须可脱离操作员独立检查。'
Assert-True ($def -match '<hasInteractionCell>true</hasInteractionCell>') '固定检测仪必须声明原版交互格。'
Assert-True ($def -match '<tickerType>Normal</tickerType>') '固定检测仪必须使用 Normal tick 驱动进度显示。'
Assert-True ($def -match '<containedPawnsSelectable>true</containedPawnsSelectable>') '舱内角色必须保持可选。'

Write-Host 'PASS: 固定 Trion 检测仪使用原版可进入建筑边界。'
