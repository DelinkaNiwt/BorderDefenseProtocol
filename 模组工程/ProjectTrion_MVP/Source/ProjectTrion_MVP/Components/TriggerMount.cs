using System.Collections.Generic;
using Verse;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// 触发器挂载点
    ///
    /// 职责：
    /// - 管理装备在触发器上的组件（最多N个）
    /// - 计算该挂载点的总占用值（Reserved）
    /// - 计算该挂载点的总消耗（TrionCost）
    /// - 提供组件装卸接口
    ///
    /// 数据关系：
    /// TrionTrigger
    ///   └─ Mounts[0]: TriggerMount (LeftHand, maxSlots=4)
    ///      └─ EquippedComponents: TriggerComponent[]
    ///   └─ Mounts[1]: TriggerMount (RightHand, maxSlots=4)
    ///   └─ Mounts[2]: TriggerMount (Special, maxSlots=1)
    /// </summary>
    public class TriggerMount : IExposable
    {
        // ============ 数据 ============
        private TrionTrigger _parent;                          // 父触发器
        private string _slotName;                               // 挂载点名称（LeftHand/RightHand/Special）
        private int _maxSlots;                                  // 最大容量
        private List<TriggerComponent> _equippedComponents;     // 装备的组件列表

        // ============ 属性 ============
        public TrionTrigger Parent => _parent;
        public string SlotName => _slotName;
        public int MaxSlots => _maxSlots;
        public List<TriggerComponent> EquippedComponents => _equippedComponents;
        public int ComponentCount => _equippedComponents?.Count ?? 0;
        public bool HasSlotSpace => ComponentCount < _maxSlots;

        // ============ 构造函数 ============
        public TriggerMount()
        {
            // 用于序列化
        }

        public TriggerMount(TrionTrigger parent, string slotName, int maxSlots)
        {
            _parent = parent;
            _slotName = slotName;
            _maxSlots = maxSlots;
            _equippedComponents = new List<TriggerComponent>();
        }

        // ============ 计算方法 ============

        /// <summary>
        /// 计算该挂载点的总占用值（Reserved）
        /// = 所有装备组件的 ReservedCost 之和
        /// </summary>
        public float CalculateTotalReserved()
        {
            float total = 0f;
            foreach (var comp in _equippedComponents)
            {
                total += comp.ReservedCost;
            }
            return total;
        }

        /// <summary>
        /// 计算该挂载点的总消耗（TrionCost）
        /// = 所有装备组件的 TrionCost 之和
        /// </summary>
        public float CalculateTotalTrionCost()
        {
            float total = 0f;
            foreach (var comp in _equippedComponents)
            {
                total += comp.TrionCost;
            }
            return total;
        }

        // ============ 组件管理 ============

        /// <summary>
        /// 尝试装备一个组件到此挂载点
        ///
        /// 前置检查：
        /// 1. 挂载点是否有空位
        /// 2. 组件定义是否有效
        /// 3. 战斗体是否激活（激活后不能修改配置）
        /// </summary>
        public bool TryAddComponent(TriggerComponent comp)
        {
            if (comp == null || comp.Def == null)
            {
                Log.Warning($"[Trion] 尝试装备空引用组件");
                return false;
            }

            // 检查1：是否有空位
            if (!HasSlotSpace)
            {
                Log.Warning($"[Trion] 挂载点 {_slotName} 已满（{ComponentCount}/{_maxSlots}）");
                return false;
            }

            // 检查2：战斗体是否激活
            var wearerComp = _parent?.GetWearerCompTrion();
            if (wearerComp?.IsInCombat ?? false)
            {
                Log.Warning("[Trion] 战斗体已激活，无法修改配置");
                return false;
            }

            // 装备成功
            _equippedComponents.Add(comp);
            Log.Message($"[Trion] {comp.Def.label} 已装备到 {_slotName} ({ComponentCount}/{_maxSlots})");
            return true;
        }

        /// <summary>
        /// 尝试卸下一个组件
        /// </summary>
        public bool TryRemoveComponent(TriggerComponent comp)
        {
            if (comp == null)
                return false;

            // 检查：战斗体是否激活
            var wearerComp = _parent?.GetWearerCompTrion();
            if (wearerComp?.IsInCombat ?? false)
            {
                Log.Warning("[Trion] 战斗体已激活，无法修改配置");
                return false;
            }

            if (_equippedComponents.Remove(comp))
            {
                Log.Message($"[Trion] {comp.Def.label} 已从 {_slotName} 卸下");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 清空此挂载点的所有组件
        /// </summary>
        public void ClearAllComponents()
        {
            _equippedComponents.Clear();
            Log.Message($"[Trion] 挂载点 {_slotName} 已清空");
        }

        // ============ 查询方法 ============

        /// <summary>
        /// 查找指定 defName 的组件
        /// </summary>
        public TriggerComponent FindComponentByDef(string defName)
        {
            foreach (var comp in _equippedComponents)
            {
                if (comp.Def.defName == defName)
                    return comp;
            }
            return null;
        }

        /// <summary>
        /// 检查是否装备了指定的组件
        /// </summary>
        public bool HasComponent(string defName)
        {
            return FindComponentByDef(defName) != null;
        }

        // ============ 序列化 ============

        public void ExposeData()
        {
            Scribe_References.Look(ref _parent, "parent");
            Scribe_Values.Look(ref _slotName, "slotName");
            Scribe_Values.Look(ref _maxSlots, "maxSlots");
            Scribe_Collections.Look(ref _equippedComponents, "equippedComponents", LookMode.Deep);

            // 防止null引用
            if (_equippedComponents == null)
                _equippedComponents = new List<TriggerComponent>();
        }
    }
}
