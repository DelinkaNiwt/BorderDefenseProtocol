# 执行真实制造研究判断代码；原版研究完成状态用最小桩提供，避免启动 Unity。
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$sourcePath = Join-Path $modRoot 'Source/BDP.Content/Assembly/ChipManufacturing/Resolution/ChipManufacturingResearch.cs'
if (-not (Test-Path -LiteralPath $sourcePath)) { throw '缺少制造研究检查：部分研究完成仍可能制造联动产物。' }
$stubs = @'
using System.Collections.Generic;
namespace Verse { public class ResearchProjectDef { public bool IsFinished; } }
namespace BDP.Content.Assembly.ChipManufacturing.Defs {
 public class ChipManufacturingPresetDef { public bool showInManufacturing = true; public List<Verse.ResearchProjectDef> researchPrerequisites; }
 public class ChipArmamentFormDef { public List<Verse.ResearchProjectDef> researchPrerequisites; }
}
namespace BDP.Content.Assembly.ChipManufacturing.Model {
 public class ChipCombinationResolution {
  public IReadOnlyList<BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef> FunctionDefs;
  public BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef ArmamentForm;
 }
}
public static class ResearchManufacturingFixture {
 public static void Run() {
  var a = new Verse.ResearchProjectDef(); var b = new Verse.ResearchProjectDef();
  var form = new Verse.ResearchProjectDef();
  var function = new BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef {
   researchPrerequisites = new List<Verse.ResearchProjectDef> { a, b }
  };
  var product = new BDP.Content.Assembly.ChipManufacturing.Model.ChipCombinationResolution {
   FunctionDefs = new List<BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef> { function }
  };
  Check(product, false, "未完成任何研究"); a.IsFinished = true;
  Check(product, false, "仅完成A"); a.IsFinished = false; b.IsFinished = true;
  Check(product, false, "仅完成B"); a.IsFinished = true;
  Check(product, true, "A和B均完成");
  product.ArmamentForm = new BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef {
   researchPrerequisites = new List<Verse.ResearchProjectDef> { form }
  };
  Check(product, false, "功能已开放但构型尚未开放"); form.IsFinished = true;
  Check(product, true, "功能和构型都开放");
  b.IsFinished = false; Check(product, false, "重置研究后旧组合重新受限");
  function.researchPrerequisites = null; Check(product, true, "没有研究要求的模板");
  function.showInManufacturing = false;
  Check(product, false, "撤销制造入口后旧组合也不能继续生产");
  function.showInManufacturing = true;
  Check(product, true, "恢复制造入口后已满足条件的组合可以生产");
  function.researchPrerequisites = new List<Verse.ResearchProjectDef> { null };
  Check(product, false, "无效研究引用不得放行");
 }
 private static void Check(BDP.Content.Assembly.ChipManufacturing.Model.ChipCombinationResolution product, bool expected, string message) {
  if (BDP.Content.Assembly.ChipManufacturing.Resolution.ChipManufacturingResearch.IsUnlocked(product) != expected)
   throw new System.Exception(message);
 }
}
'@
$source = [IO.File]::ReadAllText($sourcePath) -replace '(?m)^using [^\r\n]+;\r?\n', ''
Add-Type -TypeDefinition ("using Verse;`nusing BDP.Content.Assembly.ChipManufacturing.Model;`n" + $stubs + $source)
[ResearchManufacturingFixture]::Run()
Write-Host 'PASS：制造联动要求全部研究完成，武装构型参与判断，重置研究或撤销制造入口后旧组合不能绕过。'
