using System;
using System.Collections.Generic;
using System.Linq;
using ProjectTrion.Core;
using Verse;
using RimWorld;
using UnityEngine;

namespace ProjectTrion.Components
{
    /// <summary>
    /// Trion能量管理系统的核心组件。
    /// 负责战斗体生命周期、能量消耗、组件管理等。
    ///
    /// Core component for Trion energy management system.
    /// Handles combat body lifecycle, energy consumption, component management, etc.
    /// </summary>
    public class CompTrion : ThingComp
    {
        /// <summary>
        /// 应用层提供的天赋→容量查表函数。
        /// 必须在游戏加载时设置。
        ///
        /// Application-provided lookup function: TalentGrade → Capacity.
        /// Must be set on mod load before units are generated.
        /// </summary>
        public static Func<TalentGrade, float> TalentCapacityProvider;

        private CompProperties_Trion _props;
        private ILifecycleStrategy _strategy;

        // === Trion能量系统 ===
        private float _capacity = 1000f;
        private float _reserved = 0f;        // 组件占用
        private float _consumed = 0f;        // 已消耗（不可逆）

        // === 战斗体状态 ===
        private bool _isInCombat = false;
        private CombatBodySnapshot _snapshot;

        // === 组件系统 ===
        private List<TriggerMount> _mounts = new List<TriggerMount>();

        // === 泄漏缓存 ===
        private float _cachedLeakRate = 0f;
        private int _leakCacheTickExpire = 0;

        /// <summary>
        /// 属性：Trion总容量
        /// Property: Total Trion capacity
        /// </summary>
        public float Capacity
        {
            get { return _capacity; }
            set
            {
                _capacity = Mathf.Max(0, value);
                ValidateDataConsistency();
            }
        }

        /// <summary>
        /// 属性：Trion占用量（由组件锁定）
        /// Property: Reserved Trion (locked by components)
        /// </summary>
        public float Reserved
        {
            get { return _reserved; }
            private set
            {
                _reserved = Mathf.Clamp(value, 0, _capacity);
                ValidateDataConsistency();
            }
        }

        /// <summary>
        /// 属性：Trion已消耗量（不可逆，仅能通过恢复补充）
        /// Property: Consumed Trion (irreversible, can only be recovered)
        /// </summary>
        public float Consumed
        {
            get { return _consumed; }
            private set
            {
                _consumed = Mathf.Clamp(value, 0, _capacity);
                ValidateDataConsistency();
            }
        }

        /// <summary>
        /// 属性：当前可用Trion（派生值，只读）
        /// Property: Available Trion (derived, read-only)
        /// 公式：Available = Capacity - Reserved - Consumed
        /// Formula: Available = Capacity - Reserved - Consumed
        /// </summary>
        public float Available => Mathf.Max(0, _capacity - _reserved - _consumed);

        /// <summary>
        /// 属性：是否在战斗体激活状态
        /// Property: Whether combat body is active
        /// </summary>
        public bool IsInCombat => _isInCombat;

        /// <summary>
        /// 属性：当前快照数据
        /// Property: Current snapshot data
        /// </summary>
        public CombatBodySnapshot Snapshot => _snapshot;

        /// <summary>
        /// 属性：当前Strategy实例
        /// Property: Current lifecycle strategy
        /// </summary>
        public ILifecycleStrategy Strategy => _strategy;

        /// <summary>
        /// 属性：所有挂载的组件列表
        /// Property: List of mounted components
        /// </summary>
        public List<TriggerMount> Mounts => _mounts;

        public override void PostExposeData()
        {
            base.PostExposeData();

            // 能量数据
            Scribe_Values.Look(ref _capacity, "capacity", 1000f);
            Scribe_Values.Look(ref _reserved, "reserved", 0f);
            Scribe_Values.Look(ref _consumed, "consumed", 0f);

            // 战斗体状态
            Scribe_Values.Look(ref _isInCombat, "isInCombat", false);

            // 快照数据
            Scribe_Deep.Look(ref _snapshot, "snapshot");

            // 组件列表
            Scribe_Collections.Look(ref _mounts, "mounts", LookMode.Deep);

            // 验证数据一致性
            ValidateDataConsistency();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            _props = this.props as CompProperties_Trion;
            if (_props == null)
            {
                Log.Error($"CompTrion: props不是CompProperties_Trion类型");
                return;
            }

            // ===== 读档分支 =====
            if (respawningAfterLoad)
            {
                // 从存档恢复时，直接恢复数据，无需重新计算
                // Capacity、Reserved、Consumed等将通过PostExposeData自动恢复

                // 重新注入Strategy（Strategy对象无法序列化，需重新创建）
                InitializeStrategy();
                if (_strategy == null)
                {
                    Log.Error($"CompTrion: 读档时无法为 {parent?.Label} 提供Strategy");
                    return;
                }

                return; // 读档完成，跳出
            }

            // ===== 新建分支 =====

            // 第1步：创建Strategy
            InitializeStrategy();
            if (_strategy == null)
            {
                Log.Error($"CompTrion: 无法为 {parent?.Label} 创建Strategy");
                return;
            }

            // 第2步：调用Strategy获取初始天赋
            TalentGrade? talent = _strategy.GetInitialTalent(this);

            // 第3步：根据天赋计算Capacity
            if (talent.HasValue)
            {
                // 框架根据天赋计算Capacity
                RecalculateCapacity(talent.Value);
            }
            else
            {
                // 天赋为null时，框架不干预
                // 应用层应该在其他地方设置Capacity
                Log.Message($"CompTrion: {parent?.Label} 的Strategy返回null天赋，由应用层自己设置Capacity");
                // 使用配置中的默认值
                _capacity = _props.capacity;
            }

            // 第4步：初始化能量四要素
            _reserved = 0f;
            _consumed = 0f;
            // Available由属性自动计算

            // 第5步：初始化Mounts列表
            if (_mounts == null)
            {
                _mounts = new List<TriggerMount>();
            }

            // 第6步：初始化快照
            if (_snapshot == null)
            {
                _snapshot = new CombatBodySnapshot();
            }
        }

        /// <summary>
        /// 反射加载并初始化Strategy。
        /// Reflect and initialize the lifecycle strategy.
        /// </summary>
        private void InitializeStrategy()
        {
            if (_props == null || _props.strategyClassName.NullOrEmpty())
            {
                Log.Error($"CompTrion: {parent?.Label}没有指定strategyClassName");
                return;
            }

            try
            {
                var strategyType = GenTypes.GetTypeInAnyAssembly(_props.strategyClassName);
                if (strategyType == null)
                {
                    Log.Error($"CompTrion: 找不到Strategy类{_props.strategyClassName}");
                    return;
                }

                // 创建Strategy实例，要求有接受CompTrion参数的构造函数
                _strategy = (ILifecycleStrategy)Activator.CreateInstance(strategyType, this);
                if (_strategy == null)
                {
                    Log.Error($"CompTrion: 无法创建Strategy实例{_props.strategyClassName}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CompTrion: 初始化Strategy失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 根据天赋等级重新计算并设置Capacity。
        /// 此方法由PostSpawnSetup在新建时调用，通过Strategy.GetInitialTalent的返回值触发。
        ///
        /// Recalculate Capacity based on talent grade.
        /// Called by PostSpawnSetup after Strategy.GetInitialTalent() returns a valid talent.
        /// </summary>
        private void RecalculateCapacity(TalentGrade talent)
        {
            // 使用TalentCapacityProvider委托查表
            if (TalentCapacityProvider != null)
            {
                _capacity = TalentCapacityProvider(talent);
            }
            else
            {
                Log.Error("CompTrion: TalentCapacityProvider未设置，无法计算Capacity");
                _capacity = _props.capacity;  // 使用默认配置值
            }

            // 验证Capacity有效性
            if (_capacity <= 0)
            {
                Log.Error($"CompTrion: 天赋{talent}计算出无效Capacity={_capacity}");
                _capacity = Mathf.Max(100f, _props.capacity);  // 使用最小值或配置值
            }

            ValidateDataConsistency();
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!_isInCombat || _strategy == null)
                return;

            // 每60Tick执行一次消耗计算
            if (this.parent.IsHashIntervalTick(60))
            {
                TickConsumption();
            }
        }

        /// <summary>
        /// 执行每60Tick一次的消耗计算。
        /// Perform consumption calculation once per 60 ticks.
        /// </summary>
        private void TickConsumption()
        {
            // 步骤1：基础维持消耗
            float baseMaintenance = _strategy.GetBaseMaintenance();

            // 步骤2：组件激活消耗
            float mountConsumption = 0f;
            foreach (var mount in _mounts.Where(m => m.IsActive))
            {
                mountConsumption += mount.GetConsumptionRate();
            }

            // 步骤3：泄漏消耗（缓存以提高性能）
            float leak = GetLeakRate();

            // 步骤4：累加并消耗
            float totalConsumption = baseMaintenance + mountConsumption + leak;
            Consume(totalConsumption);

            // 步骤5：检查耗尽
            if (Available <= 0)
            {
                TriggerBailOut();
            }

            // 步骤6：委托给Strategy处理复杂逻辑
            _strategy.OnTick(this);
        }

        /// <summary>
        /// 获取当前泄漏速率（带缓存）。
        /// 泄漏主要来自伤口。
        /// Get current leak rate with caching.
        /// Leak mainly from injuries.
        /// </summary>
        private float GetLeakRate()
        {
            // 检查缓存是否过期
            if (Find.TickManager.TicksGame >= _leakCacheTickExpire)
            {
                _cachedLeakRate = CalculateLeakRate();
                _leakCacheTickExpire = Find.TickManager.TicksGame + 60;
            }

            return _cachedLeakRate;
        }

        /// <summary>
        /// 计算当前泄漏速率。
        /// 遍历所有伤口（Hediff），根据类型和严重程度计算泄漏。
        /// Calculate leak rate based on injuries.
        /// </summary>
        private float CalculateLeakRate()
        {
            float leak = 0f;

            var pawn = this.parent as Pawn;
            if (pawn == null || pawn.health == null)
                return leak;

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                // 只计算伤口类型的泄漏
                var injury = hediff as Hediff_Injury;
                if (injury != null)
                {
                    // 泄漏速率基于伤口严重程度
                    leak += GetLeakRateForInjury(injury);
                }
            }

            return leak;
        }

        /// <summary>
        /// 根据伤口类型和位置获取泄漏速率。
        /// Get leak rate for a specific injury.
        /// 这是框架的通用计算，应用层可以通过Strategy覆盖。
        /// </summary>
        private float GetLeakRateForInjury(Hediff_Injury injury)
        {
            if (injury == null)
                return 0f;

            // 基础泄漏 + 伤口严重程度
            float leak = 0.5f + injury.Severity * 0.1f;

            // 断肢或其他关键部位受伤增加泄漏
            if (injury.Part != null && IsImportantBodyPart(injury.Part))
            {
                leak *= 2f;
            }

            return leak;
        }

        /// <summary>
        /// 检查是否是重要部位。
        /// Check if a body part is important.
        /// </summary>
        private bool IsImportantBodyPart(BodyPartRecord part)
        {
            if (part == null)
                return false;

            return part.IsInGroup(BodyPartGroupDefOf.LeftHand) ||
                   part.IsInGroup(BodyPartGroupDefOf.RightHand) ||
                   part.IsInGroup(BodyPartGroupDefOf.Legs) ||
                   part.IsInGroup(BodyPartGroupDefOf.Torso);
        }

        /// <summary>
        /// 消耗Trion能量。
        /// Consume Trion energy.
        /// </summary>
        public void Consume(float amount)
        {
            if (amount < 0)
            {
                Log.Warning($"CompTrion: 尝试消耗负数量的Trion({amount})");
                return;
            }

            Consumed += amount;
        }

        /// <summary>
        /// 恢复Trion能量。
        /// Recover Trion energy.
        /// </summary>
        public void Recover(float amount)
        {
            if (amount < 0)
            {
                Log.Warning($"CompTrion: 尝试恢复负数量的Trion({amount})");
                return;
            }

            _consumed = Mathf.Max(0, _consumed - amount);
        }

        /// <summary>
        /// 生成战斗体。
        /// Generate combat body and create snapshot.
        /// </summary>
        public void GenerateCombatBody()
        {
            if (_isInCombat)
            {
                Log.Warning($"CompTrion: {parent?.Label}已在战斗体状态，不能重复生成");
                return;
            }

            var pawn = this.parent as Pawn;
            if (pawn == null)
                return;

            // 保存快照
            _snapshot = new CombatBodySnapshot();
            _snapshot.CaptureFromPawn(pawn);

            // 标记为战斗体激活
            _isInCombat = true;

            // 回调Strategy
            _strategy?.OnCombatBodyGenerated(this);

            Log.Message($"CompTrion: {pawn.Name}的战斗体已生成，可用Trion: {Available}");
        }

        /// <summary>
        /// 摧毁战斗体并恢复到快照状态。
        /// Destroy combat body and restore to snapshot state.
        /// </summary>
        public void DestroyCombatBody(DestroyReason reason)
        {
            if (!_isInCombat)
            {
                Log.Warning($"CompTrion: {parent?.Label}不在战斗体状态");
                return;
            }

            var pawn = this.parent as Pawn;
            if (pawn == null)
                return;

            // 恢复快照
            if (_snapshot != null && _props.enableSnapshot)
            {
                _snapshot.RestoreToPawn(pawn);
            }

            // 根据摧毁原因计算后果
            if (reason == DestroyReason.TrionDepleted || reason == DestroyReason.BailOutSuccess)
            {
                // 被动解除：占用量流失
                _consumed += _reserved;
                _reserved = 0;
            }
            else
            {
                // 主动解除：占用量返还
                _reserved = 0;
            }

            // 标记为战斗体解除
            _isInCombat = false;

            // 清除泄漏缓存
            _leakCacheTickExpire = 0;

            // 回调Strategy
            _strategy?.OnCombatBodyDestroyed(this, reason);

            Log.Message($"CompTrion: {pawn.Name}的战斗体已摧毁，原因: {reason}，已消耗: {_consumed}");
        }

        /// <summary>
        /// 触发Bail Out紧急脱离系统。
        /// Trigger Bail Out emergency teleport system.
        ///
        /// 触发条件（两个都要满足）：
        /// 1. Trion供给器官未被摧毁（由Strategy.CanBailOut检查）
        /// 2. Available <= 0 或供给器官被摧毁
        /// </summary>
        public void TriggerBailOut()
        {
            if (!_isInCombat || _strategy == null)
                return;

            if (!_props.enableBailOut)
            {
                Log.Warning($"CompTrion: {parent?.Label}的Bail Out系统未启用");
                DestroyCombatBody(DestroyReason.TrionDepleted);
                return;
            }

            // 检查是否可以执行Bail Out
            if (!_strategy.CanBailOut(this))
            {
                Log.Message($"CompTrion: {parent?.Label}无法执行Bail Out，战斗体直接破裂");
                DestroyCombatBody(DestroyReason.BailOutFailed);
                return;
            }

            // 获取传送目标
            var target = _strategy.GetBailOutTarget(this);
            if (target == IntVec3.Invalid)
            {
                Log.Warning($"CompTrion: {parent?.Label}的Bail Out目标无效，战斗体破裂");
                DestroyCombatBody(DestroyReason.BailOutFailed);
                return;
            }

            // 执行传送
            var pawn = this.parent as Pawn;
            if (pawn != null && pawn.Map != null)
            {
                pawn.Position = target;
                pawn.Notify_Teleported();
                Log.Message($"CompTrion: {pawn.Label}执行Bail Out传送到{target}");
            }

            // 摧毁战斗体（被动解除）
            DestroyCombatBody(DestroyReason.BailOutSuccess);
        }

        /// <summary>
        /// 检测关键部位被摧毁。
        /// 由Harmony补丁调用。
        /// Detect vital part destruction.
        /// Called by Harmony patches.
        /// </summary>
        public void NotifyVitalPartDestroyed(BodyPartRecord part)
        {
            if (!_isInCombat || _strategy == null)
                return;

            Log.Message($"CompTrion: {parent?.Label}的关键部位被摧毁：{part.Label}");

            _strategy.OnVitalPartDestroyed(this, part);

            // 触发Bail Out
            TriggerBailOut();
        }

        /// <summary>
        /// 检测伤口变化，使泄漏缓存失效。
        /// Invalidate leak cache when injuries change.
        /// </summary>
        public void InvalidateLeakCache()
        {
            _leakCacheTickExpire = 0;
        }

        /// <summary>
        /// 数据一致性检查。
        /// Validate data consistency.
        /// </summary>
        private void ValidateDataConsistency()
        {
            // 检查1：Capacity必须 > 0
            if (_capacity <= 0)
            {
                Log.Error($"CompTrion: {parent?.Label} Capacity不能≤0，当前值{_capacity}");
                _capacity = 1f;
            }

            // 检查2：Reserved不能超过Capacity
            if (_reserved > _capacity)
            {
                Log.Error($"CompTrion: {parent?.Label} Reserved({_reserved}) > Capacity({_capacity})");
                _reserved = _capacity;
            }

            // 检查3：Consumed不能为负
            if (_consumed < 0)
            {
                Log.Error($"CompTrion: {parent?.Label} Consumed不能为负，当前值{_consumed}");
                _consumed = 0;
            }

            // 检查4：Reserved + Consumed不能超过Capacity
            if (_reserved + _consumed > _capacity)
            {
                Log.Error($"CompTrion: {parent?.Label} Reserved({_reserved}) + Consumed({_consumed}) > Capacity({_capacity})");
                _consumed = Mathf.Max(0, _capacity - _reserved);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (_strategy == null)
                return "CompTrion: 未初始化";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Trion容量: {_capacity}");
            sb.Append($"占用: {_reserved}, 消耗: {_consumed}, 可用: {Available}");
            if (_isInCombat)
            {
                sb.AppendLine();
                sb.Append("战斗体状态：激活中");
            }

            return sb.ToString();
        }
    }
}
