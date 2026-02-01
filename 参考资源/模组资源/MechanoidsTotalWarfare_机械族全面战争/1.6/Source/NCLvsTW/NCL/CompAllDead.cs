using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompAllDead : ThingComp
{
	public int ticks = 0;

	public List<Pawn> pawns;

	public CompProperties_AllDead Props => props as CompProperties_AllDead;

	public Pawn Owner => parent as Pawn;

	public bool CanApply => Owner != null && !pawns.NullOrEmpty();

	public override void CompTick()
	{
		base.CompTick();
		if (Owner == null || !Owner.Spawned || Owner.Map?.mapPawns == null)
		{
			return;
		}
		ticks++;
		if (ticks > 100)
		{
			ticks = 0;
			pawns = Owner.Map.mapPawns.AllPawnsSpawned.Where((Pawn x) => x.def == Props.mechanoidToKill && x.Faction != null && x.Faction == Owner.Faction).ToList();
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (Owner?.Faction == Faction.OfPlayer)
		{
			Command_Action allDead = new Command_Action
			{
				defaultLabel = Props.toggleLabelKey.Translate(),
				defaultDesc = Props.toggleDescKey.Translate(),
				icon = ContentFinder<Texture2D>.Get(Props.toggleIconPath),
				action = delegate
				{
					KillAll();
				}
			};
			if (!CanApply)
			{
				allDead.Disable(Props.unableKey.Translate());
			}
			yield return allDead;
		}
	}

	public void KillAll()
	{
		MoteMaker.ThrowText(Owner.DrawPos, Owner.Map, "NCL.ClearAllSplitterSpiders".Translate($"{Owner.Name}"), 5f);
		foreach (Pawn pawn in pawns)
		{
			if (pawn != null && !pawn.Destroyed)
			{
				pawn.Destroy();
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ticks, "ticks", 0);
	}
}
