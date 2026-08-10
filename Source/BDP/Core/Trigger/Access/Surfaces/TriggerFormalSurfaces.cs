using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 正式读取表面。
    /// 它把 owner 的正式读取能力收成对外只读面。
    /// </summary>
    internal sealed class TriggerLoadoutReaderSurface : ITriggerLoadoutReader
    {
        /// <summary>
        /// 当前表面所代理的 Trigger owner。
        /// </summary>
        private readonly CompTriggerBody owner;

        /// <summary>
        /// 用一个 Trigger owner 构造正式读取表面。
        /// </summary>
        public TriggerLoadoutReaderSurface(CompTriggerBody owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// 读取当前触发体的芯片配置控制模式。
        /// </summary>
        public TriggerLoadoutControlMode LoadoutControlMode
        {
            get { return owner.LoadoutControlMode; }
        }

        /// <summary>
        /// 读取全部槽位。
        /// </summary>
        public IEnumerable<ITriggerSlotState> GetAllSlots()
        {
            return owner.GetAllSlots();
        }

        /// <summary>
        /// 按侧读取槽位。
        /// </summary>
        public IEnumerable<ITriggerSlotState> GetSlots(TriggerSide side)
        {
            return owner.GetSlots(side);
        }

        /// <summary>
        /// 读取当前正式激活槽位集合。
        /// </summary>
        public IEnumerable<ITriggerSlotState> GetActiveSlots()
        {
            return owner.GetActiveSlots();
        }

        /// <summary>
        /// 读取某一侧当前正式激活槽位。
        /// </summary>
        public ITriggerSlotState GetActiveSlot(TriggerSide side)
        {
            return owner.GetActiveSlot(side);
        }

        /// <summary>
        /// 读取某一侧当前正在切换到的目标槽位。
        /// </summary>
        public ITriggerSlotState GetActivatingSlot(TriggerSide side)
        {
            return owner.GetActivatingSlot(side);
        }

        /// <summary>
        /// 读取某一侧当前切换状态快照。
        /// </summary>
        public ITriggerSwitchState GetSwitchState(TriggerSide side)
        {
            return owner.GetSwitchState(side);
        }

        /// <summary>
        /// 读取某枚芯片当前正式形态键。
        /// </summary>
        public string GetChipModeKey(Thing chip)
        {
            return owner.GetChipModeKey(chip);
        }

        /// <summary>
        /// 读取某枚正式启用多形态芯片的有序形态选项。
        /// </summary>
        public IReadOnlyList<ChipModeOptionSnapshot> GetChipModeOptions(Thing chip)
        {
            return owner.GetChipModeOptions(chip);
        }

    }

    /// <summary>
    /// Trigger 正式交互语义表面。
    /// 它只负责把 owner 内部规则解释成对外稳定可读的交互语义结果。
    /// </summary>
    internal sealed class TriggerInteractionSurface : ITriggerInteractionReader
    {
        /// <summary>
        /// 当前表面所代理的 Trigger owner。
        /// </summary>
        private readonly CompTriggerBody owner;

        /// <summary>
        /// 用一个 Trigger owner 构造正式交互语义表面。
        /// </summary>
        public TriggerInteractionSurface(CompTriggerBody owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// 读取全部槽位的交互语义结果。
        /// </summary>
        public IEnumerable<ITriggerSlotInteractionState> GetAllSlotInteractions()
        {
            return owner.GetAllSlotInteractions();
        }

        /// <summary>
        /// 按侧读取槽位交互语义结果。
        /// </summary>
        public IEnumerable<ITriggerSlotInteractionState> GetSlotInteractions(TriggerSide side)
        {
            return owner.GetSlotInteractions(side);
        }

        /// <summary>
        /// 读取某个指定槽位的交互语义结果。
        /// </summary>
        public ITriggerSlotInteractionState GetSlotInteraction(TriggerSide side, int slotIndex)
        {
            return owner.GetSlotInteraction(side, slotIndex);
        }

        /// <summary>
        /// 读取某一侧的整体交互语义结果。
        /// </summary>
        public ITriggerSideInteractionState GetSideInteraction(TriggerSide side)
        {
            return owner.GetSideInteraction(side);
        }
    }

    /// <summary>
    /// Trigger 正式请求表面。
    /// 它只承接会改动真值的正式输入。
    /// </summary>
    internal sealed class TriggerLoadoutCommandSurface : ITriggerLoadoutCommands
    {
        /// <summary>
        /// 当前表面所代理的 Trigger owner。
        /// </summary>
        private readonly CompTriggerBody owner;

        /// <summary>
        /// 用一个 Trigger owner 构造正式请求表面。
        /// </summary>
        public TriggerLoadoutCommandSurface(CompTriggerBody owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// 请求向指定槽位装入芯片。
        /// </summary>
        public bool TryLoadChip(TriggerSide side, int slotIndex, Thing chip)
        {
            return owner.TryLoadChip(side, slotIndex, chip);
        }

        /// <summary>
        /// 请求从指定槽位卸下芯片。
        /// </summary>
        public bool TryUnloadChip(TriggerSide side, int slotIndex)
        {
            return owner.TryUnloadChip(side, slotIndex);
        }

        /// <summary>
        /// 请求启用指定槽位。
        /// </summary>
        public bool RequestActivate(TriggerSide side, int slotIndex)
        {
            return owner.RequestActivate(side, slotIndex);
        }

        /// <summary>
        /// 请求停用某一侧当前激活槽位。
        /// </summary>
        public bool RequestDeactivate(TriggerSide side)
        {
            return owner.RequestDeactivate(side);
        }

        /// <summary>
        /// 请求销毁指定槽位中与目标 ThingID 匹配的已装载芯片。
        /// </summary>
        public bool TryDestroyLoadedChip(TriggerSide side, int slotIndex, string expectedThingId)
        {
            return owner.TryDestroyLoadedChip(side, slotIndex, expectedThingId);
        }

        /// <summary>
        /// 请求切换到指定芯片形态。
        /// </summary>
        public bool RequestSwitchChipMode(Thing chip, string targetModeKey)
        {
            return owner.RequestSwitchChipMode(chip, targetModeKey);
        }

        /// <summary>
        /// 请求切换到作者顺序中的下一芯片形态。
        /// </summary>
        public bool RequestCycleChipMode(Thing chip)
        {
            return owner.RequestCycleChipMode(chip);
        }
    }

    /// <summary>
    /// Trigger 正式事件表面。
    /// 它只负责把 owner 的状态广播对外转发出去。
    /// </summary>
    internal sealed class TriggerEventSurface : ITriggerEvents
    {
        /// <summary>
        /// 当前表面所代理的 Trigger owner。
        /// </summary>
        private readonly CompTriggerBody owner;

        /// <summary>
        /// 用一个 Trigger owner 构造正式事件表面。
        /// </summary>
        public TriggerEventSurface(CompTriggerBody owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// 转发槽位装配内容变化事件。
        /// </summary>
        public event Action<TriggerSlotStateChangedArgs> SlotLoadoutChanged
        {
            add { owner.SlotLoadoutChanged += value; }
            remove { owner.SlotLoadoutChanged -= value; }
        }

        /// <summary>
        /// 转发槽位正式启用完成事件。
        /// </summary>
        public event Action<TriggerSlotStateChangedArgs> SlotActivationCommitted
        {
            add { owner.SlotActivationCommitted += value; }
            remove { owner.SlotActivationCommitted -= value; }
        }

        /// <summary>
        /// 转发槽位正式停用完成事件。
        /// </summary>
        public event Action<TriggerSlotStateChangedArgs> SlotDeactivated
        {
            add { owner.SlotDeactivated += value; }
            remove { owner.SlotDeactivated -= value; }
        }

        /// <summary>
        /// 转发槽位禁用状态变化事件。
        /// </summary>
        public event Action<TriggerSlotStateChangedArgs> SlotDisableStateChanged
        {
            add { owner.SlotDisableStateChanged += value; }
            remove { owner.SlotDisableStateChanged -= value; }
        }
    }
}
