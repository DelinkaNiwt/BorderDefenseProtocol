using Verse;

namespace BDP.Core.CombatBody.External
{
    /// <summary>
    /// 战斗体被动崩解扩展提供器。
    /// Core 只定义生命周期通知，不认识具体扩展业务。
    /// </summary>
    public interface ICombatBodyCollapseExtensionProvider
    {
        /// <summary>
        /// 崩解刚开始时准备扩展自己的缓存状态。
        /// </summary>
        void Prepare(Pawn pawn);

        /// <summary>
        /// 崩解表现结束后执行扩展附加阶段。
        /// </summary>
        void Execute(Pawn pawn);

        /// <summary>
        /// 清理扩展可能残留的运行状态。
        /// </summary>
        void Clear(Pawn pawn);
    }
}
