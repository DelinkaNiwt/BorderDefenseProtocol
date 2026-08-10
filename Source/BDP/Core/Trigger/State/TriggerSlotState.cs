using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 单个槽位的最小状态对象。
    ///
    /// 它只回答这个槽位自己的事实：
    /// - 属于哪一侧
    /// - 是第几格
    /// - 当前装了什么芯片
    /// - 当前是否正式激活
    /// - 当前是否被禁用
    ///
    /// 它不负责：
    /// - 决定哪一侧允许激活哪一格
    /// - 处理切换表现
    /// - 生成表达或按钮
    /// </summary>
    public sealed class TriggerSlotState : IExposable, ITriggerSlotState
    {
        /// <summary>
        /// 槽位所属侧别。
        /// </summary>
        private TriggerSide side;

        /// <summary>
        /// 槽位在该侧中的索引。
        /// </summary>
        private int index;

        /// <summary>
        /// 当前装入的芯片。
        /// </summary>
        private Thing loadedChip;

        /// <summary>
        /// 当前槽位正式真值记录的芯片稳定标识。
        /// 它只服务读档后把 slot 真值重新绑定回实际 Thing，不把 container 升级成业务真值。
        /// </summary>
        private string loadedChipThingId;

        /// <summary>
        /// 当前是否处于正式激活状态。
        /// </summary>
        private bool isActive;

        /// <summary>
        /// 当前正式启用根槽所采用的芯片形态键。
        /// 未启用、单形态或镜像槽位必须为空。
        /// </summary>
        private string currentModeKey;

        /// <summary>
        /// 当前是否被外部规则禁用。
        /// </summary>
        private bool isDisabled;

        /// <summary>
        /// 当前禁用原因码。
        /// </summary>
        private TriggerDisableReason disabledReason;

        /// <summary>
        /// 当前槽位是否参与了跨侧绑定。
        /// </summary>
        private bool hasBindingPartner;

        /// <summary>
        /// 当前槽位是否是绑定关系中的镜像副本。
        /// </summary>
        private bool isBindingMirror;

        /// <summary>
        /// 当前绑定关系的主槽位侧别。
        /// </summary>
        private TriggerSide bindingRootSide;

        /// <summary>
        /// 当前绑定关系的主槽位索引。
        /// </summary>
        private int bindingRootIndex = -1;

        /// <summary>
        /// 当前绑定关系的对侧槽位侧别。
        /// </summary>
        private TriggerSide bindingPartnerSide;

        /// <summary>
        /// 当前绑定关系的对侧槽位索引。
        /// </summary>
        private int bindingPartnerIndex = -1;

        /// <summary>
        /// 给存档反序列化保留的空构造。
        /// </summary>
        public TriggerSlotState()
        {
        }

        /// <summary>
        /// 用指定侧别和索引构造槽位状态。
        /// </summary>
        public TriggerSlotState(TriggerSide side, int index)
        {
            this.side = side;
            this.index = index;
            ClearBinding();
        }

        /// <summary>
        /// 槽位所属侧别。
        /// </summary>
        public TriggerSide Side
        {
            get { return side; }
        }

        /// <summary>
        /// 槽位在所属侧中的索引。
        /// </summary>
        public int Index
        {
            get { return index; }
        }

        /// <summary>
        /// 当前装入的芯片。
        /// </summary>
        public Thing LoadedChip
        {
            get { return loadedChip; }
        }

        /// <summary>
        /// 当前槽位正式真值记录的芯片稳定标识。
        /// 为空表示这个槽位正式上没有芯片。
        /// </summary>
        public string LoadedChipThingId
        {
            get { return loadedChipThingId; }
        }

        /// <summary>
        /// 当前是否正式激活。
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
        }

        /// <summary>
        /// 当前正式启用根槽所采用的芯片形态键。
        /// 只供 Core（核心程序集）内部维护，外部统一通过正式读取面取得。
        /// </summary>
        internal string CurrentModeKey
        {
            get { return currentModeKey; }
        }

        /// <summary>
        /// 当前是否被禁用。
        /// </summary>
        public bool IsDisabled
        {
            get { return isDisabled; }
        }

        /// <summary>
        /// 当前禁用原因码。
        /// </summary>
        public TriggerDisableReason DisabledReason
        {
            get { return disabledReason; }
        }

        /// <summary>
        /// 当前槽位是否参与了一组跨侧绑定。
        /// </summary>
        public bool HasBindingPartner
        {
            get { return hasBindingPartner; }
        }

        /// <summary>
        /// 当前槽位是否是镜像副本。
        /// </summary>
        public bool IsBindingMirror
        {
            get { return isBindingMirror; }
        }

        /// <summary>
        /// 当前绑定关系的主槽位侧别。
        /// </summary>
        public TriggerSide BindingRootSide
        {
            get { return bindingRootSide; }
        }

        /// <summary>
        /// 当前绑定关系的主槽位索引。
        /// </summary>
        public int BindingRootIndex
        {
            get { return bindingRootIndex; }
        }

        /// <summary>
        /// 当前绑定关系的对侧槽位侧别。
        /// </summary>
        public TriggerSide BindingPartnerSide
        {
            get { return bindingPartnerSide; }
        }

        /// <summary>
        /// 当前绑定关系的对侧槽位索引。
        /// </summary>
        public int BindingPartnerIndex
        {
            get { return bindingPartnerIndex; }
        }

        /// <summary>
        /// 设置当前槽位装入的芯片。
        /// 这里只改事实，不广播事件。
        /// </summary>
        public void SetLoadedChip(Thing chip)
        {
            if (loadedChip != chip)
            {
                currentModeKey = null;
            }

            loadedChip = chip;
            loadedChipThingId = chip != null ? chip.ThingID : null;
        }

        /// <summary>
        /// 尝试把槽位置为激活或关闭。
        /// 只有槽位里有芯片且未被禁用时，激活状态才允许成立。
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active && loadedChip != null && !isDisabled;
            if (!isActive)
            {
                currentModeKey = null;
            }
        }

        /// <summary>
        /// 设置禁用状态。
        /// 一旦被禁用，当前激活也必须同步失效。
        /// </summary>
        public void SetDisabled(bool disabled, TriggerDisableReason reason)
        {
            isDisabled = disabled;
            disabledReason = isDisabled ? reason : TriggerDisableReason.None;
            if (isDisabled)
            {
                isActive = false;
                currentModeKey = null;
            }
        }

        /// <summary>
        /// 写入当前根槽的形态键。
        /// 空白统一正规化为空，调用方负责先确认目标形态合法。
        /// </summary>
        internal void SetCurrentModeKey(string modeKey)
        {
            currentModeKey = string.IsNullOrWhiteSpace(modeKey) ? null : modeKey;
        }

        /// <summary>
        /// 设置当前槽位的绑定元数据。
        /// 这里只记录关系事实，不处理同步业务。
        /// </summary>
        public void SetBinding(
            bool mirror,
            TriggerSide rootSide,
            int rootIndex,
            TriggerSide partnerSide,
            int partnerIndex)
        {
            hasBindingPartner = true;
            isBindingMirror = mirror;
            if (isBindingMirror)
            {
                currentModeKey = null;
            }

            bindingRootSide = rootSide;
            bindingRootIndex = rootIndex;
            bindingPartnerSide = partnerSide;
            bindingPartnerIndex = partnerIndex;
        }

        /// <summary>
        /// 清空当前槽位的绑定元数据。
        /// 普通单槽位默认把自己视作根。
        /// </summary>
        public void ClearBinding()
        {
            hasBindingPartner = false;
            isBindingMirror = false;
            bindingRootSide = side;
            bindingRootIndex = index;
            bindingPartnerSide = side;
            bindingPartnerIndex = -1;
        }

        /// <summary>
        /// 存读档槽位最小状态事实。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref side, "side", TriggerSide.Main);
            Scribe_Values.Look(ref index, "index", 0);
            Scribe_References.Look(ref loadedChip, "loadedChip");
            Scribe_Values.Look(ref loadedChipThingId, "loadedChipThingId");
            Scribe_Values.Look(ref isActive, "isActive", false);
            Scribe_Values.Look(ref currentModeKey, "currentModeKey");
            Scribe_Values.Look(ref isDisabled, "isDisabled", false);
            Scribe_Values.Look(ref disabledReason, "disabledReason", TriggerDisableReason.None);
            Scribe_Values.Look(ref hasBindingPartner, "hasBindingPartner", false);
            Scribe_Values.Look(ref isBindingMirror, "isBindingMirror", false);
            Scribe_Values.Look(ref bindingRootSide, "bindingRootSide", TriggerSide.Main);
            Scribe_Values.Look(ref bindingRootIndex, "bindingRootIndex", -1);
            Scribe_Values.Look(ref bindingPartnerSide, "bindingPartnerSide", TriggerSide.Main);
            Scribe_Values.Look(ref bindingPartnerIndex, "bindingPartnerIndex", -1);

            // 读档后如果芯片没了，或该槽位本来就被禁用，
            // 就不能再继续保留“已激活”状态。
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!hasBindingPartner)
                {
                    bindingRootSide = side;
                    bindingRootIndex = index;
                    bindingPartnerSide = side;
                    bindingPartnerIndex = -1;
                }

                // 读档阶段如果直接引用尚未恢复，但 slot 已经声明了芯片标识，
                // 这里不能提前把激活态抹掉；后续由 Trigger 真值恢复步骤把标识重新绑定回实际 Thing。
                if ((loadedChip == null && string.IsNullOrWhiteSpace(loadedChipThingId)) || isDisabled)
                {
                    isActive = false;
                    if (!isDisabled)
                    {
                        disabledReason = TriggerDisableReason.None;
                    }
                }

                if (!isActive
                    || (loadedChip == null && string.IsNullOrWhiteSpace(loadedChipThingId))
                    || isBindingMirror)
                {
                    currentModeKey = null;
                }
            }
        }

        /// <summary>
        /// 按槽位自身记录的芯片标识恢复读档后的实际引用。
        /// 这里只绑定 slot 已经声明的目标，不从 container 反向猜测业务含义。
        /// </summary>
        public void RestoreLoadedChipReference(Thing chip)
        {
            loadedChip = chip;
            loadedChipThingId = chip != null ? chip.ThingID : loadedChipThingId;
            if (loadedChip == null && string.IsNullOrWhiteSpace(loadedChipThingId))
            {
                isActive = false;
                currentModeKey = null;
            }

            if (isBindingMirror)
            {
                currentModeKey = null;
            }
        }
    }
}
