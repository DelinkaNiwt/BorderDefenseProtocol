using System;
using System.Globalization;
using System.Text;
using BDP.Content.Shield;
using BDP.Core.Trion;
using LudeonTK;
using RimWorld;
using Verse;

namespace BDP.Development.Shield
{
    /// <summary>护盾实测菜单；整体删除开发程序集即可移除所有临时入口。</summary>
    public static class EnergyShieldDebugActions
    {
        /// <summary>原版开发菜单节点，所有玩家可见文案来自语言包。</summary>
        [DebugAction("BDP", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static DebugActionNode ShieldTestMenu()
        {
            var root = new DebugActionNode("BDP_Dev_Shield_Menu".Translate());
            root.AddChild(new DebugActionNode("BDP_Dev_Shield_Output".Translate(), action: () => Selected(Output)));
            root.AddChild(new DebugActionNode("BDP_Dev_Shield_Hit5".Translate(), action: () => Selected(p => Hit(p, 5f, false))));
            root.AddChild(new DebugActionNode("BDP_Dev_Shield_Hit25".Translate(), action: () => Selected(p => Hit(p, 25f, false))));
            root.AddChild(new DebugActionNode("BDP_Dev_Shield_Bypass5".Translate(), action: () => Selected(p => Hit(p, 5f, true))));
            root.AddChild(new DebugActionNode("BDP_Dev_Shield_Break".Translate(), action: () => Selected(p =>
            {
                int count = EnergyShieldApi.BreakActiveShields(p);
                Log.Message("BDP_Dev_Shield_BreakResult".Translate(count));
                Output(p);
            })));
            root.AddChild(new DebugActionNode("BDP_Dev_Shield_Observe".Translate(), action: () =>
            {
                var observer = Find.CurrentMap.GetComponent<MapComponent_EnergyShieldDiagnostics>();
                observer.Enabled = !observer.Enabled;
                Log.Message((observer.Enabled ? "BDP_Dev_Shield_ObserveOn" : "BDP_Dev_Shield_ObserveOff").Translate());
            }));
            return root;
        }

        /// <summary>菜单动作只对明确选中的人物执行。</summary>
        private static void Selected(Action<Pawn> action)
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null) { Messages.Message("BDP_Dev_Shield_SelectPawn".Translate(), MessageTypeDefOf.RejectInput, false); return; }
            action(pawn);
        }

        /// <summary>从正面施加真实原版弹伤；不改写格挡概率，未格挡会真实受伤。</summary>
        private static void Hit(Pawn pawn, float amount, bool bypass)
        {
            Output(pawn);
            var damage = new DamageInfo(DamageDefOf.Bullet, amount, 0f, pawn.Rotation.AsAngle + 180f);
            if (bypass) { using (EnergyShieldApi.BeginBypass(pawn)) pawn.TakeDamage(damage); }
            else pawn.TakeDamage(damage);
            Output(pawn);
        }

        /// <summary>输出正式状态，不自行生成、修改或结算耐久。</summary>
        public static void Output(Pawn pawn) { Log.Message(BuildSnapshot(pawn)); }

        /// <summary>统一快照展示资源、比例、恢复时点及防护宿主存在性。</summary>
        internal static string BuildSnapshot(Pawn pawn)
        {
            var text = new StringBuilder();
            var resource = TrionSurfaceAccess.ResolveReader(pawn);
            text.AppendLine("BDP_Dev_Shield_Header".Translate(pawn.LabelShortCap, pawn.ThingID,
                Number(resource?.Cur ?? 0f), Find.TickManager.TicksGame));
            var snapshots = EnergyShieldApi.GetSnapshots(pawn);
            if (snapshots.Count == 0) text.AppendLine("BDP_Dev_Shield_NoState".Translate());
            foreach (var state in snapshots)
                text.AppendLine("BDP_Dev_Shield_Row".Translate(state.GroupKey,
                    Number(state.CurrentDurability), Number(state.MaximumDurability),
                    Number(state.DurabilityFraction * 100f), state.RecoveryUntilTick,
                    (state.IsBroken ? "BDP_Shield_Broken" : "BDP_Shield_Intact").Translate(),
                    (state.HasDefenseHost ? "BDP_Dev_Shield_HostOn" : "BDP_Dev_Shield_HostOff").Translate()));
            return text.ToString();
        }

        /// <summary>稳定小数格式便于比较前后变化。</summary>
        private static string Number(float number) { return number.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    /// <summary>短时观测选中人物，开关关闭时不参与任何护盾读取。</summary>
    public sealed class MapComponent_EnergyShieldDiagnostics : MapComponent
    {
        /// <summary>会话内观测开关，不写入存档。</summary>
        public bool Enabled;
        /// <summary>最近观测人物。</summary>
        private Pawn lastPawn;
        /// <summary>最近状态内容；去除时间变化，避免每秒重复日志。</summary>
        private string lastState;
        /// <summary>由原版地图自动建立的可插拔组件。</summary>
        public MapComponent_EnergyShieldDiagnostics(Map map) : base(map) { }
        /// <summary>开发模式每秒检查一次，仅状态变化才输出完整诊断。</summary>
        public override void MapComponentTick()
        {
            if (!Prefs.DevMode || !Enabled || Find.TickManager.TicksGame % 60 != 0) return;
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null || pawn.Map != map) { lastPawn = null; lastState = null; return; }
            string snapshot = EnergyShieldDebugActions.BuildSnapshot(pawn);
            int headerEnd = snapshot.IndexOf('\n');
            string state = headerEnd >= 0 ? snapshot.Substring(headerEnd + 1) : snapshot;
            if (pawn == lastPawn && state == lastState) return;
            lastPawn = pawn;
            lastState = state;
            Log.Message(snapshot);
        }
    }
}
