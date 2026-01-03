using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

[StaticConstructorOnStartup]
public class CompThingContainer_Milian : CompThingContainer
{
	public int staySec = 0;

	public int hitPointMax = 0;

	public static readonly Texture2D CancelLoadCommandTex = ContentFinder<Texture2D>.Get("AncotLibrary/Gizmos/DropPawn");

	public Pawn innerPawn => base.ContainedThing as Pawn;

	private CompMechAutoFight compMechAutoFight => ((Thing)innerPawn).TryGetComp<CompMechAutoFight>();

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		staySec = 20;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (!parent.Faction.IsPlayer || compMechAutoFight.AutoFight)
		{
			if (parent.IsHashIntervalTick(60))
			{
				staySec--;
			}
			if (staySec <= 0)
			{
				CancelLoad();
			}
		}
		if (innerPawn != null && innerPawn.needs.energy != null && innerPawn.needs.energy.IsLowEnergySelfShutdown)
		{
			CancelLoad();
		}
	}

	public void CancelLoad()
	{
		CompThingCarrier_Custom val = ((Thing)innerPawn).TryGetComp<CompThingCarrier_Custom>();
		Thing thing = ThingMaker.MakeThing(val.fixedIngredient);
		int hitPoints = parent.HitPoints;
		if (parent.HitPoints > hitPointMax)
		{
			hitPoints = hitPointMax;
		}
		thing.stackCount = hitPoints;
		val.innerContainer.TryAdd(thing, hitPoints);
		innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
		parent.Destroy();
		if (innerPawn != null && innerPawn.Faction.IsPlayer)
		{
			innerPawn.drafter.Drafted = true;
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (parent != null && parent.Faction != null && parent.Faction.IsPlayer)
		{
			yield return new Command_Action
			{
				defaultLabel = "Milira_ExitFortress".Translate(),
				defaultDesc = "Milira_ExitFortressDesc".Translate(),
				icon = CancelLoadCommandTex,
				action = delegate
				{
					CancelLoad();
				}
			};
			CompThingCarrier_Custom compThingCarrier = base.ContainedThing.TryGetComp<CompThingCarrier_Custom>();
			if (Find.Selector.SingleSelectedThing == parent && compThingCarrier != null)
			{
				yield return (Gizmo)new ThingCarrierGizmo(compThingCarrier);
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref staySec, "staySec", 0);
		Scribe_Values.Look(ref hitPointMax, "hitPointMax", 0);
	}
}
