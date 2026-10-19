using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Trion.Facility.Sacrifice
{
    /// <summary>原版运输容器的待装／已装清单；只读展示，完整批次通过取消准备重新选择。</summary>
    public sealed class ITab_TrionFacilitySacrifice : ITab_ContentsTransporter
    {
        /// <summary>设置供物文案并关闭逐项卸货，避免改变已确认的完整批次数量。</summary>
        public ITab_TrionFacilitySacrifice()
        {
            labelKey = "BDP_TrionFacility_Sacrifice_Tab";
            containedItemsKey = "BDP_TrionFacility_Sacrifice_LoadedGoods";
            canRemoveThings = false;
        }

        /// <summary>来源损毁后原版行会隐藏；追加明确提示，待搬件数仍保留且不能完成献祭。</summary>
        protected override void DoItemsLists(Rect inRect, ref float curY)
        {
            if (Transporter.leftToLoad?.Any(entry => entry.CountToTransfer > 0 && !entry.HasAnyThing) == true)
            {
                string warning = "BDP_TrionFacility_Sacrifice_MissingSupplies".Translate();
                float height = Text.CalcHeight(warning, inRect.width);
                Widgets.Label(new Rect(0, curY, inRect.width, height), warning);
                curY += height + 8f;
            }
            base.DoItemsLists(inRect, ref curY);
        }
    }
}
