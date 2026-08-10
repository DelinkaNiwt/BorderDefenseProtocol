using BDP.Core.Semantics;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 原版最终伤口来源名写入点的最小接线。
    /// 只在当前伤害作用域里存在 BDP 攻击语义时覆盖来源名，否则完全回退原版。
    /// </summary>
    [HarmonyPatch(typeof(DamageWorker_AddInjury), "FinalizeAndAddInjury", new[] { typeof(Pawn), typeof(Hediff_Injury), typeof(DamageInfo), typeof(DamageWorker.DamageResult) })]
    /// <summary>
    /// 伤口来源名回写补丁。
    /// </summary>
    public static class Patch_DamageWorker_AddInjury_SourceLabel
    {
        /// <summary>
        /// 在原版真正写入伤口来源名之前，检查当前伤害链上是否存在 BDP 攻击语义。
        /// 有就覆盖括号里的来源名；没有就完全不碰，让原版照常处理。
        /// </summary>
        public static void Prefix(ref Hediff_Injury injury, DamageInfo dinfo)
        {
            ISemanticContext semanticContext = SemanticRuntimeScope.Current;
            if (!BdpDamageSemanticBridge.TryApplyInjurySource(
                injury,
                semanticContext,
                dinfo.Weapon,
                dinfo.Tool?.label,
                dinfo.WeaponBodyPartGroup))
            {
                return;
            }
        }
    }
}
