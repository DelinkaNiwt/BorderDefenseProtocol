using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Verbs;
using Verse;

namespace BDP.Core.VerbHosting
{
    /// <summary>
    /// TriggerBody 内部正式宿主绑定管理器。
    /// 它只维护固定宿主槽位与当前表达结果的绑定关系，不再承担运行时 Verb 工厂职责。
    /// </summary>
    internal sealed class TriggerBodyVerbHostManager
    {
        /// <summary>
        /// 当前宿主管理器所属的 TriggerBody owner。
        /// </summary>
        private readonly CompTriggerBody owner;

        /// <summary>
        /// 当前 owner 下全部固定正式宿主槽位的绑定表。
        /// </summary>
        private readonly Dictionary<BdpFormalVerbHostSlot, BdpFormalVerbBinding> bindings =
            new Dictionary<BdpFormalVerbHostSlot, BdpFormalVerbBinding>();

        /// <summary>
        /// 当前仍需要持续 VerbTick 的 formal host 槽位队列。
        /// steady-state 下只遍历它，不再每 tick 全扫全部 binding。
        /// </summary>
        private readonly List<BdpFormalVerbHostSlot> activeVerbsForTick =
            new List<BdpFormalVerbHostSlot>();

        /// <summary>
        /// 活跃 formal host 队列的去重索引。
        /// 用固定槽位身份去重，避免同一槽位重复入队。
        /// </summary>
        private readonly HashSet<BdpFormalVerbHostSlot> activeVerbSet =
            new HashSet<BdpFormalVerbHostSlot>();

        /// <summary>
        /// formal host 远程壳的固定存档列表。
        /// 存档顺序严格对齐 CompTriggerBody.FormalHostSlots。
        /// </summary>
        private List<BdpVerb_FormalHostShoot> rangedShells;

        /// <summary>
        /// formal host 近战壳的固定存档列表。
        /// 存档顺序严格对齐 CompTriggerBody.FormalHostSlots。
        /// </summary>
        private List<BdpVerb_FormalHostMelee> meleeShells;

        /// <summary>
        /// 使用指定 owner 构造正式宿主绑定管理器。
        /// </summary>
        public TriggerBodyVerbHostManager(CompTriggerBody owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// 按当前已发布战斗投影刷新全部绑定状态。
        /// formal host 壳由 BDP 内部长期持有，不再依赖原版 VerbTracker 枚举。
        /// </summary>
        public void Refresh(TriggerCombatProjectionState projection)
        {
            EnsureFormalBindings();
            ResetBindings();

            if (projection?.ResultIdToFormalSlot != null && projection.ResultIndex != null)
            {
                foreach (KeyValuePair<string, BdpFormalVerbHostSlot> pair in projection.ResultIdToFormalSlot)
                {
                    FormalExpressionResult result;
                    if (!projection.ResultIndex.TryGetValue(pair.Key, out result))
                    {
                        continue;
                    }

                    BdpFormalVerbBindingState bindingState;
                    if (!TryBuildBindingState(result, pair.Value, out bindingState))
                    {
                        continue;
                    }

                    bindingState.SessionToken = AttackSessionToken.Create(
                        owner != null ? owner.OwnerPawn : null,
                        result.Id,
                        projection.ProjectionVersion);

                    BdpFormalVerbBinding binding = TryGetBinding(bindingState.Slot);
                    if (binding == null)
                    {
                        continue;
                    }

                    binding.State = bindingState;
                }
            }

            SyncFormalShells();
        }

        /// <summary>
        /// 清空当前 owner 的正式宿主绑定状态。
        /// 正式宿主壳仍由 BDP 内部持有，只是全部回到未绑定状态。
        /// </summary>
        public void Clear()
        {
            EnsureFormalBindings();
            ResetBindings();
            SyncFormalShells();
        }

        /// <summary>
        /// 推进 BDP 内部 formal host 壳的最小 verb 生命周期。
        /// 这些壳已经不在原版 VerbTracker.AllVerbs 中，必须由 BDP 自己补上 VerbTick。
        /// </summary>
        public void Tick()
        {
            EnsureFormalBindings();
            RepairActiveQueueIfLoadedRuntimeMissing();

            for (int i = activeVerbsForTick.Count - 1; i >= 0; i--)
            {
                BdpFormalVerbHostSlot slot = activeVerbsForTick[i];
                BdpFormalVerbBinding binding = TryGetBinding(slot);
                if (binding == null || !binding.ShouldTickAsFormalHost())
                {
                    RemoveActiveSlotAt(i);
                    continue;
                }

                if (binding.RangedVerb != null && binding.RangedVerb.ShouldTickAsFormalHost())
                {
                    binding.RangedVerb.VerbTick();
                }

                if (binding.MeleeVerb != null && binding.MeleeVerb.ShouldTickAsFormalHost())
                {
                    binding.MeleeVerb.VerbTick();
                }

                if (!binding.ShouldTickAsFormalHost())
                {
                    RemoveActiveSlotAt(i);
                }
            }
        }

        /// <summary>
        /// 修复读档恢复后“verb 自身仍有运行态，但 active tick 队列为空”的最小失配。
        /// 正常稳态队列非空时直接返回，避免每 tick 全表扫描。
        /// </summary>
        private void RepairActiveQueueIfLoadedRuntimeMissing()
        {
            if (activeVerbsForTick.Count > 0)
            {
                return;
            }

            foreach (KeyValuePair<BdpFormalVerbHostSlot, BdpFormalVerbBinding> pair in bindings)
            {
                if (pair.Value == null || !pair.Value.ShouldTickAsFormalHost())
                {
                    continue;
                }

                activeVerbSet.Add(pair.Key);
                activeVerbsForTick.Add(pair.Key);
            }
        }

        /// <summary>
        /// 当 formal host 壳成功起手一轮真实会话后，把对应固定槽位加入活跃 tick 队列。
        /// </summary>
        internal void NotifyFormalHostSessionStarted(BdpFormalVerbHostSlot slot)
        {
            EnsureFormalBindings();
            TryAddActiveSlot(slot);
        }

        /// <summary>
        /// 按固定槽位读取当前正式绑定。
        /// </summary>
        public BdpFormalVerbBinding TryGetBinding(BdpFormalVerbHostSlot slot)
        {
            bindings.TryGetValue(slot, out BdpFormalVerbBinding binding);
            return binding;
        }

        /// <summary>
        /// 按正式结果标识读取当前绑定。
        /// </summary>
        public bool TryGetByResultId(string resultId, out BdpFormalVerbBinding binding)
        {
            binding = null;
            if (string.IsNullOrWhiteSpace(resultId))
            {
                return false;
            }

            EnsureFormalBindings();
            foreach (KeyValuePair<BdpFormalVerbHostSlot, BdpFormalVerbBinding> pair in bindings)
            {
                BdpFormalVerbBinding current = pair.Value;
                if (current != null
                    && current.IsAvailable
                    && current.ResultId == resultId)
                {
                    binding = current;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 把 fixed formal host 壳接入 owner 的存档树。
        /// 这里只存壳对象本体，不引入额外的持久化 DTO。
        /// </summary>
        public void ExposeVerbShells()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                EnsureFormalBindings();
                rangedShells = BuildOrderedRangedShellList();
                meleeShells = BuildOrderedMeleeShellList();
            }

            Scribe_Collections.Look(ref rangedShells, "formalHostRangedShells", LookMode.Deep);
            Scribe_Collections.Look(ref meleeShells, "formalHostMeleeShells", LookMode.Deep);
        }

        /// <summary>
        /// 把已从存档恢复回来的壳重新挂回 fixed slot binding。
        /// 这里只恢复对象连续性，不在这里重算表达结果。
        /// </summary>
        public void RestoreShellsPostLoad()
        {
            EnsureFormalBindings();
            for (int i = 0; i < CompTriggerBody.FormalHostSlots.Length; i++)
            {
                BdpFormalVerbHostSlot slot = CompTriggerBody.FormalHostSlots[i];
                BdpFormalVerbBinding binding = TryGetBinding(slot);
                if (binding == null)
                {
                    continue;
                }

                BdpVerb_FormalHostShoot rangedShell = rangedShells != null && i < rangedShells.Count
                    ? rangedShells[i]
                    : null;
                BdpVerb_FormalHostMelee meleeShell = meleeShells != null && i < meleeShells.Count
                    ? meleeShells[i]
                    : null;

                binding.RangedVerb = rangedShell ?? binding.RangedVerb ?? CreateRangedShell(slot);
                binding.MeleeVerb = meleeShell ?? binding.MeleeVerb ?? CreateMeleeShell(slot);

                if (rangedShell != null)
                {
                    binding.RangedVerb.MarkPreserveLoadedStateOnce();
                }

                if (meleeShell != null)
                {
                    binding.MeleeVerb.MarkPreserveLoadedStateOnce();
                }

                binding.RangedVerb?.InitializeFormalHost(owner, slot);
                binding.MeleeVerb?.InitializeFormalHost(owner, slot);
                binding.RangedVerb?.ApplyPostLoadFallbackSurface(owner.GetFormalHostFallbackVerbProps(slot, WeaponExpressionMode.Ranged));
                binding.MeleeVerb?.ApplyPostLoadFallbackSurface(owner.GetFormalHostFallbackVerbProps(slot, WeaponExpressionMode.Melee));
            }

            RebuildActiveVerbQueue();
        }

        /// <summary>
        /// 确保固定正式宿主绑定表和 BDP 自持的正式壳 verb 已经接上线。
        /// </summary>
        private void EnsureFormalBindings()
        {
            if (owner == null)
            {
                return;
            }

            for (int i = 0; i < CompTriggerBody.FormalHostSlots.Length; i++)
            {
                BdpFormalVerbHostSlot slot = CompTriggerBody.FormalHostSlots[i];
                if (!bindings.ContainsKey(slot))
                {
                    bindings.Add(slot, new BdpFormalVerbBinding
                    {
                        Slot = slot,
                        State = CreateUnavailableState(slot),
                        RangedVerb = CreateRangedShell(slot),
                        MeleeVerb = CreateMeleeShell(slot)
                    });
                }
                else
                {
                    BdpFormalVerbBinding binding = bindings[slot];
                    if (binding.RangedVerb == null)
                    {
                        binding.RangedVerb = CreateRangedShell(slot);
                    }

                    if (binding.MeleeVerb == null)
                    {
                        binding.MeleeVerb = CreateMeleeShell(slot);
                    }
                }
            }
        }

        /// <summary>
        /// 以 fixed slot 顺序导出远程壳列表。
        /// </summary>
        private List<BdpVerb_FormalHostShoot> BuildOrderedRangedShellList()
        {
            List<BdpVerb_FormalHostShoot> shells = new List<BdpVerb_FormalHostShoot>();
            for (int i = 0; i < CompTriggerBody.FormalHostSlots.Length; i++)
            {
                BdpFormalVerbBinding binding = TryGetBinding(CompTriggerBody.FormalHostSlots[i]);
                shells.Add(binding?.RangedVerb);
            }

            return shells;
        }

        /// <summary>
        /// 以 fixed slot 顺序导出近战壳列表。
        /// </summary>
        private List<BdpVerb_FormalHostMelee> BuildOrderedMeleeShellList()
        {
            List<BdpVerb_FormalHostMelee> shells = new List<BdpVerb_FormalHostMelee>();
            for (int i = 0; i < CompTriggerBody.FormalHostSlots.Length; i++)
            {
                BdpFormalVerbBinding binding = TryGetBinding(CompTriggerBody.FormalHostSlots[i]);
                shells.Add(binding?.MeleeVerb);
            }

            return shells;
        }

        /// <summary>
        /// 把全部绑定重置为未绑定状态。
        /// </summary>
        private void ResetBindings()
        {
            ClearActiveVerbQueue();
            foreach (KeyValuePair<BdpFormalVerbHostSlot, BdpFormalVerbBinding> pair in bindings)
            {
                pair.Value.State = CreateUnavailableState(pair.Key);
            }
        }

        /// <summary>
        /// 把当前 binding 状态同步回 BDP 内部持有的正式壳 verb。
        /// </summary>
        private void SyncFormalShells()
        {
            foreach (KeyValuePair<BdpFormalVerbHostSlot, BdpFormalVerbBinding> pair in bindings)
            {
                BdpFormalVerbBinding binding = pair.Value;
                if (binding == null)
                {
                    continue;
                }

                bool bindRanged = binding.State != null
                    && binding.State.IsAvailable
                    && binding.State.WeaponMode != WeaponExpressionMode.Melee;
                bool bindMelee = binding.State != null
                    && binding.State.IsAvailable
                    && binding.State.WeaponMode == WeaponExpressionMode.Melee;

                if (binding.RangedVerb != null)
                {
                    binding.RangedVerb.InitializeFormalHost(owner, binding.Slot);
                    binding.RangedVerb.SyncFormalBinding(
                        bindRanged ? binding.State : CreateUnavailableState(binding.Slot),
                        owner.GetFormalHostFallbackVerbProps(binding.Slot, WeaponExpressionMode.Ranged));
                }

                if (binding.MeleeVerb != null)
                {
                    binding.MeleeVerb.InitializeFormalHost(owner, binding.Slot);
                    binding.MeleeVerb.SyncFormalBinding(
                        bindMelee ? binding.State : CreateUnavailableState(binding.Slot),
                        owner.GetFormalHostFallbackVerbProps(binding.Slot, WeaponExpressionMode.Melee));
                }
            }

            RebuildActiveVerbQueue();
        }

        /// <summary>
        /// 按当前 formal host 壳自身持有的最小运行时状态，重建活跃 tick 队列。
        /// 这里允许在 refresh / post-load 这样的低频路径里线性扫描一次全部固定槽位。
        /// </summary>
        private void RebuildActiveVerbQueue()
        {
            ClearActiveVerbQueue();
            foreach (KeyValuePair<BdpFormalVerbHostSlot, BdpFormalVerbBinding> pair in bindings)
            {
                if (pair.Value != null && pair.Value.ShouldTickAsFormalHost())
                {
                    activeVerbSet.Add(pair.Key);
                    activeVerbsForTick.Add(pair.Key);
                }
            }
        }

        /// <summary>
        /// 清空当前活跃 formal host 队列。
        /// </summary>
        private void ClearActiveVerbQueue()
        {
            activeVerbSet.Clear();
            activeVerbsForTick.Clear();
        }

        /// <summary>
        /// 尝试把一个 fixed slot 加入活跃 formal host 队列。
        /// </summary>
        private void TryAddActiveSlot(BdpFormalVerbHostSlot slot)
        {
            if (slot == BdpFormalVerbHostSlot.None || activeVerbSet.Contains(slot))
            {
                return;
            }

            BdpFormalVerbBinding binding = TryGetBinding(slot);
            if (binding == null || !binding.ShouldTickAsFormalHost())
            {
                return;
            }

            activeVerbSet.Add(slot);
            activeVerbsForTick.Add(slot);
        }

        /// <summary>
        /// 从活跃 formal host 队列中移除指定索引位置的槽位。
        /// </summary>
        private void RemoveActiveSlotAt(int activeIndex)
        {
            if (activeIndex < 0 || activeIndex >= activeVerbsForTick.Count)
            {
                return;
            }

            BdpFormalVerbHostSlot slot = activeVerbsForTick[activeIndex];
            activeVerbsForTick.RemoveAt(activeIndex);
            activeVerbSet.Remove(slot);
        }

        /// <summary>
        /// 创建一条 BDP 内部长期持有的远程 formal host 壳。
        /// </summary>
        private BdpVerb_FormalHostShoot CreateRangedShell(BdpFormalVerbHostSlot slot)
        {
            BdpVerb_FormalHostShoot verb = new BdpVerb_FormalHostShoot();
            verb.InitializeFormalHost(owner, slot);
            verb.SyncFormalBinding(CreateUnavailableState(slot), owner.GetFormalHostFallbackVerbProps(slot, WeaponExpressionMode.Ranged));
            return verb;
        }

        /// <summary>
        /// 创建一条 BDP 内部长期持有的近战 formal host 壳。
        /// </summary>
        private BdpVerb_FormalHostMelee CreateMeleeShell(BdpFormalVerbHostSlot slot)
        {
            BdpVerb_FormalHostMelee verb = new BdpVerb_FormalHostMelee();
            verb.InitializeFormalHost(owner, slot);
            verb.SyncFormalBinding(CreateUnavailableState(slot), owner.GetFormalHostFallbackVerbProps(slot, WeaponExpressionMode.Melee));
            return verb;
        }

        /// <summary>
        /// 尝试把一条正式结果解析到 formal host 固定槽位。
        /// 这条映射会同时服务运行时发布索引和 binding 刷新。
        /// </summary>
        internal static bool TryResolveFormalHostSlot(FormalExpressionResult result, out BdpFormalVerbHostSlot slot)
        {
            slot = BdpFormalVerbHostSlot.None;
            if (result == null)
            {
                return false;
            }

            if (result.CompositeKind == CompositeExpressionKind.DualWeapon)
            {
                slot = result.VerbAttackRole == VerbAttackRole.Secondary
                    ? BdpFormalVerbHostSlot.DualSecondary
                    : BdpFormalVerbHostSlot.DualPrimary;
                return true;
            }

            if (result.CompositeKind == CompositeExpressionKind.Combo)
            {
                slot = result.VerbAttackRole == VerbAttackRole.Secondary
                    ? BdpFormalVerbHostSlot.ComboSecondary
                    : BdpFormalVerbHostSlot.ComboPrimary;
                return true;
            }

            if (result.OriginKind == ExpressionOriginKind.Main)
            {
                slot = result.VerbAttackRole == VerbAttackRole.Secondary
                    ? BdpFormalVerbHostSlot.MainSecondary
                    : BdpFormalVerbHostSlot.MainPrimary;
                return true;
            }

            if (result.OriginKind == ExpressionOriginKind.Sub)
            {
                slot = result.VerbAttackRole == VerbAttackRole.Secondary
                    ? BdpFormalVerbHostSlot.SubSecondary
                    : BdpFormalVerbHostSlot.SubPrimary;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 把一条正式结果翻译成正式宿主 binding 状态。
        /// </summary>
        private static bool TryBuildBindingState(FormalExpressionResult result, out BdpFormalVerbBindingState bindingState)
        {
            bindingState = null;
            if (result == null
                || result.ResultKind != ExpressionResultKind.Verb
                || string.IsNullOrWhiteSpace(result.Id)
                || result.VerbProps == null)
            {
                return false;
            }

            BdpFormalVerbHostSlot slot;
            if (!TryResolveFormalHostSlot(result, out slot))
            {
                return false;
            }

            return TryBuildBindingState(result, slot, out bindingState);
        }

        /// <summary>
        /// 用已解析好的固定槽位构建正式宿主 binding 状态。
        /// </summary>
        private static bool TryBuildBindingState(
            FormalExpressionResult result,
            BdpFormalVerbHostSlot slot,
            out BdpFormalVerbBindingState bindingState)
        {
            bindingState = null;
            if (result == null
                || slot == BdpFormalVerbHostSlot.None
                || result.ResultKind != ExpressionResultKind.Verb
                || string.IsNullOrWhiteSpace(result.Id)
                || result.VerbProps == null)
            {
                return false;
            }

            bindingState = new BdpFormalVerbBindingState
            {
                Slot = slot,
                ResultId = result.Id,
                SessionToken = null,
                IsAvailable = result.IsAvailable,
                WeaponMode = result.WeaponMode,
                VerbProps = result.VerbProps,
                Tool = result.Tool,
                DeclaredTools = result.DeclaredTools,
                DeclaredMeleeToolSurfaces = result.DeclaredMeleeToolSurfaces,
                Maneuver = result.Maneuver
            };
            return true;
        }

        /// <summary>
        /// 创建一条未绑定状态的最小 binding state。
        /// </summary>
        private static BdpFormalVerbBindingState CreateUnavailableState(BdpFormalVerbHostSlot slot)
        {
            return new BdpFormalVerbBindingState
            {
                Slot = slot,
                ResultId = null,
                SessionToken = null,
                IsAvailable = false,
                WeaponMode = WeaponExpressionMode.Ranged,
                VerbProps = null,
                Tool = null,
                DeclaredTools = null,
                DeclaredMeleeToolSurfaces = null,
                Maneuver = null
            };
        }

    }
}
