using System.Linq;
using System.Runtime.CompilerServices;
using BDP.Core.Bootstrap;
using BDP.Core.CombatBody;
using Verse;

namespace BDP.Content.PlayerLog.CombatBody
{
    /// <summary>只给已有战斗体宿主的人物定义接入关键事件记录。</summary>
    internal static class PawnCombatBodyPlayerLogInjector
    {
        /// <summary>启动期追加一个普通原版组件属性，不改其它人物。</summary>
        internal static void Apply()
        {
            // 原版不保证不同程序集的启动构造顺序；先完成既有宿主注入，再筛选宿主定义。
            RuntimeHelpers.RunClassConstructor(typeof(PawnCompBootstrap).TypeHandle);
            foreach (ThingDef definition in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (definition.category != ThingCategory.Pawn || definition.comps == null
                    || !definition.comps.Any(comp => comp is CompProperties_CombatBodyHost)
                    || definition.comps.Any(comp => comp.compClass == typeof(CompCombatBodyPlayerLog))) continue;
                definition.comps.Add(new CompProperties(typeof(CompCombatBodyPlayerLog)));
            }
        }
    }
}
