using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimWorld;

public class CompAutoMechSpawner : ThingComp
{
	public bool autoSpawn = false;

	private CompMechCarrier mechCarrier;

	private const int CheckInterval = 60;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		mechCarrier = parent.GetComp<CompMechCarrier>();
	}

	public override void CompTick()
	{
		base.CompTick();
		if (Find.TickManager.TicksGame % 60 == 0 && autoSpawn && mechCarrier != null && ShouldAutoSpawn())
		{
			mechCarrier.TrySpawnPawns();
		}
	}

	private bool ShouldAutoSpawn()
	{
		if (parent is Pawn pawn && (pawn.DestroyedOrNull() || !pawn.Spawned || pawn.Downed || pawn.Dead || !pawn.Awake()))
		{
			return false;
		}
		FieldInfo cooldownField = typeof(CompMechCarrier).GetField("cooldownTicksRemaining", BindingFlags.Instance | BindingFlags.NonPublic);
		if (cooldownField != null)
		{
			int cooldown = (int)cooldownField.GetValue(mechCarrier);
			if (cooldown > 0)
			{
				return false;
			}
		}
		PropertyInfo maxCanSpawnProperty = typeof(CompMechCarrier).GetProperty("MaxCanSpawn");
		if (maxCanSpawnProperty != null)
		{
			int maxCanSpawn = (int)maxCanSpawnProperty.GetValue(mechCarrier);
			return maxCanSpawn > 0;
		}
		return false;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (parent.Faction != Faction.OfPlayer || mechCarrier == null)
		{
			yield break;
		}
		yield return new Command_Toggle
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/CompAutoMechSpawner"),
			defaultLabel = "AutoReleaseMechs".Translate(),
			defaultDesc = "AutoReleaseMechsDesc".Translate(),
			isActive = () => autoSpawn,
			toggleAction = delegate
			{
				autoSpawn = !autoSpawn;
			}
		};
		if (Prefs.DevMode)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Toggle AutoSpawn",
				action = delegate
				{
					autoSpawn = !autoSpawn;
				}
			};
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref autoSpawn, "autoSpawn", defaultValue: false);
	}
}
