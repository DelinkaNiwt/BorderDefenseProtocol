using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedProtocol.Aim;
using BDP.Core.AttackExecution.RangedProtocol.Fire;
using BDP.Core.AttackExecution.RangedProtocol.Prepare;
using BDP.Core.AttackExecution.RangedProtocol.ProjectileInit;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Expressions;
using BDP.Core.Projectiles.RangedFlightProtocol.Arrival;
using BDP.Core.Projectiles.RangedFlightProtocol.Flight;
using BDP.Core.Projectiles.RangedFlightProtocol.Hit;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 单次远程攻击的模块运行时会话。
    /// 它冻结模块顺序快照，并为各阶段分发已初始化运行时。
    /// </summary>
    internal sealed class RangedAttackModuleSession
    {
        /// <summary>
        /// 当前攻击会话持有的统一攻击上下文运行态。
        /// </summary>
        private AttackContext attackContext = new AttackContext();

        /// <summary>
        /// 当前攻击绑定的统一攻击上下文。
        /// 模块私有上下文只允许落在这里，不再额外挂独立快照主干。
        /// </summary>
        public AttackContext AttackContext
        {
            get
            {
                return attackContext;
            }

            set
            {
                attackContext = value ?? new AttackContext();
            }
        }

        /// <summary>
        /// 当前攻击的宿主 Pawn。
        /// </summary>
        public Pawn Pawn { get; set; }

        /// <summary>
        /// 当前攻击绑定的正式表达结果。
        /// </summary>
        public FormalExpressionResult Result { get; set; }

        /// <summary>
        /// 当前攻击冻结后的模块挂载顺序快照。
        /// </summary>
        public IReadOnlyList<RangedModuleMountConfig> Mounts { get; set; }

        /// <summary>
        /// 当前攻击已冻结的模块槽位列表。
        /// </summary>
        public IReadOnlyList<RangedAttackModuleSlot> Slots { get; set; }

        /// 当前瞄准过程绑定的目标交互会话。
        /// 它承载上游多轮输入的中性推进状态。
        /// </summary>
        public TargetingInteractionSession TargetingInteractionSession { get; set; }

        /// <summary>
        /// 读取指定模块当前槽位上的私有上下文。
        /// </summary>
        internal T GetPrivateContext<T>(IRangedAttackModuleRuntime runtime)
            where T : class, IRangedModulePrivateContext
        {
            RangedAttackModuleSlot slot = FindSlot(runtime);
            return slot != null && AttackContext != null
                ? AttackContext.Get<T>(AttackContextKeys.GetModulePrivateKey(slot.MountIndex))
                : null;
        }

        /// <summary>
        /// 尝试读取指定模块当前槽位上的私有上下文。
        /// </summary>
        internal bool TryGetPrivateContext<T>(IRangedAttackModuleRuntime runtime, out T context)
            where T : class, IRangedModulePrivateContext
        {
            context = GetPrivateContext<T>(runtime);
            return context != null;
        }

        /// <summary>
        /// 读取或创建指定模块当前槽位上的私有上下文。
        /// </summary>
        internal T GetOrCreatePrivateContext<T>(IRangedAttackModuleRuntime runtime)
            where T : class, IRangedModulePrivateContext, new()
        {
            RangedAttackModuleSlot slot = FindSlot(runtime);
            if (slot == null || AttackContext == null)
            {
                return null;
            }

            return AttackContext.GetOrCreate<T>(AttackContextKeys.GetModulePrivateKey(slot.MountIndex));
        }

        /// <summary>
        /// 读取指定挂载索引上的私有上下文。
        /// </summary>
        internal T GetPrivateContext<T>(int mountIndex)
            where T : class, IRangedModulePrivateContext
        {
            return AttackContext != null
                ? AttackContext.Get<T>(AttackContextKeys.GetModulePrivateKey(mountIndex))
                : null;
        }

        /// <summary>
        /// 把当前会话里的模块私有上下文导出到统一攻击上下文。
        /// 主模组只按槽位索引落键，不解释上下文本体内容。
        /// </summary>
        internal void ExportPrivateContexts(AttackContext attackContext)
        {
            if (attackContext == null || AttackContext == null || Slots == null)
            {
                return;
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                RangedAttackModuleSlot slot = Slots[i];
                if (slot == null)
                {
                    continue;
                }

                IAttackContextNode node = AttackContext.GetNode(AttackContextKeys.GetModulePrivateKey(slot.MountIndex));
                if (node != null)
                {
                    attackContext.Set(AttackContextKeys.GetModulePrivateKey(slot.MountIndex), node.Clone());
                }
            }
        }

        /// <summary>
        /// 从统一攻击上下文快照恢复当前会话对应槽位的模块私有上下文。
        /// 这里只按挂载顺序对位恢复，不建立跨模块共享协议。
        /// </summary>
        internal void ImportPrivateContexts(AttackContextSnapshot attackContextSnapshot)
        {
            if (attackContextSnapshot == null || Slots == null)
            {
                return;
            }

            if (AttackContext == null)
            {
                AttackContext = new AttackContext();
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                RangedAttackModuleSlot slot = Slots[i];
                if (slot == null)
                {
                    continue;
                }

                IAttackContextNode node = attackContextSnapshot.GetNode(AttackContextKeys.GetModulePrivateKey(slot.MountIndex));
                if (node != null)
                {
                    AttackContext.Set(AttackContextKeys.GetModulePrivateKey(slot.MountIndex), node.Clone());
                }
            }
        }

        /// <summary>
        /// 读取或创建当前攻击会话的目标交互会话。
        /// </summary>
        internal TargetingInteractionSession GetOrCreateTargetingInteractionSession()
        {
            if (TargetingInteractionSession == null)
            {
                TargetingInteractionSession = new TargetingInteractionSession();
                TargetingInteractionSession.Activate();
            }

            return TargetingInteractionSession;
        }

        /// <summary>
        /// 读取当前会话可参与 Aim 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IAimStageModule> GetAimModules()
        {
            List<IAimStageModule> result = new List<IAimStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 Prepare 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IPrepareStageModule> GetPrepareModules()
        {
            List<IPrepareStageModule> result = new List<IPrepareStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 Fire 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IFireStageModule> GetFireModules()
        {
            List<IFireStageModule> result = new List<IFireStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 ProjectileInit 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IProjectileInitStageModule> GetProjectileInitModules()
        {
            List<IProjectileInitStageModule> result = new List<IProjectileInitStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 Flight 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IFlightStageModule> GetFlightModules()
        {
            List<IFlightStageModule> result = new List<IFlightStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 Arrival 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IArrivalStageModule> GetArrivalModules()
        {
            List<IArrivalStageModule> result = new List<IArrivalStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 Hit 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IHitStageModule> GetHitModules()
        {
            List<IHitStageModule> result = new List<IHitStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 Impact 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IImpactStageModule> GetImpactModules()
        {
            List<IImpactStageModule> result = new List<IImpactStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 ManualEntry 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IManualEntryStageModule> GetManualEntryModules()
        {
            List<IManualEntryStageModule> result = new List<IManualEntryStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 Targeting 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<ITargetingStageModule> GetTargetingModules()
        {
            List<ITargetingStageModule> result = new List<ITargetingStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 Preview 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IPreviewStageModule> GetPreviewModules()
        {
            List<IPreviewStageModule> result = new List<IPreviewStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与 Confirm 阶段的模块列表。
        /// </summary>
        internal IReadOnlyList<IConfirmStageModule> GetConfirmModules()
        {
            List<IConfirmStageModule> result = new List<IConfirmStageModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 读取当前会话可参与阶段附加挂件的模块列表。
        /// </summary>
        internal IReadOnlyList<IRangedStageAddonModule> GetAddonModules()
        {
            List<IRangedStageAddonModule> result = new List<IRangedStageAddonModule>();
            AppendStageModules(result);
            return result;
        }

        /// <summary>
        /// 从运行时实例列表里筛出实现指定阶段接口的模块。
        /// </summary>
        private void AppendStageModules<TModule>(List<TModule> target)
            where TModule : class
        {
            if (target == null || Slots == null)
            {
                return;
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                TModule module = Slots[i]?.Runtime as TModule;
                if (module != null)
                {
                    target.Add(module);
                }
            }
        }

        /// <summary>
        /// 查找当前运行时实例对应的模块槽位。
        /// </summary>
        private RangedAttackModuleSlot FindSlot(IRangedAttackModuleRuntime runtime)
        {
            if (runtime == null || Slots == null)
            {
                return null;
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                RangedAttackModuleSlot slot = Slots[i];
                if (slot != null && ReferenceEquals(slot.Runtime, runtime))
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// 查找指定挂载顺序索引对应的模块槽位。
        /// </summary>
        private RangedAttackModuleSlot FindSlot(int mountIndex)
        {
            if (Slots == null)
            {
                return null;
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                RangedAttackModuleSlot slot = Slots[i];
                if (slot != null && slot.MountIndex == mountIndex)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
