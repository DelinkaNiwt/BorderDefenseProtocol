using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 快照排除配置Def。
    /// 声明哪些Hediff不参与战斗体快照→回滚流程。
    ///
    /// 支持两种维度：
    ///   excludedHediffs      — 按具体HediffDef排除（精确匹配）
    ///   excludedHediffClasses — 按C#类名排除（含所有子类，IsAssignableFrom）
    ///
    /// 设计原则：
    ///   - 不修改任何原版Def，零侵入
    ///   - 其他模组可添加自己的BDPSnapshotConfigDef，启动时自动合并
    ///   - C#代码对具体defName/类名一无所知，完全数据驱动
    /// </summary>
    public class BDPSnapshotConfigDef : Def
    {
        /// <summary>按具体HediffDef排除。</summary>
        public List<HediffDef> excludedHediffs = new List<HediffDef>();

        /// <summary>
        /// 按C#类名排除（含子类）。
        /// 填完整类名，如 "Verse.Hediff_Psylink"。
        /// 启动时通过 GenTypes.GetTypeInAnyAssembly 解析。
        /// </summary>
        public List<string> excludedHediffClasses = new List<string>();

        // ── 缓存（懒初始化，首次调用 IsExcluded 时构建） ──
        private static HashSet<HediffDef> _defCache;
        private static List<Type> _typeCache;

        /// <summary>
        /// 判断指定Hediff是否应被排除在快照之外。
        /// </summary>
        public static bool IsExcluded(Hediff h)
        {
            if (_defCache == null)
                BuildCache();

            if (_defCache.Contains(h.def))
                return true;

            foreach (var t in _typeCache)
                if (t.IsAssignableFrom(h.GetType()))
                    return true;

            return false;
        }

        private static void BuildCache()
        {
            _defCache = new HashSet<HediffDef>();
            _typeCache = new List<Type>();

            foreach (var cfg in DefDatabase<BDPSnapshotConfigDef>.AllDefs)
            {
                foreach (var d in cfg.excludedHediffs)
                    _defCache.Add(d);

                foreach (var typeName in cfg.excludedHediffClasses)
                {
                    var t = GenTypes.GetTypeInAnyAssembly(typeName);
                    if (t != null)
                        _typeCache.Add(t);
                    else
                        Log.Warning($"[BDP] BDPSnapshotConfigDef: 未找到类型 \"{typeName}\"，请检查类名是否正确");
                }
            }
        }
    }
}
