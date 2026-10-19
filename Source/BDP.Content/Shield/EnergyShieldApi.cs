using System;
using System.Collections.Generic;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>本类 BDP 护盾统一调用入口；穿盾只作用于本次同步伤害调用，不影响原版盾。</summary>
    public static class EnergyShieldApi
    {
        /// <summary>光魂防护、冲拳与推进共同引用的持久盾组键。</summary>
        public const string LightSoulGroupKey = "light_soul";

        /// <summary>当前线程嵌套伤害的穿盾范围。</summary>
        [ThreadStatic] private static BypassScope currentBypass;
        /// <summary>当前游戏刻。</summary>
        internal static int CurrentTick { get { return Find.TickManager != null ? Find.TickManager.TicksGame : 0; } }

        /// <summary>读取持久状态宿主；不触发表达重建。</summary>
        internal static Hediff_EnergyShieldState ResolveState(Pawn pawn, bool create = false)
        {
            if (pawn?.health?.hediffSet == null) return null;
            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
                if (pawn.health.hediffSet.hediffs[i] is Hediff_EnergyShieldState state) return state;
            if (!create) return null;
            var definition = DefDatabase<HediffDef>.GetNamed("BDP_Hediff_EnergyShieldState");
            var added = (Hediff_EnergyShieldState)HediffMaker.MakeHediff(definition, pawn);
            pawn.health.AddHediff(added);
            return added;
        }

        /// <summary>读取各组不可写快照；有效宿主的上限优先于最近配置参考值。</summary>
        public static IReadOnlyList<EnergyShieldSnapshot> GetSnapshots(Pawn pawn)
        {
            var snapshots = new List<EnergyShieldSnapshot>();
            var owner = ResolveState(pawn);
            if (owner == null) return snapshots.AsReadOnly();
            foreach (var state in owner.Groups)
            {
                float maximum = state.LastMaximumDurability;
                var shield = FindActiveShield(pawn, state.GroupKey);
                bool hasHost = shield != null;
                if (hasHost) maximum = shield.MaximumDurability;
                snapshots.Add(new EnergyShieldSnapshot(state, maximum, CurrentTick, hasHost));
            }
            return snapshots.AsReadOnly();
        }

        /// <summary>定位盾组当前防护宿主，供恢复与快照共用；不创建记录或推进恢复。</summary>
        internal static HediffComp_EnergyShield FindActiveShield(Pawn pawn, string groupKey)
        {
            if (pawn?.health?.hediffSet == null) return null;
            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                var shield = pawn.health.hediffSet.hediffs[i].TryGetComp<HediffComp_EnergyShield>();
                if (shield != null && shield.Props.durabilityGroupKey == groupKey) return shield;
            }
            return null;
        }

        /// <summary>读取组是否完好；首次启用尚无记录时默认满耐久。</summary>
        public static bool IsIntact(Pawn pawn, string groupKey)
        {
            EnergyShieldDurabilityState state = ResolveState(pawn)?.FindGroup(groupKey);
            return pawn != null && (state == null || !state.IsBrokenAt(CurrentTick));
        }

        /// <summary>先按旧配置结算，再登记有效来源；启停、上限和姿态变化不重置比例与计时。</summary>
        internal static EnergyShieldDurabilityState Bind(HediffComp_EnergyShield shield)
        {
            var state = ResolveState(shield.Pawn, true).FindGroup(shield.Props.durabilityGroupKey, true);
            state.RecoverIfDue(CurrentTick);
            UpdateSourceConfiguration(state, shield);
            return state;
        }

        /// <summary>更新当前来源配置与有效上限；只更新参数，不改变剩余比例或计时。</summary>
        internal static void UpdateSourceConfiguration(EnergyShieldDurabilityState state, HediffComp_EnergyShield shield)
        {
            state.RecoveryTicks = shield.Props.recoveryTicks;
            state.NaturalRecoveryUsePercentage = shield.Props.naturalRecoveryUsePercentage;
            state.NaturalRecoveryPerSecond = shield.Props.naturalRecoveryPerSecond;
            state.NaturalRecoveryDelayTicks = shield.Props.naturalRecoveryDelayTicks;
            state.LastMaximumDurability = shield.MaximumDurability;
        }

        /// <summary>强制破坏指定已登记盾组；关闭后仍沿用该组配置，重复破盾不延时。</summary>
        public static bool TryBreak(Pawn pawn, string groupKey)
        {
            var state = ResolveState(pawn)?.FindGroup(groupKey);
            if (state == null || !state.TryBreak(CurrentTick, state.RecoveryTicks)) return false;
            NotifyBroken(pawn);
            return true;
        }

        /// <summary>盾组从完好转为破盾后统一播放反馈并刷新表达；只在状态转换成功时调用。</summary>
        internal static void NotifyBroken(Pawn pawn)
        {
            // 受击路径已完整吸收本次伤害；表现与宿主移除不得提前到伤害结算之前。
            EnergyShieldEffectPlayer.PlayBreakSound(pawn);
            ExpressionSurfaceAccess.NotifySourceConditionsChanged(pawn);
        }

        /// <summary>强制破坏当前存在的全部本类盾；先收集组键，避免发布过程中修改遍历集合。</summary>
        public static int BreakActiveShields(Pawn pawn)
        {
            var keys = new HashSet<string>();
            if (pawn?.health?.hediffSet == null) return 0;
            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                var shield = pawn.health.hediffSet.hediffs[i].TryGetComp<HediffComp_EnergyShield>();
                if (shield != null) { Bind(shield); keys.Add(shield.Props.durabilityGroupKey); }
            }
            int broken = 0;
            foreach (string key in keys) if (TryBreak(pawn, key)) broken++;
            return broken;
        }

        /// <summary>为本次同步 TakeDamage（受伤）调用开启穿盾范围；可选限定受击人物与盾组。</summary>
        public static IDisposable BeginBypass(Pawn target = null, string groupKey = null)
        {
            var scope = new BypassScope(currentBypass, target, groupKey);
            currentBypass = scope;
            return scope;
        }

        /// <summary>供盾组件读取本次伤害是否穿过自己，正常格挡路径无范围时直接返回。</summary>
        public static bool ShouldBypass(Pawn target, string groupKey)
        {
            for (BypassScope scope = currentBypass; scope != null; scope = scope.Previous)
                if ((scope.Target == null || scope.Target == target)
                    && (scope.GroupKey == null || scope.GroupKey == groupKey)) return true;
            return false;
        }

        /// <summary>成对释放的线程局部范围；异常退出也恢复外层设置。</summary>
        private sealed class BypassScope : IDisposable
        {
            /// <summary>外层范围。</summary>
            internal readonly BypassScope Previous;
            /// <summary>可选受击人物限制。</summary>
            internal readonly Pawn Target;
            /// <summary>可选盾组限制。</summary>
            internal readonly string GroupKey;
            /// <summary>避免重复释放覆盖后续范围。</summary>
            private bool disposed;
            /// <summary>记录外层与本层限制。</summary>
            internal BypassScope(BypassScope previous, Pawn target, string groupKey)
            { Previous = previous; Target = target; GroupKey = groupKey; }
            /// <summary>恢复进入前的穿盾范围，调用方须按嵌套顺序释放。</summary>
            public void Dispose() { if (!disposed) { currentBypass = Previous; disposed = true; } }
        }
    }
}
