using Verse;

namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 自然恢复门控的中性接口。
    /// Core 只读取本次恢复倍率，不认识具体建筑、地图或派系规则。
    /// </summary>
    public interface ITrionRecoveryGate
    {
        /// <summary>
        /// 读取指定 Pawn 当前自然恢复相对于基准速率的倍率。
        /// </summary>
        /// <param name="pawn">拥有当前 Trion 组件的 Pawn。</param>
        /// <returns>0 表示停止恢复；1 表示基准速率；正值表示相应倍率。</returns>
        float GetRecoveryMultiplier(Pawn pawn);
    }
}
