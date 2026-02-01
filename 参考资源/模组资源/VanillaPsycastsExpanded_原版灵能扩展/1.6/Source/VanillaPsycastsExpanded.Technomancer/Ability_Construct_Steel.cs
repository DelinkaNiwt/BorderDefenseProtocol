using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class Ability_Construct_Steel : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(VPE_DefOf.VPE_SteelConstruct, base.pawn.Faction);
			pawn.TryGetComp<CompBreakLink>().Pawn = base.pawn;
			Thing thing = globalTargetInfo.Thing;
			GenSpawn.Spawn(pawn, thing.Position, thing.Map, thing.Rotation);
			thing.SplitOff(1).Destroy();
		}
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!((Ability)this).ValidateTarget(target, showMessages))
		{
			return false;
		}
		if (!target.HasThing)
		{
			return false;
		}
		if (target.Thing.def != ThingDefOf.ChunkSlagSteel)
		{
			if (showMessages)
			{
				Messages.Message("VPE.MustBeSteelSlag".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		if (base.pawn.psychicEntropy.MaxEntropy - base.pawn.psychicEntropy.EntropyValue <= 20f)
		{
			if (showMessages)
			{
				Messages.Message("VPE.NotEnoughHeat".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return true;
	}
}
