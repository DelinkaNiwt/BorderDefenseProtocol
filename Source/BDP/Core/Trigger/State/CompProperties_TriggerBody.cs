using System.Collections.Generic;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体最小配置。
    /// 当前正式切换时序已下放到芯片 Def 层，这里主要保留槽位数量配置。
    /// </summary>
    public sealed class CompProperties_TriggerBody : CompProperties
    {
        /// <summary>
        /// 当前触发体所属的正式触发器类别。
        /// 所有正式触发体都必须显式填写，不根据名称推断默认类别。
        /// </summary>
        public TriggerCategoryDef triggerCategory;

        /// <summary>
        /// 当前触发体的芯片配置是否允许玩家修改。
        /// 默认保持现有触发体的可配置行为；玩家不可配置的触发体应显式填写 PlayerNonConfigurable。
        /// </summary>
        public TriggerLoadoutControlMode loadoutControlMode = TriggerLoadoutControlMode.PlayerConfigurable;

        /// <summary>
        /// 主侧槽位数量。
        /// </summary>
        public int mainSlotCount = 4;

        /// <summary>
        /// 副侧槽位数量。
        /// </summary>
        public int subSlotCount = 4;

        /// <summary>
        /// 特殊侧槽位数量。
        /// </summary>
        public int specialSlotCount = 0;

        /// <summary>
        /// 触发体首次生成时按定义预装的真实芯片列表；省略或为空表示不预装。
        /// </summary>
        public List<TriggerFixedLoadoutEntry> fixedLoadout;

        /// <summary>
        /// 构造触发体配置并绑定正式 Comp 类型。
        /// </summary>
        public CompProperties_TriggerBody()
        {
            compClass = typeof(CompTriggerBody);
        }

        /// <summary>
        /// 检查触发体是否填写正式类别。
        /// </summary>
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (triggerCategory == null)
            {
                yield return (parentDef != null ? parentDef.defName : "<unknown>")
                    + " requires triggerCategory.";
            }

            foreach (string error in TriggerFixedLoadoutValidator.ConfigErrors(this, parentDef))
            {
                yield return error;
            }
        }
    }
}
