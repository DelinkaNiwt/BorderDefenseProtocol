$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$guardRoot = Join-Path $sourceRoot 'BDP.Content\LightSoul'
$verbPath = Join-Path $guardRoot 'Verb_LightSoulGuardWatch.cs'
$drawPatchPath = Join-Path $guardRoot 'Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_LightSoulGuardWatch.cs'

Assert-True (Test-Path -LiteralPath $verbPath) '缺少光魂注视警戒 Verb。'
Assert-True (Test-Path -LiteralPath $drawPatchPath) '缺少光魂注视警戒连续瞄准绘制补丁。'

$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding UTF8
$drawPatchText = Get-Content -LiteralPath $drawPatchPath -Raw -Encoding UTF8

Assert-True ($verbText -match 'TryGetCurrentWatchTarget\s*\(\s*out LocalTargetInfo target\s*\)') '注视警戒 Verb 必须提供统一的当前警戒目标查询。'
Assert-True ($verbText -match 'curJob\?\.def\s*==\s*LightSoulGuardDefOf\.BDP_LightSoulGuardWatch') '统一查询必须优先读取当前手动警戒作业。'
Assert-True ($verbText -match 'curJob\.verbToUse\s*==\s*this') '手动警戒目标必须属于当前正式 Verb。'
Assert-True ($verbText -match 'CanHitTarget\(manualTarget\)') '手动目标只有当前可注视时才能驱动连续瞄准视觉。'
Assert-True ($verbText -match 'TryGetAutomaticWatchTarget\(out target\)') '没有可用手动目标时必须回退到自动警戒目标。'

Assert-True ($drawPatchText -match 'HarmonyPatch\(typeof\(PawnRenderUtility\),\s*"DrawEquipmentAiming"\)') '连续瞄准必须接在原版 DrawEquipmentAiming 边界。'
Assert-True ($drawPatchText -match 'HarmonyPriority\(Priority\.First\)') '警戒绘制补丁必须先于 BDP 通用视觉采样执行。'
Assert-True ($drawPatchText -match 'ref Vector3 drawLoc' -and $drawPatchText -match 'ref float aimAngle') '补丁必须真正改写原版绘制位置和角度。'
Assert-True ($drawPatchText -match 'TryGetCurrentWatchTarget\(out LocalTargetInfo target\)') '绘制补丁必须只读取正式 Verb 给出的当前有效目标。'
Assert-True ($drawPatchText -match 'new Vector3\(0f,\s*0f,\s*-0\.11f\)') '必须精确反解原版朝北公开持械偏移。'
Assert-True ($drawPatchText -match 'new Vector3\(0\.22f,\s*0f,\s*-0\.22f\)') '必须精确反解原版朝东公开持械偏移。'
Assert-True ($drawPatchText -match 'new Vector3\(0f,\s*0f,\s*-0\.22f\)') '必须精确反解原版朝南公开持械偏移。'
Assert-True ($drawPatchText -match 'new Vector3\(-0\.22f,\s*0f,\s*-0\.22f\)') '必须精确反解原版朝西公开持械偏移。'
Assert-True ($drawPatchText -match '\.AngleFlat\(\)') '瞄准角必须由 Pawn 到目标的连续方向计算。'
Assert-True ($drawPatchText -match '0\.4f\s*\+\s*eq\.def\.equippedDistanceOffset') '瞄准距离必须沿用原版装备距离公式。'
Assert-True ($drawPatchText -match '\.RotatedBy\(aimAngle\)') '瞄准位移必须随连续目标角旋转。'
Assert-True ($drawPatchText -match 'ParentHolder\s+is\s+Pawn_EquipmentTracker') 'Content 补丁必须从原版装备持有链解析 Pawn。'
Assert-True ($drawPatchText -notmatch 'triggerBody\?\.OwnerPawn') 'Content 不得读取 Core 程序集内部可见的 CompTriggerBody.OwnerPawn。'
Assert-True ($drawPatchText -notmatch 'SetStance|Stance_Busy|TryStartCastOn|TryCastShot') '连续瞄准视觉不得建立攻击或忙碌状态。'

Write-Output 'LightSoulGuardAimVisualSmokeTests PASS'
