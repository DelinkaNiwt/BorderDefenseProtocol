using Verse;

namespace BDP.Core.CombatBody.Presentation
{
    /// <summary>
    /// 战斗体宿主变换表现提供器。
    /// Core 只通知变换开始前与完成后，不规定具体表现形式。
    /// </summary>
    public interface ICombatBodyTransformPresentationProvider
    {
        /// <summary>
        /// 在宿主真实变换开始前通知表现提供器。
        /// </summary>
        void Begin(Pawn pawn, CombatBodyTransformDirection direction);

        /// <summary>
        /// 在宿主真实变换完成后通知表现提供器。
        /// </summary>
        void End(Pawn pawn, CombatBodyTransformDirection direction);
    }
}
