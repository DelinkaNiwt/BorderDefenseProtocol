using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Chronopath;

[HarmonyPatch]
public class GameCondition_RaidPause : GameCondition_TimeSnow
{
	private Sustainer sustainer;

	public override void GameConditionTick()
	{
		base.GameConditionTick();
		if (base.TicksPassed % 60 == 0)
		{
			foreach (Map affectedMap in base.AffectedMaps)
			{
				foreach (Pawn item in affectedMap.attackTargetsCache.TargetsHostileToColony.OfType<Pawn>())
				{
					item.stances.stunner.StunFor(61, null, addBattleLog: false);
				}
			}
		}
		if (sustainer == null)
		{
			sustainer = VPE_DefOf.VPE_RaidPause_Sustainer.TrySpawnSustainer(SoundInfo.OnCamera());
		}
		else
		{
			sustainer.Maintain();
		}
	}

	public override void End()
	{
		sustainer.End();
		base.End();
	}

	[HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage")]
	[HarmonyPostfix]
	public static void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt, Pawn ___pawn)
	{
		if (!(totalDamageDealt >= 0f) || !dinfo.Def.ExternalViolenceFor(___pawn))
		{
			return;
		}
		Thing instigator = dinfo.Instigator;
		if (instigator == null || !___pawn.HostileTo(instigator) || instigator.HostileTo(Faction.OfPlayer) || ___pawn.MapHeld?.gameConditionManager == null)
		{
			return;
		}
		foreach (GameCondition_RaidPause item in ___pawn.MapHeld.gameConditionManager.ActiveConditions.OfType<GameCondition_RaidPause>().ToList())
		{
			item.End();
		}
	}
}
