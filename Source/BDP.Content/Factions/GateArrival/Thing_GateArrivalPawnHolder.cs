using System.Collections.Generic;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>每扇门各自的地图持有物；待出场 Pawn 的原版持有位置就是当前门格。</summary>
    public sealed class Thing_GateArrivalPawnHolder : Thing, IThingHolder
    {
        /// <summary>本门尚未落地 Pawn 的唯一深存容器。</summary>
        private ThingOwner<Pawn> innerContainer;

        /// <summary>原版创建 Thing 及深存读取都需要无参构造。</summary>
        public Thing_GateArrivalPawnHolder() { innerContainer = new ThingOwner<Pawn>(this); }

        /// <summary>仅供同程序集门控制器读写本门所有权。</summary>
        internal ThingOwner<Pawn> InnerContainer { get { return innerContainer; } }

        /// <summary>将未出场人物容器连同持有物移动到重定向后的门格。</summary>
        internal bool MoveTo(IntVec3 cell, Map map)
        {
            if (!Thing_GateArrival.IsValidDestination(map, cell)) return false;
            if (Spawned && Map == map && Position == cell) return true;

            bool wasSpawned = Spawned;
            IntVec3 oldPosition = Position;
            if (wasSpawned) DeSpawn(DestroyMode.Vanish);
            try
            {
                GenSpawn.Spawn(this, cell, map);
                if (Spawned && Map == map && Position == cell) return true;
            }
            catch
            {
                if (wasSpawned && !Destroyed && !Spawned && oldPosition.IsValid)
                    GenSpawn.Spawn(this, oldPosition, map);
                throw;
            }
            if (wasSpawned && !Destroyed && !Spawned && oldPosition.IsValid)
                GenSpawn.Spawn(this, oldPosition, map);
            return false;
        }

        /// <summary>深存本门 Pawn，使原版 Pawn.PositionHeld 沿持有链取得门格。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        /// <summary>向原版容器树公开本门直接持有人物。</summary>
        public ThingOwner GetDirectlyHeldThings() { return innerContainer; }

        /// <summary>向原版容器树继续公开人物装备等深层持有物。</summary>
        public void GetChildHolders(List<IThingHolder> outChildren)
        { ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, innerContainer); }
    }
}
