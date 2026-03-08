using HarmonyLib;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Postfix patch on Pawn.TryGetAttackVerb（v14.0自动攻击支持）。
    ///
    /// 用途：使引擎自动攻击系统能找到芯片远程Verb。
    ///
    /// 原问题：
    /// - 触发体装备后，小人不会自动使用芯片远程攻击，只会等到贴身用"柄"Tool近战
    /// - 原因：芯片Verb脱离VerbTracker（v5.1设计），Pawn.TryGetAttackVerb()只找到"柄"近战Verb
    ///
    /// 解决方案：
    /// - 原版已返回有效远程Verb → 不干预
    /// - 检查触发体CompTriggerBody.ProxyVerb
    /// - ProxyVerb.Available()==true → 替换返回值
    ///
    /// 委托流程：
    /// 1. 引擎调用Pawn.TryGetAttackVerb() → 获得ProxyVerb
    /// 2. 引擎调用ProxyVerb.TryStartCastOn() → 委托给芯片Verb.TryStartCastOn()
    /// 3. 芯片Verb进入Stance_Warmup → warmup结束后触发TryCastShot射击
    /// 4. TryCastShot中检查Trion、FireMode、引导弹等逻辑，发射弹道
    ///
    /// 不受影响的现有功能：
    /// - 芯片Gizmo（Command_BDPChipAttack）：ProxyVerb的hasStandardCommand=false，不生成Gizmo
    /// - "柄"Y按钮：ProxyVerb不在AllVerbs中，不影响GetVerbsCommands
    /// - BDP专用JobDriver：手动Gizmo攻击仍走OrderForceTarget→BDP Job
    /// - 近战选择池：ProxyVerb继承Verb_Shoot，IsMeleeAttack=false
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
    public static class Patch_Pawn_TryGetAttackVerb
    {
        public static void Postfix(ref Verb __result, Pawn __instance)
        {
            // 原版已返回有效远程Verb → 不干预
            if (__result != null && !__result.IsMeleeAttack) return;

            // 检查触发体
            var triggerComp = __instance.equipment?.Primary?.GetComp<CompTriggerBody>();
            if (triggerComp == null) return;

            var proxy = triggerComp.ProxyVerb;
            if (proxy != null && proxy.Available())
                __result = proxy;
        }
    }
}
