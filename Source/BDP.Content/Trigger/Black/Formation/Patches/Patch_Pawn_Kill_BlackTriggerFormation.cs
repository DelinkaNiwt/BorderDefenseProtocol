using HarmonyLib;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>在 Pawn.Kill 前准备完整候选，在尸体生成后建立唯一待决事件。</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_BlackTriggerFormation
    {
        /// <summary>一次 Pawn.Kill 调用内部传递的冻结准备结果。</summary>
        public sealed class DeathContext
        {
            /// <summary>死亡前已经完成匹配的准备结果。</summary>
            public BlackTriggerPreparedAttempt PreparedAttempt;
        }

        /// <summary>死亡发生前冻结资料；主动燃尽使用预先验证的同一份结果。</summary>
        public static void Prefix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit, ref DeathContext __state)
        {
            BlackTriggerInheritedTraitService.RemoveAllTemporary(__instance);
            BlackTriggerBindingService.NotifyBinderDeath(__instance);
            if (!CanCapture(__instance)) return;
            BlackTriggerPreparedAttempt prepared = BlackTriggerSacrificeService.GetPrepared(__instance);
            if (prepared != null)
            {
                __state = new DeathContext { PreparedAttempt = prepared };
                return;
            }
            if (BlackTriggerDeathFilter.IsIntentionalDeath(dinfo, exactCulprit)) return;
            string failureReasonKey;
            if (BlackTriggerFormationWorldService.TryPrepare(__instance, BlackTriggerFormationSource.NaturalDeath, dinfo, exactCulprit, false, true, out prepared, out failureReasonKey))
            {
                __state = new DeathContext { PreparedAttempt = prepared };
            }
        }

        /// <summary>尸体已经由原版生成后，提交唯一待决事件。</summary>
        public static void Postfix(Pawn __instance, DeathContext __state)
        {
            if (__state?.PreparedAttempt == null || __instance?.Corpse == null || __instance.Corpse.Destroyed) return;
            BlackTriggerFormationWorldService.TryCreatePending(__state.PreparedAttempt, __instance.Corpse);
        }

        /// <summary>来源只允许尚未死亡且生命印记未耗尽的玩家殖民者。</summary>
        private static bool CanCapture(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && pawn.IsColonistPlayerControlled && !BlackTriggerExhaustionUtility.IsExhausted(pawn);
        }
    }

    /// <summary>排除处决和外科致死等不属于自然死亡的来源。</summary>
    public static class BlackTriggerDeathFilter
    {
        /// <summary>判断是否属于明确主动致死。</summary>
        public static bool IsIntentionalDeath(DamageInfo? dinfo, Hediff exactCulprit)
        {
            string damageName = dinfo.HasValue ? dinfo.Value.Def?.defName ?? string.Empty : string.Empty;
            return damageName.IndexOf("Execution", System.StringComparison.OrdinalIgnoreCase) >= 0
                || damageName.IndexOf("Surgical", System.StringComparison.OrdinalIgnoreCase) >= 0
                || exactCulprit?.def?.defName?.IndexOf("Surgical", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
