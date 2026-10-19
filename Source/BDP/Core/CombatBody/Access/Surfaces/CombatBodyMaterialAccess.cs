using BDP.Core.CombatBody.Wounds;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>从原版组件读取材料宿主，不将业务组件注册到全局缓存。</summary>
    public static class CombatBodyMaterialAccess
    {
        /// <summary>取得唯一材料宿主；人类会话与永久身体不得同时挂载。</summary>
        public static ICombatBodyMaterialHost Resolve(Pawn pawn)
        {
            if (pawn?.AllComps == null) return null;
            foreach (ThingComp comp in pawn.AllComps)
                if (comp is ICombatBodyMaterialHost host) return host;
            return null;
        }
        /// <summary>读取身体身份，不返回人类转换会话。</summary>
        public static bool IsActive(Pawn pawn) => Resolve(pawn)?.IsCombatBodyActive == true;
        /// <summary>取得既有伤口实例，便于移除伤口时清理派生来源。</summary>
        public static CombatBodyWoundRuntime Wounds(Pawn pawn) => Resolve(pawn)?.CombatBodyWounds;
    }
}
