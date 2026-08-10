using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.BounceSlash
{
    /// <summary>
    /// 弹射砍击飞行器 — 继承蚱蜢的直线贴地弹射，附加沿途碰撞伤害。
    ///
    /// 每帧沿飞行线段（Bresenham 栅格化）检测沿途 Pawn，
    /// 本段内每目标最多伤害一次，跨段自动刷新。
    /// 伤害来源名称格式："sourceLabel(施法者名)"。
    /// </summary>
    public class PawnFlyer_BounceSlash : Grasshopper.PawnFlyer_Grasshopper
    {
        /// <summary>本段飞行中已伤害过的 Pawn（每段起跳时清空）。</summary>
        private HashSet<Pawn> hurtThisSegment = new HashSet<Pawn>();

        /// <summary>上一帧所在的格子（用于线段起点）。</summary>
        private IntVec3 lastCell;

        /// <summary>沿途碰撞伤害类型；由能力配置注入，运行期缺失时再回退为 Cut（切割）。</summary>
        public DamageDef damageDef;

        /// <summary>每次命中的伤害值。</summary>
        public int damageAmount = 15;

        /// <summary>护甲穿透。</summary>
        public float armorPenetration = 0f;

        /// <summary>伤害来源名称（不含施法者——实际显示会拼接为"名称(施法者)"）。</summary>
        public string sourceLabel = "弹射砍击";

        /// <summary>关闭蚱蜢专属踏板/气浪特效（后续可替换为砍击专属特效）。</summary>
        protected override bool ShowGrasshopperTrailFx => true;

        /// <summary>
        /// 每帧 Tick：飞行推进 + 沿途碰撞检测。
        /// </summary>
        protected override void Tick()
        {
            IntVec3 curCell = DrawPos.ToIntVec3();

            // 线段碰撞检测（防高速跨格遗漏）
            if (lastCell.IsValid && Map != null)
            {
                List<IntVec3> cells = GenSight.BresenhamCellsBetween(lastCell, curCell);
                for (int ci = 0; ci < cells.Count; ci++)
                {
                    IntVec3 cell = cells[ci];
                    List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
                    for (int ti = 0; ti < things.Count; ti++)
                    {
                        if (things[ti] is Pawn target
                            && target != FlyingPawn
                            && !target.Dead
                            && !hurtThisSegment.Contains(target))
                        {
                            // 快照伤害前的所有 Hediff_Injury 引用（同类伤口 TakeDamage 会
                            // 叠 severity 而非新建 Hediff，故不能用数量计数，只能用引用比对）
                            var injuriesBefore = SnapshotInjuries(target);

                            target.TakeDamage(new DamageInfo(
                                damageDef ?? DamageDefOf.Cut,
                                damageAmount,
                                armorPenetration,
                                instigator: FlyingPawn));

                            hurtThisSegment.Add(target);

                            // 跨引用比对找到受影响（新增或叠层）的伤口，覆写 sourceLabel
                            TrySetInjurySourceAfter(target, injuriesBefore);
                        }
                    }
                }
            }

            lastCell = curCell;
            base.Tick();
        }

        /// <summary>快照目标身上所有 Hediff_Injury 的引用集合。</summary>
        private HashSet<Hediff_Injury> SnapshotInjuries(Pawn target)
        {
            var set = new HashSet<Hediff_Injury>();
            if (target?.health?.hediffSet?.hediffs == null) return set;
            var hediffs = target.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Injury injury) set.Add(injury);
            }
            return set;
        }

        /// <summary>
        /// TakeDamage 后，对比伤害前快照找到受影响（新增或叠层）的伤口，
        /// 设 sourceLabel="能力名(施法者)" + sourceDef（缺 sourceDef 则 LabelInBrackets 不显示来源）。
        /// </summary>
        private void TrySetInjurySourceAfter(Pawn target, HashSet<Hediff_Injury> injuriesBefore)
        {
            if (target?.health?.hediffSet?.hediffs == null) return;
            if (FlyingPawn == null || sourceLabel == null) return;

            string newLabel = $"{sourceLabel}[{FlyingPawn.LabelShort}]";
            var hediffs = target.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Injury injury
                    && (injuriesBefore == null || !injuriesBefore.Contains(injury)))
                {
                    injury.sourceLabel = newLabel;
                    injury.sourceDef = FlyingPawn.def ?? ThingDefOf.Human;
                    injury.sourceToolLabel = null;   // 清空避免走 SourceToolLabel 翻译键模板
                    injury.sourceBodyPartGroup = null;
                }
            }
        }

        /// <summary>
        /// 清空本段已伤害记录。由 Comp 的 OnBeforeSegmentJump 钩子调用。
        /// </summary>
        public void ResetHurtSet()
        {
            hurtThisSegment.Clear();
            lastCell = IntVec3.Invalid;
        }
    }
}
