using RimWorld;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// Ability 来源级冷却服务。
    /// 它把 Def 配置、全局账本和原版 Ability 对象之间的同步集中在一个中性入口。
    /// </summary>
    internal static class AbilityReleaseCooldownService
    {
        /// <summary>
        /// 判断指定 Ability 当前是否仍处于来源级冷却。
        /// </summary>
        internal static bool IsOnCooldown(Pawn pawn, AbilityDef abilityDef, out int remaining)
        {
            remaining = 0;
            string cooldownKey = ResolveCooldownKey(abilityDef);
            if (pawn == null || string.IsNullOrWhiteSpace(cooldownKey))
            {
                return false;
            }

            GameComponent_AbilityReleaseCooldownLedger ledger = ResolveLedger();
            if (ledger == null)
            {
                return false;
            }

            remaining = ledger.GetRemaining(pawn.ThingID, cooldownKey);
            return remaining > 0;
        }

        /// <summary>
        /// 记录 Ability 已经由原版启动的实际冷却时长。
        /// </summary>
        internal static void RecordReleaseCooldown(Ability ability, string cooldownKey)
        {
            if (ability?.pawn == null || string.IsNullOrWhiteSpace(cooldownKey))
            {
                return;
            }

            int cooldownTicks = ability.CooldownTicksRemaining;
            if (cooldownTicks <= 0)
            {
                return;
            }

            int cooldownDurationTicks = ability.CooldownTicksTotal;
            if (cooldownDurationTicks <= 0)
            {
                return;
            }

            ResolveLedger()?.StartCooldown(
                ability.pawn.ThingID,
                cooldownKey,
                cooldownTicks,
                cooldownDurationTicks);
        }

        /// <summary>
        /// Ability 宿主重新挂载时恢复来源级冷却的剩余时间。
        /// </summary>
        internal static void RestoreAbilityCooldown(Pawn pawn, Ability ability, AbilityDef abilityDef)
        {
            int remaining;
            if (ability == null
                || !IsOnCooldown(pawn, abilityDef, out remaining)
                || remaining <= ability.CooldownTicksRemaining)
            {
                return;
            }

            ability.StartCooldown(remaining);
        }

        /// <summary>
        /// 读取来源级冷却在释放时保存的总时长，供原版进度条表面使用。
        /// </summary>
        internal static bool TryGetCooldownDuration(Ability ability, out int durationTicks)
        {
            durationTicks = 0;
            if (ability?.pawn == null)
            {
                return false;
            }

            string cooldownKey = ResolveCooldownKey(ability.def);
            if (string.IsNullOrWhiteSpace(cooldownKey))
            {
                return false;
            }

            int remaining;
            return ResolveLedger()?.TryGetCooldown(
                ability.pawn.ThingID,
                cooldownKey,
                out remaining,
                out durationTicks) == true;
        }

        /// <summary>
        /// 从 AbilityDef 组件中读取通用冷却键。
        /// </summary>
        internal static string ResolveCooldownKey(AbilityDef abilityDef)
        {
            if (abilityDef?.comps == null)
            {
                return null;
            }

            for (int index = 0; index < abilityDef.comps.Count; index++)
            {
                CompProperties_AbilityReleaseCooldown properties =
                    abilityDef.comps[index] as CompProperties_AbilityReleaseCooldown;
                if (properties != null && !string.IsNullOrWhiteSpace(properties.cooldownKey))
                {
                    return properties.cooldownKey;
                }
            }

            return null;
        }

        /// <summary>
        /// 解析当前游戏中的通用来源级冷却账本。
        /// </summary>
        private static GameComponent_AbilityReleaseCooldownLedger ResolveLedger()
        {
            return Current.Game != null
                ? Current.Game.GetComponent<GameComponent_AbilityReleaseCooldownLedger>()
                : null;
        }
    }
}
