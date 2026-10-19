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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$appearanceRoot = Join-Path $sourceRoot 'BDP.Content\CombatBody\Appearance'
$windowPath = Join-Path $appearanceRoot 'Window_CombatBodyAppearanceEditor.cs'
$previewServicePath = Join-Path $appearanceRoot 'CombatBodyAppearancePreviewService.cs'
$previewStatePath = Join-Path $appearanceRoot 'CombatBodyAppearancePreviewState.cs'
$panelPath = Join-Path $appearanceRoot 'Panel_CombatBodyAppearanceTemplates.cs'
$windowsLanguagePath = Join-Path $sourceRoot '..\Languages\ChineseSimplified (简体中文)\Keyed\Windows.xml'
$messagesLanguagePath = Join-Path $sourceRoot '..\Languages\ChineseSimplified (简体中文)\Keyed\Messages.xml'

foreach ($path in @($windowPath, $previewServicePath, $previewStatePath, $panelPath, $windowsLanguagePath, $messagesLanguagePath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "战斗体编辑面板文件缺失：$path"
}

$window = Get-Content -LiteralPath $windowPath -Raw -Encoding utf8
$previewService = Get-Content -LiteralPath $previewServicePath -Raw -Encoding utf8
$previewState = Get-Content -LiteralPath $previewStatePath -Raw -Encoding utf8
$panel = Get-Content -LiteralPath $panelPath -Raw -Encoding utf8
$windowsLanguage = Get-Content -LiteralPath $windowsLanguagePath -Raw -Encoding utf8
$messagesLanguage = Get-Content -LiteralPath $messagesLanguagePath -Raw -Encoding utf8

Assert-True ($window -match 'Dialog_Rename|IRenameable') '重命名必须复用原版文本输入窗口。'
Assert-True ($window -match 'CombatBodyAppearanceTemplateService|UploadCurrentApparel') '窗口必须接入模板上传服务。'
Assert-True ($window -match 'MirrorOriginal|Preset|SetMode') '窗口必须支持镜像/预设模式切换。'
Assert-True ($window -match 'DestroyResources|RenderTexture|PostClose') '窗口关闭时必须释放预览纹理资源。'
Assert-True (-not ($window -match 'WriteAllBytes|EncodeToPNG|\.png')) '预览窗口不得写入 PNG 文件。'
Assert-True ($previewService -match 'CombatBodyFrontApparelFactory\.Create') '预览必须使用 Core 服装副本工厂。'
Assert-True ($previewService -match 'AssemblyPawnPortraitCapture\.Capture') '预览必须复用原版 Pawn 肖像捕获机制。'
Assert-True ($previewService -match 'finally') '预览失败也必须执行 Pawn 状态恢复。'
Assert-True ($previewService -match 'Destroy\s*\(') '临时服装副本必须在捕获后销毁。'
Assert-True ($previewState -match 'RenderTexture|AssemblyPawnPortraitSnapshot') '预览状态必须持有可释放的肖像快照。'
Assert-True ($previewState -match 'DestroyResources|Release') '预览状态必须提供资源释放入口。'
Assert-True ($panel -match '模板|Templates|Entries') '模板面板必须显示模板列表和服装条目。'
Assert-True ($panel -match 'Upload|Rename|Delete|新建|重命名|删除') '模板面板必须提供模板管理操作。'
Assert-True ($window -match '左|left|middle|right|Draw.*Panel') '窗口必须组织三栏面板布局。'
Assert-True ($windowsLanguage -match 'BDP_Window_CombatBodyAppearance') '窗口文案必须使用语言键。'
Assert-True ($messagesLanguage -match 'BDP_Message_CombatBodyAppearance') '编辑面板提示文案必须使用语言键。'

Write-Output 'CombatBodyAppearanceEditorWindowSmokeTests PASS'
