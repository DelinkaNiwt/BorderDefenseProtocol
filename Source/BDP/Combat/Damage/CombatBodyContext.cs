using BDP.Core;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 伤害Pipeline共享上下文。入口构建一次，各Handler共享引用。
    /// 避免每个Handler独立执行GetFirstGeneOfType/GetComp查找。
    ///
    /// v13.0重构：使用Runtime替代Gene直接访问
    /// </summary>
    public struct CombatBodyContext
    {
        public Pawn Pawn;
        public CombatBodyRuntime Runtime;
        public CompTrion CompTrion;
        public ShadowHPTracker ShadowHP;
        public PartDestructionHandler PartDestruction;

        /// <summary>
        /// 上下文是否有效（Runtime和CompTrion都存在）。
        /// </summary>
        public bool IsValid => Runtime != null && CompTrion != null;

        /// <summary>
        /// 从Pawn构建上下文（入口查一次，后续共享）。
        /// </summary>
        public static CombatBodyContext Create(Pawn pawn)
        {
            var runtime = CombatBodyRuntime.Of(pawn);
            return new CombatBodyContext
            {
                Pawn = pawn,
                Runtime = runtime,
                CompTrion = pawn?.GetComp<CompTrion>(),
                ShadowHP = runtime?.ShadowHP,
                PartDestruction = runtime?.PartDestruction,
            };
        }
    }
}
