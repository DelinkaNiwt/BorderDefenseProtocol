using System.Collections.Generic;
using Verse;

namespace BDP.Core.Requirements
{
    /// <summary>
    /// 角色条件的唯一有序描述与求值器。
    /// </summary>
    public sealed class PawnRequirementEvaluator
    {
        /// <summary>共享无状态实例。</summary>
        public static readonly PawnRequirementEvaluator Instance = new PawnRequirementEvaluator();

        /// <summary>禁止外部创建重复求值器。</summary>
        private PawnRequirementEvaluator()
        {
        }

        /// <summary>按作者顺序构造全部静态条件说明。</summary>
        public IReadOnlyList<PawnRequirementSnapshot> Describe(
            IReadOnlyList<PawnRequirement> requirements)
        {
            List<PawnRequirementSnapshot> snapshots = new List<PawnRequirementSnapshot>();
            if (requirements == null)
            {
                return snapshots.AsReadOnly();
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                PawnRequirement requirement = requirements[i];
                if (requirement != null)
                {
                    PawnRequirementSnapshot snapshot = requirement.Describe();
                    if (snapshot != null)
                    {
                        snapshots.Add(snapshot);
                    }
                }
            }

            return snapshots.AsReadOnly();
        }

        /// <summary>按作者顺序检查全部条件，并一次收集所有失败项。</summary>
        public PawnRequirementCheckResult Evaluate(
            Pawn pawn,
            IReadOnlyList<PawnRequirement> requirements)
        {
            List<PawnRequirementSnapshot> snapshots = new List<PawnRequirementSnapshot>();
            List<PawnRequirementSnapshot> failures = new List<PawnRequirementSnapshot>();
            if (requirements != null)
            {
                for (int i = 0; i < requirements.Count; i++)
                {
                    PawnRequirement requirement = requirements[i];
                    if (requirement == null)
                    {
                        continue;
                    }

                    PawnRequirementSnapshot snapshot = requirement.Evaluate(pawn)
                        ?? PawnRequirementSnapshot.Evaluation(
                            requirement.Label,
                            "BDP_Requirement_UnknownValue".Translate().ToString(),
                            "BDP_Requirement_UnknownValue".Translate().ToString(),
                            false,
                            "BDP_Message_Requirement_MissingEvaluation".Translate(requirement.Label).ToString());
                    snapshots.Add(snapshot);
                    if (!snapshot.IsSatisfied)
                    {
                        failures.Add(snapshot);
                    }
                }
            }

            return new PawnRequirementCheckResult
            {
                Satisfied = failures.Count == 0,
                Requirements = snapshots.AsReadOnly(),
                Failures = failures.AsReadOnly()
            };
        }
    }
}
