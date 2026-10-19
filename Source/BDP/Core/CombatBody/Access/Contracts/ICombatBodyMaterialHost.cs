using BDP.Core.CombatBody.Wounds;

namespace BDP.Core.CombatBody
{
    /// <summary>只描述战斗体材料事实与伤口实例，不提供原身转换或死亡保护。</summary>
    public interface ICombatBodyMaterialHost
    {
        /// <summary>身体是否为活跃战斗体；与是否允许行动分别判断。</summary>
        bool IsCombatBodyActive { get; }
        /// <summary>当前是否保留伤口生命周期；停机的永久身体仍保留。</summary>
        bool HasCombatBodyWoundRuntime { get; }
        /// <summary>部位泄露资格是否要求原版生理流血系数。</summary>
        bool RequiresBiologicalWoundPart { get; }
        /// <summary>该宿主唯一持有的伤口运行时。</summary>
        CombatBodyWoundRuntime CombatBodyWounds { get; }
    }
}
