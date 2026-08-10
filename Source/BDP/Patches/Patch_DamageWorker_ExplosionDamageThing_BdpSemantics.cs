using System;
using BDP.Core.Semantics;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 爆炸对具体目标造成伤害前的最小接线。
    /// 它只负责把挂在 Explosion 上的语义重新压回当前伤害作用域。
    /// </summary>
    [HarmonyPatch(typeof(DamageWorker), "ExplosionDamageThing")]
    /// <summary>
    /// 爆炸逐目标伤害前的语义回灌补丁。
    /// </summary>
    public static class Patch_DamageWorker_ExplosionDamageThing_BdpSemantics
    {
        /// <summary>
        /// 爆炸真正准备对某个目标调用 `TakeDamage` 前，把挂在 Explosion 上的语义压回当前线程作用域。
        /// </summary>
        public static void Prefix(Explosion explosion, ref IDisposable __state)
        {
            __state = SemanticRuntimeScope.Push(BdpDamageSemanticBridge.GetExplosionContext(explosion));
        }

        /// <summary>
        /// 无论这次爆炸伤害是正常结束还是异常退出，都把这一小段临时语义作用域弹掉。
        /// </summary>
        [HarmonyFinalizer]
        public static Exception Finalizer(IDisposable __state, Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }
}
