using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Support.Diagnostics;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>沿原版爆炸创建过程绑定实际对象；独立生成回调不能消费外层请求。</summary>
    [HarmonyPatch(typeof(GenExplosion), nameof(GenExplosion.DoExplosion))]
    public static class Patch_GenExplosion_DoExplosion_BdpCreation
    {
        /// <summary>在原版生成对象之前领取当前调用的请求。</summary>
        [HarmonyPriority(Priority.First)]
        public static void Prefix(Map map, IntVec3 center, out IDisposable __state)
        {
            __state = ExplosionImpactRuntimeScope.EnterCreation(map, center);
        }

        /// <summary>在原版唯一启动调用处取得确切实例，保留原启动参数与流程。</summary>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var start = AccessTools.Method(typeof(Explosion), nameof(Explosion.StartExplosion));
            if (code.Count(item => item.Calls(start)) != 1)
            {
                BdpDiagnostics.Once("damage.boundary.creation_anchor_missing",
                    "BDP_ExplosionCreationPatchUnavailable".Translate());
                return code;
            }
            int index = code.FindIndex(item => item.Calls(start));
            CodeInstruction original = code[index];
            CodeInstruction replacement = new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(Patch_GenExplosion_DoExplosion_BdpCreation), nameof(StartCreatedExplosion)));
            replacement.labels.AddRange(original.labels);
            replacement.blocks.AddRange(original.blocks);
            code[index] = replacement;
            return code;
        }

        /// <summary>配置只绑定到当前调用实际生成的对象，然后继续原版启动。</summary>
        public static void StartCreatedExplosion(Explosion explosion, SoundDef sound, List<Thing> ignoredThings)
        {
            ExplosionImpactRuntimeScope.BindCreatedExplosion(explosion);
            explosion.StartExplosion(sound, ignoredThings);
        }

        /// <summary>正常或异常退出均清理创建边界，保留原异常。</summary>
        public static Exception Finalizer(IDisposable __state, Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }
}
