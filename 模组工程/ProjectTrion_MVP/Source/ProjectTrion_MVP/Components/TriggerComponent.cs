using Verse;
using RimWorld;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// Trion 组件数据类 - 轻量级
    ///
    /// 关键改动：
    /// - 改为普通数据类，不继承任何游戏类
    /// - 引用 ThingDef 来获取定义数据
    /// - 参数通过 ModExtension 存储
    ///
    /// 对比说明：
    /// - TrionComponent_Thing：在地图/背包中的物品形态
    /// - TriggerComponent：装备到触发器后的数据对象（只存在于运行时）
    ///
    /// 在 TriggerMount 中，EquippedComponents 列表存储的是 TriggerComponent 对象
    /// </summary>
    public class TriggerComponent : IExposable
    {
        // ============ 数据 ============
        private ThingDef _def;        // 引用 TriggerComponent_* 的定义
        private bool _isActive;       // 激活状态

        // ============ 属性 ============
        public ThingDef Def => _def;
        public bool IsActive => _isActive;

        /// <summary>
        /// 获取组件的 Trion 占用值（Reserved成本）
        /// </summary>
        public float ReservedCost
        {
            get
            {
                var ext = _def?.GetModExtension<TriggerComponentExt>();
                return ext?.reservedCost ?? 10f;
            }
        }

        /// <summary>
        /// 获取组件每 Tick 的消耗
        /// </summary>
        public float TrionCost
        {
            get
            {
                var ext = _def?.GetModExtension<TriggerComponentExt>();
                return ext?.trionCost ?? 1f;
            }
        }

        // ============ 构造函数 ============

        public TriggerComponent()
        {
            // 用于序列化
        }

        private TriggerComponent(ThingDef def)
        {
            _def = def;
            _isActive = false;
        }

        // ============ 创建方法 ============

        /// <summary>
        /// 从 defName 创建组件对象
        /// </summary>
        public static TriggerComponent CreateFromDef(string defName)
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName, false);
            if (def == null)
            {
                Log.Warning($"[Trion] 无法找到组件定义: {defName}");
                return null;
            }
            return new TriggerComponent(def);
        }

        /// <summary>
        /// 从 ThingDef 创建组件对象
        /// </summary>
        public static TriggerComponent CreateFromDef(ThingDef def)
        {
            if (def == null)
                return null;
            return new TriggerComponent(def);
        }

        // ============ 状态方法 ============

        /// <summary>
        /// 设置激活状态
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;
            Log.Message($"[Trion] {_def?.label ?? "未知组件"} 设置激活状态: {active}");
        }

        // ============ 序列化 ============

        public void ExposeData()
        {
            // 只需要序列化 defName，读档时重建
            string defName = _def?.defName;
            Scribe_Values.Look(ref defName, "defName");

            if (Scribe.mode == LoadSaveMode.LoadingVars && defName != null)
            {
                _def = DefDatabase<ThingDef>.GetNamed(defName, false);
            }

            Scribe_Values.Look(ref _isActive, "isActive");
        }
    }

    /// <summary>
    /// TriggerComponent 的参数扩展
    ///
    /// 在 XML 的 ModExtensions 中定义参数：
    /// <li Class="ProjectTrion_MVP.TriggerComponentExt">
    ///     <reservedCost>10</reservedCost>
    ///     <trionCost>1</trionCost>
    /// </li>
    /// </summary>
    public class TriggerComponentExt : DefModExtension
    {
        public float reservedCost = 10f;  // Trion 占用值
        public float trionCost = 1f;      // 每 Tick 消耗
    }
}
