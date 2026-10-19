using System.Collections.Generic;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 按投射物最终进攻段解析出的原版掩体快照。
    /// 概率和随机拦截对象必须来自同一组 CoverInfo（掩体信息）。
    /// </summary>
    public sealed class TrajectoryCoverSnapshot : IExposable
    {
        /// <summary>当前是否拥有有效的掩体几何原点。</summary>
        public bool HasCoverOriginCell { get; set; }

        /// <summary>当前掩体解析使用的最终飞行段起点格。</summary>
        public IntVec3 CoverOriginCell { get; set; } = IntVec3.Invalid;

        /// <summary>原版 CoverUtility（掩体工具）计算出的总体阻挡概率。</summary>
        public float OverallBlockChance { get; set; }

        /// <summary>原版掩体计算产生的候选掩体集合，仅在发射解析期间使用。</summary>
        public List<CoverInfo> Covers { get; set; } = new List<CoverInfo>();

        /// <summary>候选掩体实体引用，用于投射物存档恢复后的终端阻挡识别。</summary>
        public List<Thing> CoverThings { get; set; } = new List<Thing>();

        /// <summary>同一组候选掩体中按原版权重选出的实际拦截物。</summary>
        public Thing RandomCover { get; set; }

        /// <summary>
        /// 从指定的最终进攻段起点调用原版掩体算法。
        /// 该方法不复制原版角度、距离和组合概率公式。
        /// </summary>
        public static TrajectoryCoverSnapshot Create(LocalTargetInfo target, IntVec3 coverOriginCell, Map map)
        {
            TrajectoryCoverSnapshot snapshot = new TrajectoryCoverSnapshot
            {
                HasCoverOriginCell = coverOriginCell.IsValid && map != null && coverOriginCell.InBounds(map),
                CoverOriginCell = coverOriginCell
            };

            if (!snapshot.HasCoverOriginCell || !target.IsValid || map == null)
            {
                return snapshot;
            }

            snapshot.Covers = CoverUtility.CalculateCoverGiverSet(target, coverOriginCell, map)
                ?? new List<CoverInfo>();
            for (int i = 0; i < snapshot.Covers.Count; i++)
            {
                if (snapshot.Covers[i].Thing != null)
                {
                    snapshot.CoverThings.Add(snapshot.Covers[i].Thing);
                }
            }
            snapshot.OverallBlockChance = CoverUtility.CalculateOverallBlockChance(target, coverOriginCell, map);
            if (snapshot.Covers.TryRandomElementByWeight(
                    cover => cover.BlockChance,
                    out CoverInfo selectedCover))
            {
                snapshot.RandomCover = selectedCover.Thing;
            }

            return snapshot;
        }

        /// <summary>
        /// 保存发射后仍可能需要的几何和概率事实。
        /// CoverInfo 不作为跨存档运行时依赖；只保留候选实体引用，供已发射投射物恢复终端阻挡语义。
        /// </summary>
        public void ExposeData()
        {
            bool hasCoverOriginCell = HasCoverOriginCell;
            IntVec3 coverOriginCell = CoverOriginCell;
            float overallBlockChance = OverallBlockChance;
            ThingDef randomCoverDef = RandomCover != null ? RandomCover.def : null;
            List<Thing> coverThings = CoverThings;
            Thing randomCover = RandomCover;

            Scribe_Values.Look(ref hasCoverOriginCell, "hasCoverOriginCell", false);
            Scribe_Values.Look(ref coverOriginCell, "coverOriginCell", IntVec3.Invalid);
            Scribe_Values.Look(ref overallBlockChance, "overallBlockChance", 0f);
            Scribe_Defs.Look(ref randomCoverDef, "randomCoverDef");
            Scribe_Collections.Look(ref coverThings, "coverThings", LookMode.Reference);
            Scribe_References.Look(ref randomCover, "randomCover");

            HasCoverOriginCell = hasCoverOriginCell;
            CoverOriginCell = coverOriginCell;
            OverallBlockChance = overallBlockChance;
            Covers = Covers ?? new List<CoverInfo>();
            CoverThings = coverThings ?? new List<Thing>();
            RandomCover = randomCover;
        }
    }
}
