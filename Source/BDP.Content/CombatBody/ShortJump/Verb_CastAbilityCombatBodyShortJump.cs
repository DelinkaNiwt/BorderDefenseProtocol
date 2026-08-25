using BDP.Core.Abilities;
using RimWorld;
using Verse;

namespace BDP.Content.CombatBody
{
    /// <summary>
    /// 战斗体短距跳跃的能力 Verb（动作入口）。
    /// 复用原版跳跃校验和飞行流程，只替换为 BDP 专用快速飞行器。
    /// </summary>
    public sealed class Verb_CastAbilityCombatBodyShortJump : BdpVerb_CastAbilityJump
    {
        /// <summary>
        /// 返回战斗体短距跳跃专用的 PawnFlyer（飞行载体）定义。
        /// 运行期解析 Def，避免在 Def 加载期间访问尚未完成初始化的静态 DefOf 字段。
        /// </summary>
        public override ThingDef JumpFlyerDef
        {
            get
            {
                return DefDatabase<ThingDef>.GetNamed("BDP_PawnFlyer_CombatBodyShortJump");
            }
        }
    }
}
