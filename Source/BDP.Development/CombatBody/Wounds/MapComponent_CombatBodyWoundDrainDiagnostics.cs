using System.Collections;
using System.Collections.Generic;
using System.Text;
using BDP.Core.CombatBody;
using BDP.Core.Trion;
using HarmonyLib;
using Verse;

namespace BDP.Development.CombatBody.Wounds
{
    /// <summary>开发模式下记录独立伤口来源，用于核对实际登记、账本与期限。</summary>
    public sealed class MapComponent_CombatBodyWoundDrainDiagnostics : MapComponent
    {
        /// <summary>上次检查的人物；切换选择后重新输出一次。</summary>
        private Pawn lastPawn;

        /// <summary>上次输出的快照，避免重复记录未变化状态。</summary>
        private string lastSnapshot;

        /// <summary>由原版地图组件构造流程挂接；删除开发程序集即可整体移除。</summary>
        public MapComponent_CombatBodyWoundDrainDiagnostics(Map map) : base(map)
        {
        }

        /// <summary>每秒只检查当前选中人物；诊断读取私有保存记录，不给正式程序集添加调试接口。</summary>
        public override void MapComponentTick()
        {
            if (!Prefs.DevMode || Find.TickManager == null || Find.TickManager.TicksGame % 60 != 0)
            {
                return;
            }

            Pawn pawn = Find.Selector?.SingleSelectedThing as Pawn;
            ICombatBodyReader reader = pawn?.Map == map ? CombatBodySurfaceAccess.ResolveReader(pawn) : null;
            if (reader == null || (reader.Phase != CombatBodyPhase.Active && reader.Phase != CombatBodyPhase.Collapsing))
            {
                lastPawn = null;
                lastSnapshot = null;
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            CompCombatBodyHost host = pawn.GetComp<CompCombatBodyHost>();
            IEnumerable records = Traverse.Create(host).Field("woundRuntime").Field("trionBinding")
                .Field("woundDrains").GetValue<IEnumerable>();
            IReadOnlyDictionary<TrionDrainKey, float> ledger = TrionSurfaceAccess.ResolveReader(pawn)?.GetDrainSnapshot();
            StringBuilder details = new StringBuilder();
            int count = 0;
            if (records != null)
            {
                foreach (object record in records)
                {
                    Traverse saved = Traverse.Create(record);
                    Hediff wound = saved.Field("wound").GetValue<Hediff>();
                    BodyPartRecord part = wound?.Part;
                    if (part == null)
                    {
                        continue;
                    }

                    float registeredRate = saved.Field("drainPerSecond").GetValue<float>();
                    int expiryTick = saved.Field("expiryTick").GetValue<int>();
                    float publishedRate = 0f;
                    if (ledger != null)
                    {
                        foreach (KeyValuePair<TrionDrainKey, float> entry in ledger)
                        {
                            if (entry.Key.Domain == "CombatBody" && entry.Key.Channel == "Wound" && entry.Key.Index == wound.loadID)
                            {
                                publishedRate += entry.Value;
                            }
                        }
                    }

                    details.AppendLine("BDP_Development_WoundDrain_Wound".Translate(
                        wound.loadID, part.Label, part.def.GetMaxHealth(pawn).ToString("0.##"),
                        pawn.health.hediffSet.GetPartHealth(part).ToString("0.##"), registeredRate.ToString("0.##"),
                        publishedRate.ToString("0.##"), expiryTick, System.Math.Max(0, expiryTick - currentTick)));
                    count++;
                }
            }

            string snapshot = count + "\n" + details;
            if (lastPawn == pawn && lastSnapshot == snapshot)
            {
                return;
            }

            lastPawn = pawn;
            lastSnapshot = snapshot;
            Log.Message("BDP_Development_WoundDrain_Snapshot".Translate(pawn.LabelShort, currentTick, count, details.ToString()));
        }
    }
}
