using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体前台衣物层状态。
    /// 它只负责记录战斗体前台衣物本身，不混入 snapshot 状态。
    /// </summary>
    internal sealed class CombatBodyFrontState : IExposable, IThingHolder
    {
        /// <summary>
        /// 战斗体前台衣物容器。
        /// </summary>
        private ThingOwner<Apparel> combatApparelContainer;

        /// <summary>
        /// 当前前台层是否已应用。
        /// </summary>
        public bool IsApplied;

        /// <summary>
        /// 当前是否由镜像模式生成。
        /// </summary>
        public bool IsMirrorOriginal;

        /// <summary>
        /// 当前激活时挂上的前台衣物 id 列表。
        /// </summary>
        private List<int> activeApparelThingIds = new List<int>();

        /// <summary>
        /// 当前前台状态持有者。
        /// </summary>
        private IThingHolder holder;

        /// <summary>
        /// 绑定持有者，并确保容器存在。
        /// </summary>
        public void Bind(IThingHolder holder)
        {
            this.holder = holder;
            if (activeApparelThingIds == null)
            {
                activeApparelThingIds = new List<int>();
            }

            if (combatApparelContainer == null)
            {
                combatApparelContainer = new ThingOwner<Apparel>(this);
            }
        }

        /// <summary>
        /// 前台衣物容器。
        /// </summary>
        public ThingOwner<Apparel> CombatApparelContainer
        {
            get { return combatApparelContainer; }
        }

        /// <summary>
        /// 当前前台层激活衣物 id 列表。
        /// </summary>
        public List<int> ActiveApparelThingIds
        {
            get { return activeApparelThingIds; }
        }

        /// <summary>
        /// 父持有者。
        /// </summary>
        public IThingHolder ParentHolder
        {
            get { return holder; }
        }

        /// <summary>
        /// 直接持有容器。
        /// </summary>
        public ThingOwner GetDirectlyHeldThings()
        {
            Bind(holder);
            return combatApparelContainer;
        }

        /// <summary>
        /// 追加子容器。
        /// </summary>
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            Bind(holder);
            if (combatApparelContainer != null)
            {
                ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, combatApparelContainer);
            }
        }

        /// <summary>
        /// 清空当前前台激活记录。
        /// </summary>
        public void ClearActiveRecord()
        {
            activeApparelThingIds.Clear();
            IsApplied = false;
            IsMirrorOriginal = false;
        }

        /// <summary>
        /// 存读档前台状态。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Deep.Look(ref combatApparelContainer, "combatApparelContainer", this);
            Scribe_Values.Look(ref IsApplied, "isApplied", false);
            Scribe_Values.Look(ref IsMirrorOriginal, "isMirrorOriginal", false);
            Scribe_Collections.Look(ref activeApparelThingIds, "activeApparelThingIds", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Bind(holder);
            }
        }
    }
}
