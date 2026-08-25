namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程模块对已激活武器手持贴图的可选覆盖口。
    /// Core 只读取 DefName，不理解具体业务模块和贴图用途。
    /// </summary>
    public interface IRangedModuleWeaponVisualOverride
    {
        /// <summary>
        /// 要替换当前激活武器贴图的 ExpressionVisualPresetDef DefName。
        /// </summary>
        string WeaponVisualPresetDefName { get; }
    }
}
