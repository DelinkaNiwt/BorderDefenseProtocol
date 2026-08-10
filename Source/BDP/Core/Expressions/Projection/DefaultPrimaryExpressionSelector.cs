namespace BDP.Core.Expressions
{
    /// <summary>
    /// 默认主表达选择器。
    /// 它优先选择正规化后的主攻击 Verb，而不是靠结果顺序猜测。
    /// </summary>
    internal sealed class DefaultPrimaryExpressionSelector
    {
        /// <summary>
        /// 基于当前结果总表选择默认主表达。
        /// </summary>
        public ExpressionSnapshot Select(ExpressionSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Results == null)
            {
                return snapshot;
            }

            FormalExpressionResult firstDual = null;
            FormalExpressionResult firstDualRanged = null;
            FormalExpressionResult firstDualMelee = null;
            FormalExpressionResult firstPrimary = null;
            FormalExpressionResult firstPrimaryRanged = null;
            FormalExpressionResult firstPrimaryMelee = null;

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult result = snapshot.Results[i];
                if (!IsSelectableVerb(result))
                {
                    continue;
                }

                if (IsDualPrimary(result))
                {
                    if (firstDual == null)
                    {
                        firstDual = result;
                    }

                    if (result.WeaponMode == WeaponExpressionMode.Ranged && firstDualRanged == null)
                    {
                        firstDualRanged = result;
                    }

                    if (result.WeaponMode == WeaponExpressionMode.Melee && firstDualMelee == null)
                    {
                        firstDualMelee = result;
                    }
                }

                if (IsSingleSidePrimary(result) && firstPrimary == null)
                {
                    firstPrimary = result;
                }

                if (result.WeaponMode == WeaponExpressionMode.Ranged)
                {
                    if (IsSingleSidePrimary(result) && firstPrimaryRanged == null)
                    {
                        firstPrimaryRanged = result;
                    }
                }

                if (result.WeaponMode == WeaponExpressionMode.Melee)
                {
                    if (IsSingleSidePrimary(result) && firstPrimaryMelee == null)
                    {
                        firstPrimaryMelee = result;
                    }
                }
            }

            snapshot.PrimaryRanged = firstDualRanged ?? firstPrimaryRanged;
            snapshot.PrimaryMelee = firstDualMelee ?? firstPrimaryMelee;
            return snapshot;
        }

        /// <summary>
        /// 判断当前结果是否可参与默认主表达选择。
        /// </summary>
        private static bool IsSelectableVerb(FormalExpressionResult result)
        {
            return result != null
                && result.ResultKind == ExpressionResultKind.Verb
                && result.IsAvailable
                && (result.UseRequirementCheck == null
                    || result.UseRequirementCheck.Satisfied);
        }

        /// <summary>
        /// 判断当前结果是否为自动攻击优先使用的双持主攻。
        /// </summary>
        private static bool IsDualPrimary(FormalExpressionResult result)
        {
            return result != null
                && result.CompositeKind == CompositeExpressionKind.DualWeapon
                && result.VerbAttackRole == VerbAttackRole.Primary;
        }

        /// <summary>
        /// 判断当前结果是否为可回退的单侧主攻。
        /// </summary>
        private static bool IsSingleSidePrimary(FormalExpressionResult result)
        {
            return result != null
                && result.CompositeKind == CompositeExpressionKind.None
                && result.VerbAttackRole == VerbAttackRole.Primary
                && (result.OriginKind == ExpressionOriginKind.Main
                    || result.OriginKind == ExpressionOriginKind.Sub);
        }
    }
}
