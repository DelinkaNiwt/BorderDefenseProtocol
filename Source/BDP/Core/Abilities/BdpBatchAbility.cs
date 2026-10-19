using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace BDP.Core.Abilities
{
    /// <summary>一批目标只激活一次；次数、恢复计时和存档仍归原版能力。</summary>
    public class BdpBatchAbility : Ability
    {
        /// <summary>仅在本轮已验证动作中允许激活，防止直接调用绕过整批验证。</summary>
        private bool preparedCast;

        /// <summary>支付前冻结的一次同步动作；支付耗尽触力能可能同步结束当前作业。</summary>
        private IReadOnlyList<IntVec3> preparedTargets;

        /// <summary>与已验证目标同时冻结的地图，不在支付后重新依赖角色作业。</summary>
        private Map preparedMap;

        /// <summary>保留原版无参构造入口。</summary>
        public BdpBatchAbility() { }
        /// <summary>原版角色能力列表深存档会传入角色，读档必须提供此构造。</summary>
        public BdpBatchAbility(Pawn pawn) : base(pawn) { }
        /// <summary>原版能力工厂构造。</summary>
        public BdpBatchAbility(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        /// <summary>批量作业必须单发、单地图、单次激活；组冷却会重置原生逐次恢复进度。</summary>
        private bool SupportedConfiguration => verb is BdpVerb_CastBatchAbility
            && def.verbProperties.burstShotCount == 1 && !def.verbProperties.nonInterruptingSelfCast
            && !def.targetWorldCell && !def.waitForJobEnd && def.groupDef == null
            && (def.jobDef == null || def.jobDef == JobDefOf.CastAbilityOnThing)
            && !(def.HasAreaOfEffect && def.canUseAoeToGetTargets)
            && (!UsesCharges || (def.cooldownPerCharge && def.cooldownTicksRange.min > 0));

        /// <summary>仅显式配置的能力跳过非法中间格，不依据芯片业务名称判断。</summary>
        public bool SkipsInvalidInteriorTargets =>
            def.GetModExtension<BdpBatchTargetingExtension>()?.skipInvalidInteriorTargets == true;

        /// <summary>第一版同一能力只允许一份在途请求，避免原版按一作业一次计数而超额排队。</summary>
        public override bool CanQueueCast => base.CanQueueCast
            && !pawn.jobs.AllJobs().Any(job => job.ability == this || job.verbToUse == verb);

        /// <summary>支付前读取完整作业候选；同步支付与效果期间只读本轮实际生成目标。</summary>
        public IReadOnlyList<IntVec3> CurrentBatchTargets
        {
            get
            {
                if (preparedCast) return preparedTargets;
                Job job = pawn?.CurJob;
                if (job?.ability != this || job.verbToUse != verb || job.targetQueueA == null)
                    return new List<IntVec3>().AsReadOnly();
                return job.targetQueueA.Select(target => target.Cell).Distinct().ToList().AsReadOnly();
            }
        }

        /// <summary>当前批次地图；效果应与冻结目标共同使用此地图。</summary>
        public Map CurrentBatchMap => preparedCast ? preparedMap : pawn?.Map;

        /// <summary>验证整批几何和支付预算；预览阶段没有资源或地图副作用。</summary>
        public bool TryValidateBatch(IEnumerable<IntVec3> cells, out List<IntVec3> targets, out string reason)
        {
            var candidates = cells != null ? cells.Distinct().ToList() : new List<IntVec3>();
            targets = candidates;
            reason = "BDP_BatchAbility_InvalidRequest".Translate();
            if (pawn == null || !pawn.Spawned || candidates.Count == 0 || !CanCast
                || !SupportedConfiguration) return false;
            bool skipInterior = SkipsInvalidInteriorTargets;
            var validity = candidates.Select((cell, index) => IsBatchCellValid(cell,
                !skipInterior || index == 0 || index == candidates.Count - 1)).ToList();
            List<int> indices;
            if (!BatchAbilityTargetSelection.TrySelect(validity, skipInterior, out indices))
            {
                reason = (skipInterior && (!validity[0] || !validity[validity.Count - 1])
                    ? "BDP_BatchAbility_InvalidEndpoints" : "BDP_BatchAbility_InvalidCell").Translate();
                return false;
            }
            targets = indices.Select(index => candidates[index]).ToList();
            if (UsesCharges && targets.Count > RemainingCharges)
            {
                reason = "BDP_BatchAbility_NotEnoughCharges".Translate(targets.Count, RemainingCharges);
                return false;
            }
            return BdpAbilityTrionCostCommitter.CanAfford(this, targets.Count, out reason);
        }

        /// <summary>端点使用完整动作校验；中间只检查距离、地图与效果，允许越过遮挡。</summary>
        internal bool IsBatchCellValid(IntVec3 cell, bool validateVerb)
        {
            Map map = pawn?.Map;
            if (map == null || !cell.IsValid || !cell.InBounds(map) || cell.Fogged(map)) return false;
            var target = new LocalTargetInfo(cell);
            if (!CanApplyOn(target)) return false;
            if (validateVerb) return verb.ValidateTarget(target, false);
            if (verb.EffectiveRange > 0f && verb.OutOfRange(pawn.Position, target, CellRect.SingleCell(cell))) return false;
            return EffectComps.All(effect => effect.Valid(target, false));
        }

        /// <summary>冻结目标到原版作业队列，随作业存档；未确认前不扣费。</summary>
        public bool QueueBatch(IEnumerable<IntVec3> cells)
        {
            List<IntVec3> targets;
            string reason;
            var candidates = cells != null ? cells.Distinct().ToList() : new List<IntVec3>();
            if (!CanQueueCast || !TryValidateBatch(candidates, out targets, out reason)) return false;
            Job job = GetJob(new LocalTargetInfo(candidates[0]), LocalTargetInfo.Invalid);
            // 保存完整线而非预览筛选结果；前摇中障碍增减后必须重新计算实际目标与预算。
            job.targetQueueA = candidates.Select(cell => new LocalTargetInfo(cell)).ToList();
            return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        /// <summary>原版单点命令也转成一格批次，不创建没有目标列表的无效作业。</summary>
        public override void QueueCastingJob(LocalTargetInfo target, LocalTargetInfo destination)
        {
            if (!target.HasThing && !destination.IsValid) QueueBatch(new[] { target.Cell });
        }

        /// <summary>尚无人工智能批量选格策略，不能误用原版单点自动施放。</summary>
        public override bool AICanTargetNow(LocalTargetInfo target) => false;

        /// <summary>范围只覆盖同步的一次实际施放，不能成为另一份持久请求状态。</summary>
        internal bool ExecutePreparedCast(IReadOnlyList<IntVec3> targets, Func<bool> cast)
        {
            if (preparedCast || targets == null || targets.Count == 0 || pawn?.Map == null) return false;
            preparedTargets = targets.ToList().AsReadOnly();
            preparedMap = pawn.Map;
            preparedCast = true;
            try { return cast(); }
            finally
            {
                preparedCast = false;
                preparedTargets = null;
                preparedMap = null;
            }
        }

        /// <summary>外部不能以单点激活绕过整批请求；正式动作已经在扣费前完成验证。</summary>
        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!preparedCast || CurrentBatchTargets.Count == 0) return false;
            // 最后一份触力能也能完成已支付施放，再承受崩解；不因同步结束作业吞掉效果。
            return base.Activate(new LocalTargetInfo(preparedTargets[0]), LocalTargetInfo.Invalid);
        }

        /// <summary>地图格批次不接受世界地图激活，避免绕开批量验证并扣错次数。</summary>
        public override bool Activate(GlobalTargetInfo target) => false;

        /// <summary>额外扣 N−1 次，原版再扣一次并启动逐次恢复；已有恢复进度不重开。</summary>
        protected override void PreActivate(LocalTargetInfo? target)
        {
            if (UsesCharges) RemainingCharges -= CurrentBatchTargets.Count - 1;
            base.PreActivate(target);
        }
    }
}
