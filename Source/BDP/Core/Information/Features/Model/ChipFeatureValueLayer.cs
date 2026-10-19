namespace BDP.Core.Information
{
    /// <summary>
    /// 功能属性贡献所在的值层级。
    /// </summary>
    public enum ChipFeatureValueLayer
    {
        /// <summary>作者在声明中直接提供的值。</summary>
        DeclaredDefinition,

        /// <summary>有效定义或制造/构型合并后的值。</summary>
        EffectiveDefinition,

        /// <summary>运行时正式结果或当前状态值。</summary>
        Runtime
    }
}
