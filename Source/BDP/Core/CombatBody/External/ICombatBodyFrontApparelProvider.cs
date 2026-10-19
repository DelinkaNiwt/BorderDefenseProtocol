using RimWorld;

namespace BDP.Core.CombatBody.External
{
    /// <summary>
    /// 战斗体前台衣物准备提供器。
    /// Core 只提供统一生命周期入口，不规定衣物的具体业务属性。
    /// </summary>
    public interface ICombatBodyFrontApparelProvider
    {
        /// <summary>
        /// 判断原身衣物是否参与镜像前台生成。
        /// </summary>
        bool CanMirror(Apparel apparel);

        /// <summary>
        /// 在战斗体前台衣物穿戴前准备一个衣物实例。
        /// </summary>
        void Prepare(Apparel apparel);
    }
}
