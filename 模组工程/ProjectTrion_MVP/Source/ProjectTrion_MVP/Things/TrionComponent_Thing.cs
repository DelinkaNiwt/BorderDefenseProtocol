using Verse;
using RimWorld;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// Trion组件物品
    /// 玩家可以拾取、携带、装备这类物品
    ///
    /// MVP包含5种组件：
    /// - TrionComponent_ArcBlade（弧月）
    /// - TrionComponent_Shield（护盾）
    /// - TrionComponent_ExplosiveBullet（炸裂弹）
    /// - TrionComponent_Chameleon（变色龙）
    /// - TrionComponent_BailOut（脱离）
    /// </summary>
    public class TrionComponent_Thing : Thing
    {
        /// <summary>
        /// 此组件对应的Def
        /// 可从Def_Named<TrionComponentDef>或ThingDef获取
        /// </summary>
        public virtual TrionComponentDef ComponentDef
        {
            get
            {
                // 尝试从ThingDef中获取自定义属性（XML中定义）
                if (def is ThingDef thingDef)
                {
                    // 可扩展：从thingDef的modExtensions中读取TrionComponentDef
                    // 目前简化实现
                }

                return null;
            }
        }

        /// <summary>
        /// 获取组件的消耗值
        /// </summary>
        public virtual float GetTrionCost()
        {
            return ComponentDef?.trionCost ?? 10f;
        }

        /// <summary>
        /// 获取组件占用的槽位
        /// </summary>
        public virtual int GetSlotSize()
        {
            return ComponentDef?.slotSize ?? 1;
        }

        /// <summary>
        /// 获取组件的类型
        /// </summary>
        public virtual TrionComponentType GetComponentType()
        {
            return ComponentDef?.componentType ?? TrionComponentType.Utility;
        }

        /// <summary>
        /// 组件被装备时的回调
        /// 子类可以重写此方法实现特殊效果
        /// </summary>
        public virtual void OnEquipped(Pawn wearer)
        {
            Log.Message($"[Trion] {wearer.LabelShort} 装备了 {Label}");
        }

        /// <summary>
        /// 组件被激活时的回调（Trion能量供应给此组件时）
        /// </summary>
        public virtual void OnActivated(Pawn wearer)
        {
            Log.Message($"[Trion] {wearer.LabelShort} 的 {Label} 已激活");
        }

        /// <summary>
        /// 组件被停用时的回调
        /// </summary>
        public virtual void OnDeactivated(Pawn wearer)
        {
            Log.Message($"[Trion] {wearer.LabelShort} 的 {Label} 已停用");
        }

        /// <summary>
        /// 组件被移除时的回调
        /// </summary>
        public virtual void OnUnequipped(Pawn wearer)
        {
            Log.Message($"[Trion] {wearer.LabelShort} 卸下了 {Label}");
        }
    }

    /// <summary>
    /// 弧月 - 近战能量刃
    /// 消耗：10 Trion/60Tick
    /// 效果：提升近战伤害
    /// </summary>
    public class TrionComponent_ArcBlade : TrionComponent_Thing
    {
        public override void OnActivated(Pawn wearer)
        {
            base.OnActivated(wearer);
            Log.Message($"[Trion] {wearer.LabelShort} 的弧月激活，近战伤害提升");
        }
    }

    /// <summary>
    /// 护盾 - 能量护盾
    /// 消耗：15 Trion/60Tick
    /// 效果：吸收伤害
    /// </summary>
    public class TrionComponent_Shield : TrionComponent_Thing
    {
        public override void OnActivated(Pawn wearer)
        {
            base.OnActivated(wearer);
            Log.Message($"[Trion] {wearer.LabelShort} 的护盾激活，开始吸收伤害");
        }
    }

    /// <summary>
    /// 炸裂弹 - 能量弹药
    /// 消耗：10 Trion/60Tick
    /// 效果：远程伤害爆炸
    /// </summary>
    public class TrionComponent_ExplosiveBullet : TrionComponent_Thing
    {
        public override void OnActivated(Pawn wearer)
        {
            base.OnActivated(wearer);
            Log.Message($"[Trion] {wearer.LabelShort} 的炸裂弹激活，可发射能量爆炸");
        }
    }

    /// <summary>
    /// 变色龙 - 隐身组件
    /// 消耗：10 Trion/60Tick
    /// 效果：降低检测难度，增加规避
    /// </summary>
    public class TrionComponent_Chameleon : TrionComponent_Thing
    {
        public override void OnActivated(Pawn wearer)
        {
            base.OnActivated(wearer);
            Log.Message($"[Trion] {wearer.LabelShort} 的变色龙激活，进入隐身状态");
        }
    }

    /// <summary>
    /// 脱离 - 紧急Bail Out装置
    /// 消耗：400 Trion（一次性）
    /// 效果：当Trion耗尽或生命危急时自动脱离
    /// </summary>
    public class TrionComponent_BailOut : TrionComponent_Thing
    {
        public override void OnActivated(Pawn wearer)
        {
            base.OnActivated(wearer);
            Log.Message($"[Trion] {wearer.LabelShort} 的脱离装置激活，已准备好进行Bail Out");
        }
    }
}
