using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.VerbHosting;
using BDP.Core.Verbs;
using RimWorld;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger formal host fallback 声明面。
    /// 这一层只负责为 BDP 内部 formal host 壳提供稳定可重建的 fallback VerbProperties。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>
        /// 当前 TriggerBody 内部 formal host 壳使用的 fallback VerbProperties 列表。
        /// </summary>
        private List<VerbProperties> formalHostVerbProperties;

        /// <summary>
        /// 当前 TriggerBody 固定使用的正式宿主槽位顺序。
        /// 顺序稳定，VerbTracker 由此得到稳定 loadID。
        /// </summary>
        internal static readonly BdpFormalVerbHostSlot[] FormalHostSlots =
        {
            BdpFormalVerbHostSlot.MainPrimary,
            BdpFormalVerbHostSlot.MainSecondary,
            BdpFormalVerbHostSlot.SubPrimary,
            BdpFormalVerbHostSlot.SubSecondary,
            BdpFormalVerbHostSlot.DualPrimary,
            BdpFormalVerbHostSlot.DualSecondary,
            BdpFormalVerbHostSlot.ComboPrimary,
            BdpFormalVerbHostSlot.ComboSecondary
        };

        /// <summary>
        /// 读取指定槽位和模式的 formal host fallback VerbProperties。
        /// 它只用于 BDP 内部壳在未绑定状态下维持最小可重建表面。
        /// </summary>
        internal VerbProperties GetFormalHostFallbackVerbProps(BdpFormalVerbHostSlot slot, WeaponExpressionMode weaponMode)
        {
            EnsureFormalHostVerbDeclarations();
            int baseIndex = ResolveFormalHostIndex(slot);
            if (baseIndex < 0)
            {
                return null;
            }

            int offset = weaponMode == WeaponExpressionMode.Melee ? 1 : 0;
            int index = (baseIndex * 2) + offset;
            return index >= 0 && index < formalHostVerbProperties.Count
                ? formalHostVerbProperties[index]
                : null;
        }

        /// <summary>
        /// 确保正式宿主声明列表已经建立。
        /// </summary>
        private void EnsureFormalHostVerbDeclarations()
        {
            if (formalHostVerbProperties != null)
            {
                return;
            }

            formalHostVerbProperties = new List<VerbProperties>();
            for (int i = 0; i < FormalHostSlots.Length; i++)
            {
                BdpFormalVerbHostSlot slot = FormalHostSlots[i];
                formalHostVerbProperties.Add(BuildFormalHostVerbProps(slot, WeaponExpressionMode.Ranged));
                formalHostVerbProperties.Add(BuildFormalHostVerbProps(slot, WeaponExpressionMode.Melee));
            }
        }

        /// <summary>
        /// 为给定槽位和模式构建稳定的正式宿主占位 VerbProperties。
        /// </summary>
        private static VerbProperties BuildFormalHostVerbProps(BdpFormalVerbHostSlot slot, WeaponExpressionMode weaponMode)
        {
            VerbProperties verbProps = new VerbProperties
            {
                verbClass = weaponMode == WeaponExpressionMode.Melee
                    ? typeof(BdpVerb_FormalHostMelee)
                    : typeof(BdpVerb_FormalHostShoot),
                hasStandardCommand = false,
                label = "BDP Formal Host " + slot + " " + weaponMode,
                range = weaponMode == WeaponExpressionMode.Melee ? 1.42f : 90f,
                warmupTime = 0f
            };

            if (weaponMode == WeaponExpressionMode.Melee)
            {
                verbProps.meleeDamageDef = DamageDefOf.Blunt;
                verbProps.defaultCooldownTime = 0f;
            }

            return verbProps;
        }

        /// <summary>
        /// 把固定槽位映射到 formal host 声明列表中的稳定基序号。
        /// </summary>
        private static int ResolveFormalHostIndex(BdpFormalVerbHostSlot slot)
        {
            for (int i = 0; i < FormalHostSlots.Length; i++)
            {
                if (FormalHostSlots[i] == slot)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
