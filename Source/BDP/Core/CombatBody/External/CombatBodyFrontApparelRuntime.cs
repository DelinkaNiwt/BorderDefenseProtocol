using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Core.CombatBody.External
{
    /// <summary>
    /// 当前战斗体前台衣物实例登记表。
    /// 该表只描述实例身份，规则提供器可据此隔离普通衣物。
    /// </summary>
    public static class CombatBodyFrontApparelRuntime
    {
        /// <summary>
        /// 当前处于战斗体前台层的衣物实例集合。
        /// 这是可重建的运行时缓存，不作为存档真值。
        /// </summary>
        private static readonly HashSet<Apparel> activeApparel = new HashSet<Apparel>();

        /// <summary>
        /// 当前登记表所属的游戏实例，用于隔离同一进程内的新旧存档。
        /// </summary>
        private static Game registeredGame;

        /// <summary>
        /// 登记一个已经成功穿戴的战斗体前台衣物实例。
        /// </summary>
        public static void Register(Apparel apparel)
        {
            EnsureCurrentGameScope();
            if (apparel != null && !apparel.Destroyed)
            {
                activeApparel.Add(apparel);
            }
        }

        /// <summary>
        /// 注销一个离开战斗体前台层的衣物实例。
        /// </summary>
        public static void Unregister(Apparel apparel)
        {
            EnsureCurrentGameScope();
            if (apparel != null)
            {
                activeApparel.Remove(apparel);
            }
        }

        /// <summary>
        /// 判断一个物品实例是否属于当前战斗体前台层。
        /// </summary>
        public static bool IsActive(Thing thing)
        {
            EnsureCurrentGameScope();
            Apparel apparel = thing as Apparel;
            return apparel != null && !apparel.Destroyed && activeApparel.Contains(apparel);
        }

        /// <summary>
        /// 根据宿主当前穿戴列表和存档中的实例 id 重建运行时登记。
        /// </summary>
        public static void Rebuild(Pawn pawn, IEnumerable<int> activeThingIds)
        {
            EnsureCurrentGameScope();
            if (pawn?.apparel == null)
            {
                return;
            }

            PruneDestroyedEntries();
            HashSet<int> savedIds = new HashSet<int>();
            if (activeThingIds != null)
            {
                foreach (int thingId in activeThingIds)
                {
                    savedIds.Add(thingId);
                }
            }

            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int index = 0; index < wornApparel.Count; index++)
            {
                Apparel apparel = wornApparel[index];
                if (apparel != null && savedIds.Contains(apparel.thingIDNumber))
                {
                    Register(apparel);
                }
            }
        }

        /// <summary>
        /// 清理已经销毁的实例，避免运行时缓存长期持有无效对象。
        /// </summary>
        private static void PruneDestroyedEntries()
        {
            List<Apparel> destroyedApparel = new List<Apparel>();
            foreach (Apparel apparel in activeApparel)
            {
                if (apparel == null || apparel.Destroyed)
                {
                    destroyedApparel.Add(apparel);
                }
            }

            for (int index = 0; index < destroyedApparel.Count; index++)
            {
                activeApparel.Remove(destroyedApparel[index]);
            }
        }

        /// <summary>
        /// 切换存档或新游戏时清理上一游戏的运行时引用。
        /// </summary>
        private static void EnsureCurrentGameScope()
        {
            Game currentGame = Current.Game;
            if (registeredGame == currentGame)
            {
                return;
            }

            activeApparel.Clear();
            registeredGame = currentGame;
        }
    }
}
