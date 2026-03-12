using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Prefix patch on Pawn_MeleeVerbs.TryMeleeAttack（近战芯片自动攻击支持）。
    ///
    /// 原问题：
    ///   芯片近战Verb不在VerbTracker.AllVerbs中（v5.1设计），
    ///   GetUpdatedAvailableVerbsList只从AllVerbs+身体部位构建近战池，
    ///   导致贴身自动攻击只用拳头/触发体柄，芯片近战Verb从不被选中。
    ///
    /// 解决方案：
    ///   TryMeleeAttack有verbToUse参数——非null时直接使用，跳过池选择。
    ///   在verbToUse==null（引擎自动选择）时注入芯片近战Verb即可。
    ///
    /// 覆盖情况：
    ///   0芯片         → null → 不干预 → 拳头/柄（默认行为）
    ///   1近战芯片      → 注入近战芯片Verb
    ///   1远程芯片      → null（IsMeleeAttack=false）→ 不干预 → 拳头/柄
    ///   2近战芯片      → 注入双重近战Verb
    ///   2远程芯片      → null → 不干预 → 拳头/柄
    ///   1近战+1远程    → 注入近战侧Verb
    /// </summary>
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
    public static class Patch_Pawn_MeleeVerbs_TryMeleeAttack
    {
        public static void Prefix(Pawn_MeleeVerbs __instance, ref Verb verbToUse)
        {
            // 已指定verb（玩家手动命令或其他系统），不干预
            if (verbToUse != null) return;

            var triggerComp = __instance.Pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return;

            // 战斗体未激活 → 不干预（芯片未就绪，Verb缓存可能是旧数据）
            if (!triggerComp.IsCombatBodyActive) return;

            var meleeVerb = triggerComp.GetPrimaryMeleeChipVerb();
            if (meleeVerb == null) return;

            verbToUse = meleeVerb;
        }
    }
}
