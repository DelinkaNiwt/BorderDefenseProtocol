using Verse;

namespace ProjectTrion.Components
{
    /// <summary>
    /// Trion组件实例。
    /// 代表一个装备在触发器上的组件（如弧月、护盾、隐身等）。
    ///
    /// Instance of a Trion component mounted on a trigger.
    /// Represents equipped components like Arcus Moon, Shield, Stealth, etc.
    /// </summary>
    public class TriggerMount : IExposable
    {
        /// <summary>
        /// 所属的Def定义。
        /// The definition this mount is based on.
        /// </summary>
        public TriggerMountDef def;

        /// <summary>
        /// 激活时的剩余导引Tick。
        /// Remaining guidance ticks when activating.
        /// 部分组件激活时需要导引时间（1-5Tick）。
        /// </summary>
        public int activationTicks;

        /// <summary>
        /// 该组件是否处于激活状态。
        /// Whether this component is activated.
        /// </summary>
        private bool _isActive = false;

        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; }
        }

        /// <summary>
        /// 组件的自定义数据。
        /// 应用层可以在此存储组件特定的状态。
        /// Custom data for component-specific state.
        /// </summary>
        public IExposable customData;

        public TriggerMount()
        {
        }

        public TriggerMount(TriggerMountDef def)
        {
            this.def = def;
            this.IsActive = false;
            this.activationTicks = 0;
        }

        /// <summary>
        /// 获取组件的占用值（锁定的Trion容量）。
        /// Get the reserved Trion cost of this component.
        /// </summary>
        public float GetReservedCost()
        {
            return def?.reservedCost ?? 0f;
        }

        /// <summary>
        /// 获取组件的激活费用（一次性消耗）。
        /// Get the one-time activation cost of this component.
        /// </summary>
        public float GetActivationCost()
        {
            return def?.activationCost ?? 0f;
        }

        /// <summary>
        /// 获取组件的持续消耗速率（每60Tick）。
        /// Get the continuous consumption rate of this component.
        /// </summary>
        public float GetConsumptionRate()
        {
            if (!IsActive || def == null)
                return 0f;

            return def.consumptionRate;
        }

        /// <summary>
        /// 激活组件。
        /// Activate this component.
        /// </summary>
        public void Activate()
        {
            if (IsActive || def == null)
                return;

            IsActive = true;
            activationTicks = def.activationGuidanceTicks;
        }

        /// <summary>
        /// 停用组件。
        /// Deactivate this component.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            activationTicks = 0;
        }

        /// <summary>
        /// 每Tick更新组件状态（如导引倒计时）。
        /// Update component state each tick (e.g., activation countdown).
        /// </summary>
        public void Tick()
        {
            if (IsActive && activationTicks > 0)
            {
                activationTicks--;
            }
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref _isActive, "isActive");
            Scribe_Values.Look(ref activationTicks, "activationTicks");
            Scribe_Deep.Look(ref customData, "customData");
        }
    }
}
