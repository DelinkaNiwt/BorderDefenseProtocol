using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

[StaticConstructorOnStartup]
public class Ability_Construct_Rock : Ability
{
	private static readonly HashSet<ThingDef> chunkCache;

	static Ability_Construct_Rock()
	{
		chunkCache = new HashSet<ThingDef>();
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (IsNonResourceNaturalRock(allDef) && allDef.building.mineableThing != null)
			{
				chunkCache.Add(allDef.building.mineableThing);
			}
		}
	}

	public static bool IsNonResourceNaturalRock(ThingDef def)
	{
		if (def.category == ThingCategory.Building && def.building != null && def.building.isNaturalRock && !def.building.isResourceRock)
		{
			return !def.IsSmoothed;
		}
		return false;
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(VPE_DefOf.VPE_RockConstruct, base.pawn.Faction);
			pawn.TryGetComp<CompBreakLink>().Pawn = base.pawn;
			Thing thing = globalTargetInfo.Thing;
			GenSpawn.Spawn(pawn, thing.Position, thing.Map, thing.Rotation);
			pawn.TryGetComp<CompSetStoneColour>().SetStoneColour(thing.def);
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
		if (!chunkCache.Contains(target.Thing.def))
		{
			if (showMessages)
			{
				Messages.Message("VPE.MustBeStoneChunk".Translate(), MessageTypeDefOf.RejectInput, historical: false);
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
