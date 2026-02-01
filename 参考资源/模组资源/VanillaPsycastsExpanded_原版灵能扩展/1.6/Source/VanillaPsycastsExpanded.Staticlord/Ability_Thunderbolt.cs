using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Staticlord;

public class Ability_Thunderbolt : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			foreach (Thing item in globalTargetInfo.Cell.GetThingList(globalTargetInfo.Map).ListFullCopy())
			{
				item.TakeDamage(new DamageInfo(DamageDefOf.Flame, 25f, -1f, base.pawn.DrawPos.AngleToFlat(item.DrawPos), base.pawn));
			}
			GenExplosion.DoExplosion(globalTargetInfo.Cell, globalTargetInfo.Map, ((Ability)this).GetRadiusForPawn(), DamageDefOf.EMP, base.pawn);
			base.pawn.Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(base.pawn.Map, globalTargetInfo.Cell));
		}
	}
}
