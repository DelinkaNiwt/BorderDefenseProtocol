using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace BDP.Core
{
    /// <summary>
    /// Trion能量的通用数据容器——微内核的核心组件。
    /// 可挂载到任意ThingWithComps（Pawn、Building、Item）。
    /// 唯一的Trion数据源（Single Source of Truth）。
    ///
    /// v1.5变更：
    ///   · 新增RegisterDrain聚合消耗机制（v1.4）
    ///   · 接管原Need_Trion的恢复驱动和耗尽Hediff管理（v1.5）
    ///   · 通过CompGetGizmosExtra提供Gizmo_TrionBar（v1.5）
    ///
    /// 不变量（任何操作后必须成立）：
    ///   ① 0 ≤ cur ≤ max
    ///   ② 0 ≤ allocated ≤ cur
    ///   ③ max ≥ 0
    /// </summary>
    public class CompTrion : ThingComp
    {
        // ── 每天的tick数（RimWorld常量） ──
        private const float TICKS_PER_DAY = 60000f;

        // ── 核心数据（3个字段） ──
        private float cur;       // 当前Trion总值
        private float max;       // 当前最大容量
        private float allocated; // 被占用量（芯片注册锁定）

        // ── 状态标志 ──
        private bool frozen;     // 恢复是否被冻结（由外部系统设置）
        private bool initialized; // 是否已完成首次初始化（防止PawnFlyer落地时PostSpawnSetup重置数据）

        // ── 聚合消耗注册表（v1.4） ──
        // key=消耗源标识, value=每天消耗量
        private Dictionary<string, float> drainRegistry;

        // ── 内部控制 ──
        private int refreshTick;       // 下次从Stat刷新max的tick
        private int drainSettleTick;   // 下次聚合消耗结算的tick
        private int recoveryTick;      // 下次Pawn恢复+耗尽检查的tick

        // ── GUI缓存 ──
        private Gizmo_TrionBar gizmo;

        // ── 便利属性 ──
        public CompProperties_Trion Props => (CompProperties_Trion)props;
        public float Cur => cur;
        public float Max => max;
        public float Allocated => allocated;
        public bool Frozen => frozen;

        /// <summary>可用量 = 当前值 - 占用量。战斗中所有消耗从此扣减。</summary>
        public float Available => cur - allocated;

        /// <summary>
        /// 可用值耗尽事件（Available从>0降至≤0时触发）。
        /// 不序列化——由消费者在Notify_Equipped时注册，Notify_Unequipped时注销。
        /// </summary>
        public System.Action OnAvailableDepleted;

        /// <summary>当前百分比。供Gizmo_TrionBar显示和阈值检查使用。</summary>
        public float Percent => max > 0f ? cur / max : 0f;

        /// <summary>聚合消耗注册表中所有消耗源的每天总消耗量。</summary>
        public float TotalDrainPerDay
        {
            get
            {
                if (drainRegistry == null || drainRegistry.Count == 0) return 0f;
                float total = 0f;
                foreach (var kv in drainRegistry)
                    total += kv.Value;
                return total;
            }
        }

        // ═══════════════════════════════════════════
        //  RegisterDrain API（v1.4）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 注册一个持续消耗源。多个消耗源按key聚合，在drainSettleInterval周期统一结算。
        /// 重复注册同一key会覆盖旧值。
        /// </summary>
        /// <param name="key">消耗源唯一标识（如"passive"、芯片defName等）</param>
        /// <param name="drainPerDay">每天消耗量（正数）</param>
        public void RegisterDrain(string key, float drainPerDay)
        {
            if (string.IsNullOrEmpty(key) || drainPerDay <= 0f) return;
            drainRegistry[key] = drainPerDay;
        }

        /// <summary>注销一个持续消耗源。</summary>
        public void UnregisterDrain(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            drainRegistry.Remove(key);
        }

        // ═══════════════════════════════════════════
        //  核心API
        // ═══════════════════════════════════════════

        /// <summary>
        /// 从可用量中消耗Trion。
        /// 调用者：触发器模块（芯片使用）、战斗模块（维持/流失消耗）、设施模块（建筑运作）。
        /// </summary>
        /// <returns>true=消耗成功，false=参数无效或不足</returns>
        public bool Consume(float amount)
        {
            if (amount <= 0f) return false;
            if (Available < amount) return false;
            cur -= amount;
            CheckAvailableDepleted();
            return true;
        }

        /// <summary>
        /// 恢复Trion。日常状态下由CompTick自驱动。
        /// frozen时静默忽略。
        /// </summary>
        public void Recover(float amount)
        {
            if (amount <= 0f) return;
            if (frozen) return;
            cur = Mathf.Min(cur + amount, max);
        }

        /// <summary>
        /// 锁定占用量（芯片注册时调用）。
        /// 调用者：战斗模块（HComp_TrionAllocate）。
        /// </summary>
        public bool Allocate(float amount)
        {
            if (amount <= 0f) return false;
            if (Available < amount) return false;
            allocated += amount;
            return true;
        }

        /// <summary>
        /// 释放占用量（主动解除时调用）。
        /// 调用者：战斗模块（主动解除路径A）。
        /// </summary>
        public void Release(float amount)
        {
            // 防御性钳位：不会释放超过已占用的量
            amount = Mathf.Min(amount, allocated);
            allocated -= amount;
        }

        /// <summary>
        /// 强制耗尽所有Trion（被动破裂时调用）。
        /// 占用量随战斗体一同流失，不返还。
        /// </summary>
        public void ForceDeplete()
        {
            cur = 0f;
            allocated = 0f;
        }

        /// <summary>设置恢复冻结状态。由战斗模块在进入/退出战斗体状态时调用。</summary>
        public void SetFrozen(bool value)
        {
            frozen = value;
        }

        /// <summary>
        /// 检查Available是否已耗尽（≤0），若是则触发OnAvailableDepleted事件。
        /// 在Consume()、CompTick()聚合消耗、RefreshMax()后调用。
        /// </summary>
        private void CheckAvailableDepleted()
        {
            if (Available <= 0f)
                OnAvailableDepleted?.Invoke();
        }

        /// <summary>
        /// 从Stat系统重新读取max值。
        /// Pawn从Stat聚合读取，非Pawn用XML配置的baseMax。
        /// max缩小时cur和allocated跟着钳位，保持不变量。
        /// </summary>
        public void RefreshMax()
        {
            float newMax;
            if (parent is Pawn pawn)
            {
                newMax = pawn.GetStatValue(BDP_DefOf.BDP_TrionCapacity);
            }
            else
            {
                newMax = Props.baseMax;
            }

            max = Mathf.Max(0f, newMax);
            cur = Mathf.Clamp(cur, 0f, max);           // max缩小时cur跟着缩
            allocated = Mathf.Min(allocated, cur);       // 保持不变量②
            CheckAvailableDepleted();
        }

        // ═══════════════════════════════════════════
        //  生命周期方法
        // ═══════════════════════════════════════════

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // 确保drainRegistry已初始化（读档路径可能为null）
            if (drainRegistry == null)
                drainRegistry = new Dictionary<string, float>();

            if (!respawningAfterLoad && !initialized)
            {
                // 首次生成（非读档）：用XML配置初始化
                max = Props.baseMax;
                cur = max * Props.startPercent;
                allocated = 0f;
                frozen = false;
            }

            // 注册被动消耗（如果XML配置了passiveDrainPerDay）
            if (Props.passiveDrainPerDay > 0f)
            {
                RegisterDrain("passive", Props.passiveDrainPerDay);
            }

            // 从Stat同步max（Pawn的max来自Gene statOffsets，非baseMax）
            RefreshMax();

            // Pawn首次生成时：RefreshMax()可能将max从0提升到Stat值，
            // 但cur仍为0（因为baseMax=0）。此时用新max重新初始化cur。
            if (!respawningAfterLoad && !initialized && cur <= 0f && max > 0f)
                cur = max * Props.startPercent;

            initialized = true; // 标记已完成首次初始化，后续PostSpawnSetup不再重置

            int now = Find.TickManager.TicksGame;
            refreshTick = now + Props.statRefreshInterval;
            drainSettleTick = now + Props.drainSettleInterval;
            recoveryTick = now + Props.recoveryInterval;
        }

        public override void CompTick()
        {
            base.CompTick();
            int now = Find.TickManager.TicksGame;

            // ── 定期从Stat系统刷新max ──
            if (now >= refreshTick)
            {
                RefreshMax();
                refreshTick = now + Props.statRefreshInterval;
            }

            // ── 聚合消耗结算（每drainSettleInterval ticks） ──
            if (now >= drainSettleTick)
            {
                drainSettleTick = now + Props.drainSettleInterval;

                // 聚合消耗
                float totalDrain = TotalDrainPerDay;
                if (totalDrain > 0f)
                {
                    float drainAmount = totalDrain * (Props.drainSettleInterval / TICKS_PER_DAY);
                    Consume(drainAmount);
                }

                // 非Pawn载体的自动恢复（与消耗同周期结算）
                if (Props.recoveryPerDay > 0f && !(parent is Pawn))
                {
                    float recoverAmount = Props.recoveryPerDay * (Props.drainSettleInterval / TICKS_PER_DAY);
                    Recover(recoverAmount);
                }
            }

            // ── Pawn恢复 + 耗尽Hediff管理（每recoveryInterval ticks） ──
            if (parent is Pawn pawn && now >= recoveryTick)
            {
                recoveryTick = now + Props.recoveryInterval;
                TickPawnRecovery(pawn);
            }
        }

        /// <summary>
        /// Pawn专用：恢复驱动。
        /// 原Need_Trion.NeedInterval()的职责，迁移到CompTrion自驱动。
        /// 注意（v1.6）：不再管理Hediff_TrionDepletion——枯竭是战斗体破裂的结果，
        /// 由战斗体模块在"战斗体破裂"事件中负责添加/移除/调整Severity。
        /// </summary>
        private void TickPawnRecovery(Pawn pawn)
        {
            // 恢复逻辑（非冻结时）
            if (!frozen)
            {
                float recoveryRate = pawn.GetStatValue(BDP_DefOf.BDP_TrionRecoveryRate);
                float amount = recoveryRate * (Props.recoveryInterval / TICKS_PER_DAY);
                Recover(amount);
            }
        }

        // ═══════════════════════════════════════════
        //  Gizmo（v1.5 资源条 + v1.7 上帝模式调试）
        // ═══════════════════════════════════════════

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            // max为0说明没有Trion容量，不显示Gizmo
            if (max > 0f && Props.showGizmo)
            {
                if (gizmo == null)
                    gizmo = new Gizmo_TrionBar(this);
                yield return gizmo;
            }

            // ── 上帝模式调试按钮（仅 DebugSettings.godMode 下显示） ──
            if (!DebugSettings.godMode) yield break;

            // 仅对拥有Trion腺体基因的Pawn显示
            if (!(parent is Pawn godPawn &&
                  (godPawn.genes?.HasActiveGene(BDP_DefOf.BDP_Gene_TrionGland) ?? false)))
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "[God] Trion→0",
                defaultDesc = "强制清零Trion（测试枯竭/破裂路径）",
                action = () => { cur = 0f; allocated = 0f; }
            };
            yield return new Command_Action
            {
                defaultLabel = "[God] Trion→50%",
                defaultDesc = "设置Trion为50%（测试中间状态）",
                action = () => { cur = Mathf.Min(max * 0.5f, max); }
            };
            yield return new Command_Action
            {
                defaultLabel = "[God] Trion→满",
                defaultDesc = "填满Trion",
                action = () => { cur = max; }
            };
            yield return new Command_Action
            {
                defaultLabel = "[God] Trion+10",
                defaultDesc = "增加10点Trion（不受frozen限制）",
                action = () => { cur = Mathf.Min(cur + 10f, max); }
            };
            yield return new Command_Action
            {
                defaultLabel = "[God] Trion-10",
                defaultDesc = "减少10点Trion",
                action = () => { cur = Mathf.Max(cur - 10f, 0f); allocated = Mathf.Min(allocated, cur); }
            };
            yield return new Command_Action
            {
                defaultLabel = "[God] 切换冻结",
                defaultDesc = $"切换恢复冻结状态（当前: {(frozen ? "冻结" : "正常")}）",
                action = () => SetFrozen(!frozen)
            };
            yield return new Command_Action
            {
                defaultLabel = "[God] 强制耗尽",
                defaultDesc = "ForceDeplete()——测试被动破裂路径B",
                action = ForceDeplete
            };
            yield return new Command_Action
            {
                defaultLabel = "[God] 刷新Max",
                defaultDesc = "强制从Stat系统重算max",
                action = RefreshMax
            };
            yield return new Command_Action
            {
                defaultLabel = "[God] 输出注册表",
                defaultDesc = "将drainRegistry内容输出到日志",
                action = () =>
                {
                    string label = parent?.LabelShortCap ?? "unknown";
                    if (drainRegistry == null || drainRegistry.Count == 0)
                    {
                        Log.Message($"CompTrion [{label}] drainRegistry: (empty)");
                        return;
                    }
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"CompTrion [{label}] drainRegistry:");
                    foreach (var kv in drainRegistry)
                        sb.AppendLine($"  {kv.Key}: {kv.Value:F2}/day");
                    Log.Message(sb.ToString());
                }
            };
        }

        // ═══════════════════════════════════════════
        //  存档
        // ═══════════════════════════════════════════

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref cur, "trionCur");
            Scribe_Values.Look(ref max, "trionMax");
            Scribe_Values.Look(ref allocated, "trionAllocated");
            Scribe_Values.Look(ref frozen, "trionFrozen");
            Scribe_Values.Look(ref initialized, "trionInitialized");
            Scribe_Collections.Look(ref drainRegistry, "trionDrains", LookMode.Value, LookMode.Value);

            // 读档后校验不变量——防止存档损坏或版本升级导致的数据不一致
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                max = Mathf.Max(0f, max);
                cur = Mathf.Clamp(cur, 0f, max);
                allocated = Mathf.Clamp(allocated, 0f, cur);

                // 读档后视为已初始化，防止后续PostSpawnSetup重置
                initialized = true;

                // 旧存档兼容：drainRegistry可能为null
                if (drainRegistry == null)
                    drainRegistry = new Dictionary<string, float>();
            }
        }
    }
}
