namespace BDP.Core.Chips
{
    /// <summary>可选的宿主芯片激活资格豁免；不改变装配、费用或功能运行资格。</summary>
    public interface IChipActivationRequirementExemption
    {
        /// <summary>宿主是否明确豁免芯片声明的角色激活条件，未声明者仍按原条件检查。</summary>
        bool IgnoresChipActivationRequirements { get; }
    }
}
