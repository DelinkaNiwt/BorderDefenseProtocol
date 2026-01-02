using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Aldon.Energy
{
    /// <summary>
    /// 能量容器组件 - 挂载在殖民者身上
    /// 管理: 容量、消耗、恢复、锁定
    /// </summary>
    public class AldonEnergyComp : ThingComp
    {
        // ===== 常量 =====
        public const int CAPACITY = 1000;
        public const int RECOVERY_RATE_PER_SECOND = 2;  // 每秒恢复2点

        // ===== 状态数据 =====
        private int usedEnergy = 0;      // 消耗待恢复的能量
        private int lockedEnergy = 0;    // 被装备占用的容量
        private Dictionary<string, int> locks = new Dictionary<string, int>();

        // ===== Tick驱动 =====
        private int tickCounter = 0;
        private const int TICKS_PER_RECOVERY = 30;  // 60 ticks/sec ÷ 2点/sec = 30 ticks/point

        // ===== 计算属性 =====
        public int Available => CAPACITY - lockedEnergy - usedEnergy;
        public int LockedEnergy => lockedEnergy;
        public int UsedEnergy => usedEnergy;
        public int CurrentCapacity => CAPACITY;
        public float RecoveryPercent => (float)(CAPACITY - usedEnergy) / CAPACITY;

        // ===== 生命周期 =====

        /// <summary>
        /// 每帧Tick - 处理能量恢复
        /// </summary>
        public override void CompTick()
        {
            base.CompTick();

            // 仅在能量未满时处理恢复
            if (usedEnergy > 0)
            {
                // 检查全局信号
                if (!AldonSignalManager.Instance.IsActive)
                {
                    return;  // 信号关闭时不恢复
                }

                // Tick累计，达到恢复间隔时执行恢复
                tickCounter++;
                if (tickCounter >= TICKS_PER_RECOVERY)
                {
                    AddEnergy(1);
                    tickCounter = 0;
                }
            }
        }

        // ===== 核心操作 =====

        /// <summary>
        /// 恢复能量
        /// </summary>
        /// <param name="amount">恢复量</param>
        public void AddEnergy(int amount)
        {
            if (amount <= 0) return;

            usedEnergy = System.Math.Max(0, usedEnergy - amount);

            // 开发模式下输出详细日志
            if (Prefs.DevMode && parent is Pawn pawn)
            {
                Log.Message($"[Aldon] {pawn.Name.ToStringShort} 恢复能量 +{amount}，当前可用: {Available}");
            }
        }

        /// <summary>
        /// 尝试消耗能量
        /// 返回: true=成功消耗, false=权限不足或能量不足
        /// </summary>
        /// <param name="amount">消耗量</param>
        public bool TryConsume(int amount)
        {
            if (amount <= 0)
            {
                Log.Warning("[Aldon] TryConsume: 消耗量必须 > 0");
                return false;
            }

            Pawn pawn = parent as Pawn;
            if (pawn == null) return false;

            // 检查权限
            AldonPermissionComp permComp = pawn.GetComp<AldonPermissionComp>();
            if (permComp == null || !permComp.HasPermission)
            {
                // 低权限消耗失败，不显示错误（避免刷屏）
                return false;
            }

            // 检查信号
            if (!AldonSignalManager.Instance.IsActive)
            {
                Messages.Message("驱动塔未激活，无法消耗能量", MessageTypeDefOf.RejectInput);
                return false;
            }

            // 检查可用能量
            if (Available < amount)
            {
                Messages.Message($"{pawn.Name.ToStringShort} 能量不足", MessageTypeDefOf.RejectInput);
                return false;
            }

            // 执行消耗
            usedEnergy += amount;

            Log.Message($"[Aldon] {pawn.Name.ToStringShort} 消耗能量 -{amount}，剩余: {Available}");
            return true;
        }

        /// <summary>
        /// 注册锁定 - 装备占用容量
        /// 返回: 锁定ID (用于后续释放)
        /// </summary>
        /// <param name="amount">锁定量</param>
        /// <param name="source">来源标识</param>
        public string RegisterLock(int amount, string source)
        {
            if (amount <= 0)
            {
                Log.Warning("[Aldon] RegisterLock: 锁定量必须 > 0");
                return null;
            }

            if (lockedEnergy + amount > CAPACITY)
            {
                Pawn pawn = parent as Pawn;
                Log.Warning($"[Aldon] {pawn?.Name.ToStringShort ?? "Unknown"} 锁定超出容量: 已占用{lockedEnergy} + 请求{amount} > {CAPACITY}");
                return null;
            }

            // 生成锁定ID
            string lockID = source + "_" + System.DateTime.Now.Ticks;

            // 记录
            locks[lockID] = amount;
            lockedEnergy += amount;

            Pawn p = parent as Pawn;
            Log.Message($"[Aldon] {p?.Name.ToStringShort ?? "Unknown"} 注册锁定: {source} = {amount}点，ID: {lockID}");

            return lockID;
        }

        /// <summary>
        /// 释放锁定 - 装备卸下
        /// </summary>
        /// <param name="lockID">锁定ID</param>
        public bool ReleaseLock(string lockID)
        {
            if (locks.ContainsKey(lockID))
            {
                int amount = locks[lockID];
                lockedEnergy -= amount;
                locks.Remove(lockID);

                Pawn pawn = parent as Pawn;
                Log.Message($"[Aldon] {pawn?.Name.ToStringShort ?? "Unknown"} 释放锁定: {lockID} = -{amount}点");
                return true;
            }

            return false;
        }

        // ===== 数据持久化 =====

        /// <summary>
        /// 存档保存/加载
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref usedEnergy, "usedEnergy", 0);
            Scribe_Values.Look(ref lockedEnergy, "lockedEnergy", 0);
            Scribe_Collections.Look(ref locks, "locks", LookMode.Value, LookMode.Value);

            // 加载后确保字典不为null
            if (Scribe.mode == LoadSaveMode.LoadingVars && locks == null)
            {
                locks = new Dictionary<string, int>();
            }
        }
    }
}
