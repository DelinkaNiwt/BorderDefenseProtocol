$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Contains {
    param(
        [string]$Text,
        [string]$Expected,
        [string]$Message
    )

    Assert-True ($Text.Contains($Expected)) $Message
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$loadoutPath = Join-Path $sourceRoot 'BDP.Content\Trigger\UI\TriggerLoadoutPanelProvider.cs'
$assemblyPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\Panel_SlotLayout.cs'
$loadout = Get-Content -LiteralPath $loadoutPath -Raw -Encoding utf8
$assembly = Get-Content -LiteralPath $assemblyPath -Raw -Encoding utf8

Assert-Contains $loadout 'DrawPairedSlotConnectors(' `
    '芯片管理面板必须绘制主副槽关联线。'
Assert-Contains $loadout 'BindingPartnerSide' `
    '芯片管理面板必须按绑定关系确定关联槽位。'
Assert-Contains $loadout 'BindingPartnerIndex' `
    '芯片管理面板必须按绑定伙伴索引定位关联槽位。'
Assert-True ($loadout -notmatch 'MirrorBorderColor') `
    '镜像槽不能再用专属蓝色边框覆盖绑定芯片的状态颜色。'

$connectorCall = $loadout.IndexOf('DrawPairedSlotConnectors(', [System.StringComparison]::Ordinal)
$rowCall = $loadout.IndexOf('DrawSideRow(', [System.StringComparison]::Ordinal)
Assert-True ($connectorCall -ge 0 -and $rowCall -gt $connectorCall) `
    '芯片管理面板关联线必须先于槽位格绘制，以免压住芯片图标。'

$stageStart = $assembly.IndexOf('private void DrawAssemblyStage(', [System.StringComparison]::Ordinal)
$assemblyConnectorCall = $assembly.IndexOf('DrawPairedSlotConnectors(', $stageStart, [System.StringComparison]::Ordinal)
$portraitCall = $assembly.IndexOf('DrawOperatorPortrait(centerRect, operatorPortrait);', $stageStart, [System.StringComparison]::Ordinal)
$mainColumnCall = $assembly.IndexOf('DrawSlotColumn(', $portraitCall, [System.StringComparison]::Ordinal)
Assert-True ($stageStart -ge 0 -and $assemblyConnectorCall -gt $stageStart) `
    '装配台必须在舞台区域绘制主副槽关联线。'
Assert-True ($portraitCall -gt $assemblyConnectorCall -and $mainColumnCall -gt $portraitCall) `
    '装配台关联线必须绘制在角色贴图和槽位卡片之前。'
Assert-Contains $assembly 'BindingPartnerSide' `
    '装配台必须按真实绑定关系绘制关联线。'
Assert-Contains $assembly 'BindingPartnerIndex' `
    '装配台必须按绑定伙伴索引定位关联槽位。'

Assert-Contains $loadout 'ResolveBindingRoot(slot, reader)' `
    '成对两侧边框必须使用绑定根槽的状态。'
Assert-Contains $loadout 'Widgets.DrawLine(' `
    '芯片管理面板必须实际绘制细线，而不只记录绑定数据。'
Assert-Contains $assembly 'Widgets.DrawLine(start, end, PairedLinkColor, 1f)' `
    '装配台必须实际绘制低对比关联线。'

Write-Output 'PairedSlotLinkVisual PASS'
