using System.Linq;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_Resurrect : Ability_TargetCorpse
{
	public override Gizmo GetGizmo()
	{
		Gizmo gizmo = ((Ability)this).GetGizmo();
		if ((from x in ((Ability)this).pawn.health.hediffSet.GetNotMissingParts()
			where x.def == VPE_DefOf.Finger
			select x).All((BodyPartRecord finger) => ((Ability)this).pawn.health.hediffSet.hediffs.Any((Hediff hediff) => hediff.def == VPE_DefOf.VPE_Sacrificed && hediff.Part == finger)))
		{
			gizmo.Disable("VPE.NoAvailableFingers".Translate());
		}
		return gizmo;
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if ((from finger in ((Ability)this).pawn.health.hediffSet.GetNotMissingParts()
				where finger.def == VPE_DefOf.Finger
				where !((Ability)this).pawn.health.hediffSet.hediffs.Any((Hediff hediff) => hediff.def == VPE_DefOf.VPE_Sacrificed && hediff.Part == finger)
				select finger).TryRandomElement(out var result))
			{
				Corpse corpse = globalTargetInfo.Thing as Corpse;
				SoulFromSky obj = SkyfallerMaker.MakeSkyfaller(VPE_DefOf.VPE_SoulFromSky) as SoulFromSky;
				obj.target = corpse;
				GenPlace.TryPlaceThing(obj, corpse.Position, corpse.Map, ThingPlaceMode.Direct);
				((Ability)this).pawn.health.AddHediff(HediffMaker.MakeHediff(VPE_DefOf.VPE_Sacrificed, ((Ability)this).pawn, result), result);
			}
		}
	}
}
