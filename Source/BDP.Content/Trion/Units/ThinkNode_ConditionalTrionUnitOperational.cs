using Verse;
using Verse.AI;
using RimWorld;

namespace BDP.Content.Trion.Units
{
    /// <summary>薄行为树的运行分流，停机不使用充电作业或人类崩解状态。</summary>
    public sealed class ThinkNode_ConditionalTrionUnitOperational : ThinkNode_Conditional
    {
        /// <summary>只读取兵组件真值，世界状态不依赖这棵地图行为树。</summary>
        protected override bool Satisfied(Pawn pawn) => pawn.TryGetComp<CompTrionUnit>()?.IsOperational == true;
    }

    /// <summary>自主战斗的专用准入和作业来源边界，具体战术由下挂子树决定。</summary>
    public sealed class ThinkNode_ConditionalTrionUnitAutoCombat : ThinkNode_Conditional
    {
        /// <summary>征召、运行、身体资格和本地图敌情都从兵组件的现成事实读取。</summary>
        protected override bool Satisfied(Pawn pawn) => pawn.TryGetComp<CompTrionUnit>()?.ShouldAutoCombat == true;
    }

    /// <summary>基础战术继承原版，只声明原版构造期能识别的抢占优先级。</summary>
    public sealed class JobGiver_TrionUnitAutoCombat : JobGiver_AIFightEnemies
    {
        /// <summary>原版构造时缓存是否覆写该方法；仅在XML后填priority不会更新缓存。</summary>
        public override float GetPriority(Pawn pawn) => CompTrionUnit.AutoCombatOverridePriority;
    }

    /// <summary>停机使用原版原地等待，不伪造健康倒地或低能充电。</summary>
    public sealed class JobGiver_TrionUnitStopped : ThinkNode_JobGiver
    {
        /// <summary>短等待只承担地图表现；真正停机事实保存在角色组件。</summary>
        protected override Job TryGiveJob(Pawn pawn)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
            job.expiryInterval = 60;
            return job;
        }
    }
}
