using Verse;
using RimWorld;
using ProjectTrion.Core;
using ProjectTrion.Components;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// Trion腺体 - 植入后自动附加CompTrion到Pawn
    ///
    /// 工作流程：
    /// 1. 玩家进行"植入Trion腺体"手术
    /// 2. 手术成功后，此Hediff被添加到Pawn
    /// 3. PostAdd() 被调用，自动创建并初始化CompTrion
    /// 4. Pawn获得Trion能量系统
    ///
    /// 天赋存储：
    /// - 首次扫描时随机生成天赋，存储在takenTalent字段
    /// - 通过ExposeData()进行保存/加载
    /// - Building_TrionDetector读取此字段确定天赋
    ///
    /// 这个设计避免了全局Harmony补丁，是"投机取巧"的方案：
    /// - 框架不需要拦截Pawn生成
    /// - 植入是可选的，玩家可以选择是否启用
    /// - 符合世界观（天赋在生成时拥有，但腺体是获取方式）
    /// </summary>
    public class Hediff_TrionGland : HediffWithComps
    {
        /// <summary>
        /// 存储该Pawn的Trion天赋等级（首次扫描时初始化）
        /// </summary>
        private TalentGrade takenTalent = TalentGrade.C;

        /// <summary>
        /// 标记天赋是否已被初始化过
        /// </summary>
        private bool talentInitialized = false;

        /// <summary>
        /// 公开天赋属性供Building_TrionDetector读取
        /// </summary>
        public TalentGrade TakenTalent
        {
            get { return takenTalent; }
            set { takenTalent = value; }
        }

        /// <summary>
        /// 公开初始化标记
        /// </summary>
        public bool TalentInitialized
        {
            get { return talentInitialized; }
            set { talentInitialized = value; }
        }

        /// <summary>
        /// 当此Hediff被添加到Pawn时调用
        /// 在这里自动初始化CompTrion
        /// </summary>
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            if (pawn == null)
                return;

            // 检查是否已有CompTrion（防止重复添加）
            var existingComp = pawn.GetComp<CompTrion>();
            if (existingComp != null)
            {
                Log.Warning($"[Trion] {pawn.LabelShort} 已有CompTrion，植入腺体被忽略");
                return;
            }

            try
            {
                // 创建CompProperties配置
                var compProps = new CompProperties_Trion
                {
                    capacity = 1000f, // 初始化容量（临时），在首次扫描时根据天赋重算
                    enableSnapshot = true,
                    enableBailOut = true,
                    strategyClassName = typeof(DefaultTrionStrategy).FullName
                };

                // 创建CompTrion实例
                var comp = new CompTrion();
                comp.props = compProps;
                comp.parent = pawn;

                // 手动调用PostSpawnSetup初始化
                comp.PostSpawnSetup(respawningAfterLoad: false);

                // 将组件添加到Pawn的AllComps列表
                var allComps = pawn.AllComps;
                if (!allComps.Contains(comp))
                {
                    allComps.Add(comp);
                }

                Log.Message($"[Trion] {pawn.Name} 植入Trion腺体，获得Trion能量系统 " +
                    $"(初始Capacity: {comp.Capacity})");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[Trion] 植入{pawn.LabelShort}的Trion腺体时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 当此Hediff被移除时调用
        /// 移除Trion腺体会导致Trion系统停止工作（但CompTrion仍存在于Pawn上）
        /// 这符合"卸载时清理"的原则
        /// </summary>
        public override void PostRemoved()
        {
            base.PostRemoved();

            if (pawn != null)
            {
                Log.Message($"[Trion] {pawn.Name} 的Trion腺体被移除");
            }
        }

        /// <summary>
        /// 保存/加载天赋数据
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref takenTalent, "takenTalent", TalentGrade.C);
            Scribe_Values.Look(ref talentInitialized, "talentInitialized", false);
        }
    }
}
