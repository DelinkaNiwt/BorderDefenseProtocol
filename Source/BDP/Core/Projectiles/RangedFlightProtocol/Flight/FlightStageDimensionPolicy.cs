using BDP.Core.AttackExecution;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Flight
{
    /// <summary>
    /// Flight 阶段维度裁决辅助。
    /// 它把同维度的最终拥有权解释为“最后声明该维度的模块生效”。
    /// </summary>
    internal static class FlightStageDimensionPolicy
    {
        public static ModuleStageArbitrator BuildArbitrator(
            System.Collections.Generic.IReadOnlyList<FlightContribution> contributions)
        {
            ModuleStageArbitrator arbitrator = new ModuleStageArbitrator();
            if (contributions == null)
            {
                return arbitrator;
            }

            for (int i = 0; i < contributions.Count; i++)
            {
                FlightContribution contribution = contributions[i];
                if (contribution?.Declarations == null)
                {
                    continue;
                }

                for (int j = 0; j < contribution.Declarations.Count; j++)
                {
                    FlightCompatibilityDeclaration declaration = contribution.Declarations[j];
                    if (declaration?.ExclusiveDimensions == null)
                    {
                        continue;
                    }

                    arbitrator.TryClaimOverride(declaration.ExclusiveDimensions, i);
                }
            }

            return arbitrator;
        }

        public static bool CanApply(
            ModuleStageArbitrator arbitrator,
            FlightContribution contribution,
            FlightDimension dimension,
            int moduleIndex)
        {
            if (arbitrator == null)
            {
                return false;
            }

            return arbitrator.CanApply(
                dimension,
                moduleIndex,
                ClaimsExclusiveDimension(contribution, dimension));
        }

        private static bool ClaimsExclusiveDimension(FlightContribution contribution, FlightDimension dimension)
        {
            if (contribution?.Declarations == null)
            {
                return false;
            }

            for (int i = 0; i < contribution.Declarations.Count; i++)
            {
                FlightCompatibilityDeclaration declaration = contribution.Declarations[i];
                if (declaration?.ExclusiveDimensions == null)
                {
                    continue;
                }

                for (int j = 0; j < declaration.ExclusiveDimensions.Count; j++)
                {
                    if (declaration.ExclusiveDimensions[j] == dimension)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
