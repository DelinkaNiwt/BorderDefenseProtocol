using BDP.Core.Trigger;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 多选攻击 Gizmo 显隐补丁。
    /// 原版 AtLeastOneSelectedPlayerPawnHasRangedWeapon 只检查 def.IsRangedWeapon，
    /// 触发体 def 无 &lt;verbs&gt; 不满足该条件。此处补充 BDP 触发体 + 活跃远程芯片的判断。
    /// </summary>
    [HarmonyPatch(typeof(PawnAttackGizmoUtility), "AtLeastOneSelectedPlayerPawnHasRangedWeapon")]
    public static class Patch_PawnAttackGizmoUtility_RangedWeapon
    {
        /// <summary>
        /// 原版返回 false 时，额外检查选中 pawn 是否有 BDP 触发体且激活了远程芯片。
        /// </summary>
        static void Postfix(ref bool __result)
        {
            if (__result)
                return; // 原版已有远程武器，不必再查

            foreach (object obj in Find.Selector.SelectedObjectsListForReading)
            {
                if (obj is Pawn pawn
                    && pawn.IsColonistPlayerControlled
                    && pawn.equipment?.Primary != null)
                {
                    CompTriggerBody triggerBody = pawn.equipment.Primary.TryGetComp<CompTriggerBody>();
                    if (triggerBody != null && triggerBody.HasActiveRangedChip())
                    {
                        __result = true;
                        return;
                    }
                }
            }
        }
    }
}
