using Verse;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// Trion组件的定义类
    /// 定义一个组件的基础属性（消耗、效果等）
    ///
    /// MVP中包含5个组件：
    /// - 弧月（近战武器）
    /// - 护盾（防御）
    /// - 炸裂弹（远程武器）
    /// - 变色龙（隐身/检测规避）
    /// - 脱离（紧急Bail Out）
    /// </summary>
    public class TrionComponentDef : Def
    {
        /// <summary>
        /// 组件的持续消耗值（每60Tick）
        /// 当组件被激活时，这个值会加入到Reserved中
        /// 范围建议：1-500
        /// </summary>
        public float trionCost = 10f;

        /// <summary>
        /// 组件在触发器中占用的槽位数
        /// MVP中所有组件都是 slotSize = 1
        /// </summary>
        public int slotSize = 1;

        /// <summary>
        /// 组件的类型（分类）
        /// 用于防止某些组件冲突装备（如两个近战武器）
        /// </summary>
        public TrionComponentType componentType = TrionComponentType.Utility;

        /// <summary>
        /// 是否是唯一组件（同一Pawn只能有一个）
        /// </summary>
        public bool isUnique = false;

        /// <summary>
        /// 组件的简短描述（游戏中显示）
        /// </summary>
        public string componentDesc = "";
    }

    /// <summary>
    /// Trion组件的分类
    /// 用于判断组件是否能共存
    /// </summary>
    public enum TrionComponentType
    {
        /// <summary>近战武器</summary>
        MeleeWeapon,

        /// <summary>远程武器</summary>
        RangedWeapon,

        /// <summary>防御/护盾</summary>
        Shield,

        /// <summary>辅助/实用</summary>
        Utility,

        /// <summary>特殊/唯一</summary>
        Special
    }
}
