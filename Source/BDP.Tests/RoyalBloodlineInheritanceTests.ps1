$ErrorActionPreference = 'Stop'

# 用系统 .NET Framework（运行时）编译并执行真实程序集测试，避免加载游戏界面。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies'
$output = Join-Path ([IO.Path]::GetTempPath()) ('bdp-royal-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    # 原版新运行时带 Span（连续内存视图）元数据；本夹具不使用它，仅屏蔽旧编译器的此项元数据提示。
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /nowarn:1684 /target:exe "/out:$output" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\UnityEngine.CoreModule.dll" "/reference:$harmony\0Harmony.dll" (Join-Path $PSScriptRoot 'RoyalBloodlineInheritanceTests.cs')
    if ($LASTEXITCODE -ne 0) { throw '皇室遗传测试编译失败。' }
    & $output $managed $harmony (Join-Path $modRoot '1.6\Assemblies')
    if ($LASTEXITCODE -ne 0) { throw '皇室遗传行为测试失败。' }
    # 同步核对实际配置，避免代码放行的对象与正式基因身份条件不一致。
    [xml]$gene = Get-Content -LiteralPath (Join-Path $modRoot '1.6\Content\Defs\GeneDef\RoyalBloodline.xml') -Raw -Encoding utf8
    if ($gene.Defs.GeneDef.defName -ne 'BDP_Gene_RoyalBloodline' -or [int]$gene.Defs.GeneDef.biostatArc -le 0 -or $gene.Defs.GeneDef.canGenerateInGeneSet -ne 'false' -or [float]$gene.Defs.GeneDef.selectionWeight -ne 0) { throw '皇室身份基因配置与传播边界不一致。' }
    [xml]$language = Get-Content -LiteralPath (Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\GeneDef\RoyalBloodline.xml') -Raw -Encoding utf8
    [xml]$keyed = Get-Content -LiteralPath (Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\Keyed\RoyalBloodline.xml') -Raw -Encoding utf8
    if (-not $language.LanguageData.'BDP_Gene_RoyalBloodline.label' -or -not $language.LanguageData.'BDP_Gene_RoyalBloodline.description' -or -not $keyed.LanguageData.BDP_RoyalBloodlineInheritancePatchMismatch) { throw '皇室基因语言包不完整。' }
    Write-Output '通过：正式皇室基因配置与简体中文语言包。'
} finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
