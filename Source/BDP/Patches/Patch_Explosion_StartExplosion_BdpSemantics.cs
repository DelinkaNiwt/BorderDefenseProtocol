using BDP.Core.Semantics;
using HarmonyLib;
using Verse;
using Verse.Sound;

namespace BDP.Patches
{
    /// <summary>
    /// 爆炸链开始时的最小接线。
    /// 这里只把当前调用边界上的攻击语义挂到 Explosion 实例，不写任何业务规则。
    /// </summary>
    [HarmonyPatch(typeof(Explosion), nameof(Explosion.StartExplosion))]
    /// <summary>
    /// 爆炸启动时的语义挂接补丁。
    /// </summary>
    public static class Patch_Explosion_StartExplosion_BdpSemantics
    {
        /// <summary>
        /// 在爆炸真正开始前，把当前调用边界上的攻击语义挂到新生成的 Explosion 实例上。
        /// 这样后续即使跨 Tick 逐格结算，也还能知道“这次爆炸原本是哪一个攻击行为触发的”。
        /// </summary>
        public static void Prefix(Explosion __instance, SoundDef explosionSound)
        {
            ISemanticContext semanticContext = SemanticRuntimeScope.Current;
            if (semanticContext == null)
            {
                return;
            }

            // 这里只做数据挂接，不做任何爆炸业务判断。
            BdpDamageSemanticBridge.AssignExplosionContext(__instance, semanticContext);
        }
    }
}
