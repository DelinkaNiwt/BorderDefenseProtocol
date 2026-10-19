using System.Globalization;
using System.Text;
using BDP.Core.CombatBody;
using BDP.Core.Trigger;
using BDP.Core.Trion;
using Verse;

namespace BDP.Development.CombatBody.HostileResource
{
    /// <summary>只读选中人物的隐藏资源真值，不参与正式结算和界面显示。</summary>
    public sealed class MapComponent_HostileTrionDiagnostics : MapComponent
    {
        /// <summary>观测只在本次游戏运行期间开启，不写入存档。</summary>
        public bool Enabled { get; set; }

        /// <summary>最近检查的人物；切换选中对象后重新输出。</summary>
        private Pawn lastPawn;

        /// <summary>最近快照，避免重复输出无变化数据。</summary>
        private string lastSnapshot;

        /// <summary>由原版地图组件自动创建，移除开发程序集即可移除观测。</summary>
        public MapComponent_HostileTrionDiagnostics(Map map) : base(map) { }

        /// <summary>开发模式中每秒采样选中人物，关掉开关即不读取任何资源。</summary>
        public override void MapComponentTick()
        {
            if (!Prefs.DevMode || !Enabled || Find.TickManager.TicksGame % 60 != 0) return;
            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null || pawn.Map != map)
            {
                lastPawn = null;
                lastSnapshot = null;
                return;
            }
            string snapshot = BuildSnapshot(pawn);
            if (snapshot == null || (pawn == lastPawn && snapshot == lastSnapshot)) return;
            lastPawn = pawn;
            lastSnapshot = snapshot;
            Write(pawn, snapshot);
        }

        /// <summary>暂停游戏时也可手动输出当前真实状态。</summary>
        public static void Output(Pawn pawn)
        {
            string snapshot = BuildSnapshot(pawn);
            if (snapshot != null) Write(pawn, snapshot);
        }

        /// <summary>快照保留实际资源及声明账本，两者不能混为实际持续扣减。</summary>
        private static string BuildSnapshot(Pawn pawn)
        {
            ITrionReader resource = TrionSurfaceAccess.ResolveReader(pawn);
            if (resource == null) return null;
            ICombatBodyReader body = CombatBodySurfaceAccess.ResolveReader(pawn);
            TrionResourceRules rules = TrionResourceRuleRegistry.GetRules(pawn);
            var details = new StringBuilder();
            details.AppendLine("BDP_Dev_HostileTrion_Values".Translate(
                Number(resource.Cur), Number(resource.Max), Number(resource.Reserved),
                Number(resource.Allocated), Number(body?.AllocatedTrion ?? 0f),
                Number(resource.TotalDrainPerSecond), Number(resource.EffectiveRecoveryPerDay)));
            details.AppendLine("BDP_Dev_HostileTrion_Rules".Translate(
                ("BDP_Dev_HostileTrion_Phase_" + (body?.Phase ?? CombatBodyPhase.Inactive)).Translate(),
                Flag(rules.FunctionCostsEnabled), Flag(rules.CapacityCostsEnabled),
                Flag(rules.CapacityInfoVisible), Flag(resource.Frozen)));
            foreach (var entry in resource.GetDrainSnapshot())
                details.AppendLine("BDP_Dev_HostileTrion_Drain".Translate(entry.Key.ToString(), Number(entry.Value)));
            ITriggerLoadoutReader loadout = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
            if (loadout != null)
                foreach (ITriggerSlotState slot in loadout.GetAllSlots())
                    if (slot.LoadedChip != null)
                        details.AppendLine("BDP_Dev_HostileTrion_Slot".Translate(
                            slot.Side.ToString(), slot.Index + 1, slot.LoadedChip.LabelCap, Flag(slot.IsActive)));
            return details.ToString();
        }

        /// <summary>日志与普通玩家面板分离，只有开发工具主动调用时输出。</summary>
        private static void Write(Pawn pawn, string snapshot)
        {
            Log.Message("BDP_Dev_HostileTrion_Snapshot".Translate(
                pawn.LabelShortCap, pawn.ThingID, Find.TickManager.TicksGame, snapshot));
        }

        /// <summary>固定数字格式，便于比较前后样本。</summary>
        private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>布尔状态也从语言包读取。</summary>
        private static string Flag(bool value) =>
            (value ? "BDP_Dev_HostileTrion_Enabled" : "BDP_Dev_HostileTrion_Disabled").Translate();
    }
}
