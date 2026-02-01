using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_GuardianSkipBarrier : Ability, IChannelledPsycast, ILoadReferenceable
{
	public bool IsActive => base.pawn.health.hediffSet.HasHediff(VPE_DefOf.VPE_GuardianSkipBarrier);

	public override Gizmo GetGizmo()
	{
		Hediff hediff = base.pawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_GuardianSkipBarrier);
		if (hediff != null)
		{
			return new Command_Action
			{
				defaultLabel = "VPE.CancelSkipbarrier".Translate(),
				defaultDesc = "VPE.CancelSkipbarrierDesc".Translate(),
				icon = base.def.icon,
				action = delegate
				{
					base.pawn.health.RemoveHediff(hediff);
				},
				Order = 10f + (float)(int)(base.def.requiredHediff?.hediffDef?.index).GetValueOrDefault() + (float)(base.def.requiredHediff?.minimumLevel ?? 0)
			};
		}
		return ((Ability)this).GetGizmo();
	}
}
