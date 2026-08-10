using System;
using System.Collections.Generic;

namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 正式服务。
    /// 它同时承接正式读取、正式请求和正式事件，不再把单一宿主拆成多层表面。
    /// </summary>
    internal sealed class TrionService : ITrionReader, ITrionCommands, ITrionEvents
    {
        /// <summary>
        /// Trion 真值宿主。
        /// </summary>
        private readonly CompTrion owner;

        /// <summary>
        /// 绑定到指定的 Trion 宿主。
        /// </summary>
        public TrionService(CompTrion owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// 转发当前总量。
        /// </summary>
        public float Cur
        {
            get { return owner.Cur; }
        }

        /// <summary>
        /// 转发最大容量。
        /// </summary>
        public float Max
        {
            get { return owner.Max; }
        }

        /// <summary>
        /// 转发已正式占用量。
        /// </summary>
        public float Allocated
        {
            get { return owner.Allocated; }
        }

        /// <summary>
        /// 转发预占用量。
        /// </summary>
        public float Reserved
        {
            get { return owner.Reserved; }
        }

        /// <summary>
        /// 转发可自由使用量。
        /// </summary>
        public float Available
        {
            get { return owner.Available; }
        }

        /// <summary>
        /// 转发当前每日自然恢复量。
        /// </summary>
        public float RecoveryPerDay
        {
            get { return owner.RecoveryPerDay; }
        }

        /// <summary>
        /// 转发聚合持续消耗速率。
        /// </summary>
        public float TotalDrainPerSecond
        {
            get { return owner.TotalDrainPerSecond; }
        }

        /// <summary>
        /// 转发自然恢复冻结状态。
        /// </summary>
        public bool Frozen
        {
            get { return owner.Frozen; }
        }

        /// <summary>
        /// 代宿主判断这次消耗是否付得起。
        /// </summary>
        public bool CanAfford(float cost)
        {
            return owner.CanAfford(cost);
        }

        /// <summary>
        /// 代宿主尝试一次性扣减。
        /// </summary>
        public bool TryConsume(float cost)
        {
            return owner.TryConsume(cost);
        }

        /// <summary>
        /// 代宿主做尽力扣减。
        /// </summary>
        public void ConsumeUntilDepleted(float amount)
        {
            owner.ConsumeUntilDepleted(amount);
        }

        /// <summary>
        /// 代宿主更新预占用量。
        /// </summary>
        public void SetReserved(float amount)
        {
            owner.SetReserved(amount);
        }

        /// <summary>
        /// 代宿主锁定正式占用量。
        /// </summary>
        public bool Allocate(float amount)
        {
            return owner.Allocate(amount);
        }

        /// <summary>
        /// 代宿主释放正式占用量。
        /// </summary>
        public void Release(float amount)
        {
            owner.Release(amount);
        }

        /// <summary>
        /// 代宿主注册持续消耗来源。
        /// </summary>
        public void RegisterDrain(TrionDrainKey key, float perSecond)
        {
            owner.RegisterDrain(key, perSecond);
        }

        /// <summary>
        /// 转发永久潜在容量。
        /// </summary>
        public int TrionCapacityPotential
        {
            get { return owner.TrionCapacityPotential; }
        }

        /// <summary>
        /// 转发永久先天 Trion 释放力。
        /// </summary>
        public int InnateTrionIntensity
        {
            get { return owner.InnateTrionIntensity; }
        }

        /// <summary>
        /// 代宿主注销持续消耗来源。
        /// </summary>
        public void UnregisterDrain(TrionDrainKey key)
        {
            owner.UnregisterDrain(key);
        }

        /// <summary>
        /// 读取宿主当前的持续消耗登记表。
        /// </summary>
        public IReadOnlyDictionary<TrionDrainKey, float> GetDrainSnapshot()
        {
            return owner.GetDrainSnapshot();
        }

        /// <summary>
        /// 代宿主设置自然恢复冻结状态。
        /// </summary>
        public void SetFrozen(bool frozen)
        {
            owner.SetFrozen(frozen);
        }

        /// <summary>
        /// 代宿主按增减量调整当前值。
        /// </summary>
        public TrionCurrentWriteResult AdjustCurrent(float delta)
        {
            return owner.AdjustCurrent(delta);
        }

        /// <summary>
        /// 代宿主尝试直接设置当前值。
        /// </summary>
        public TrionCurrentWriteResult TrySetCurrent(float target)
        {
            return owner.TrySetCurrent(target);
        }

        /// <summary>
        /// 订阅或取消订阅“可用量见底”事件。
        /// </summary>
        public event Action AvailableDepleted
        {
            add { owner.AvailableDepleted += value; }
            remove { owner.AvailableDepleted -= value; }
        }

        /// <summary>
        /// 订阅或取消订阅“总量见底”事件。
        /// </summary>
        public event Action TrionDepleted
        {
            add { owner.TrionDepleted += value; }
            remove { owner.TrionDepleted -= value; }
        }
    }
}
