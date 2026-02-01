using RimWorld.Planet;
using VEF.Abilities;
using VEF.Buildings;
using Verse;

namespace VanillaPsycastsExpanded.Skipmaster;

public class Ability_Skipdoor : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Skipdoor skipdoor = (Skipdoor)(object)ThingMaker.MakeThing(VPE_DefOf.VPE_Skipdoor);
			skipdoor.Pawn = base.pawn;
			Find.WindowStack.Add((Window)new Dialog_RenameDoorTeleporter((DoorTeleporter)(object)skipdoor));
			GenSpawn.Spawn((Thing)(object)skipdoor, globalTargetInfo.Cell, base.pawn.Map);
		}
	}
}
