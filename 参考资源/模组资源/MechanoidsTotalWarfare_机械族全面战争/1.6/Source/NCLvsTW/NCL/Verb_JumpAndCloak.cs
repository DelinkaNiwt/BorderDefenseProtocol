using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NCL;

public class Verb_JumpAndCloak : Verb_Jump
{
	private const int CooldownTicks = 60;

	private static Dictionary<Pawn, int> _lastUseTicks = new Dictionary<Pawn, int>();

	private static readonly HediffDef CloakedHediff = HediffDef.Named("GD_Hediff_CloakedEffect");

	private bool IsOnCooldown(Pawn pawn)
	{
		if (pawn == null || !pawn.Spawned || pawn.Dead)
		{
			if (_lastUseTicks.ContainsKey(pawn))
			{
				_lastUseTicks.Remove(pawn);
			}
			return false;
		}
		int lastTick;
		return _lastUseTicks.TryGetValue(pawn, out lastTick) && Find.TickManager.TicksGame - lastTick < 60;
	}

	public override bool Available()
	{
		return base.Available() && !IsOnCooldown(CasterPawn);
	}

	protected override bool TryCastShot()
	{
		if (!base.TryCastShot())
		{
			return false;
		}
		Pawn caster = CasterPawn;
		if (CloakedHediff == null || caster == null)
		{
			return false;
		}
		Hediff existing = caster.health.hediffSet.GetFirstHediffOfDef(CloakedHediff);
		if (existing != null)
		{
			caster.health.RemoveHediff(existing);
		}
		Hediff cloaked = HediffMaker.MakeHediff(CloakedHediff, caster);
		caster.health.AddHediff(cloaked);
		_lastUseTicks[caster] = Find.TickManager.TicksGame;
		return true;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			List<Pawn> keys = _lastUseTicks.Keys.Where((Pawn p) => p != null).ToList();
			List<int> values = keys.Select((Pawn k) => _lastUseTicks[k]).ToList();
			Scribe_Collections.Look(ref keys, "_lastUseTicks_keys", LookMode.Reference);
			Scribe_Collections.Look(ref values, "_lastUseTicks_values", LookMode.Value);
		}
		else if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			List<Pawn> keys2 = null;
			List<int> values2 = null;
			Scribe_Collections.Look(ref keys2, "_lastUseTicks_keys", LookMode.Reference);
			Scribe_Collections.Look(ref values2, "_lastUseTicks_values", LookMode.Value);
			_lastUseTicks = new Dictionary<Pawn, int>();
			if (keys2 != null && values2 != null && keys2.Count == values2.Count)
			{
				for (int i = 0; i < keys2.Count; i++)
				{
					if (keys2[i] != null)
					{
						_lastUseTicks[keys2[i]] = values2[i];
					}
				}
			}
		}
		if (Scribe.mode != LoadSaveMode.PostLoadInit)
		{
			return;
		}
		List<KeyValuePair<Pawn, int>> toRemove = _lastUseTicks.Where((KeyValuePair<Pawn, int> keyValuePair) => keyValuePair.Key == null || keyValuePair.Key.Destroyed).ToList();
		foreach (KeyValuePair<Pawn, int> pair in toRemove)
		{
			_lastUseTicks.Remove(pair.Key);
		}
	}
}
