using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class Comp_ApparelHediffAdder : ThingComp
{
	private Gizmo_ApparelReloadableExtra gizmo_ApparelReloadableExtra;

	private CompApparelReloadable CompApparelReloadableSaved;

	private Pawn PawnSaved;

	private CompProperties_ApparelHediffAdder Props => (CompProperties_ApparelHediffAdder)props;

	public CompApparelReloadable CompApparelReloadable
	{
		get
		{
			if (CompApparelReloadableSaved == null)
			{
				CompApparelReloadableSaved = parent.TryGetComp<CompApparelReloadable>();
			}
			return CompApparelReloadableSaved;
		}
	}

	private Pawn Wearer
	{
		get
		{
			if (PawnSaved == null)
			{
				Apparel apparel = parent as Apparel;
				PawnSaved = apparel.Wearer;
			}
			return PawnSaved;
		}
	}

	public override void CompDrawWornExtras()
	{
		base.CompDrawWornExtras();
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetWornGizmosExtra())
		{
			yield return item;
		}
		foreach (Gizmo item2 in GetGizmo())
		{
			yield return item2;
		}
	}

	private IEnumerable<Gizmo> GetGizmo()
	{
		if (Wearer == null || !Wearer.IsPlayerControlled || !Wearer.Drafted)
		{
			yield break;
		}
		if (Find.Selector.SingleSelectedThing == Wearer)
		{
			if (gizmo_ApparelReloadableExtra == null)
			{
				gizmo_ApparelReloadableExtra = new Gizmo_ApparelReloadableExtra(this);
			}
			yield return gizmo_ApparelReloadableExtra;
		}
		if (CompApparelReloadable.RemainingCharges <= 0)
		{
			yield break;
		}
		yield return new Command_Action
		{
			defaultLabel = Props.Label.Translate(),
			icon = new CachedTexture(Props.UIPath).Texture,
			action = delegate
			{
				HediffDef named = DefDatabase<HediffDef>.GetNamed(Props.HediffName);
				if (named != null && Wearer != null)
				{
					if (!Wearer.health.hediffSet.TryGetHediff(named, out var hediff))
					{
						Hediff hediff2 = HediffMaker.MakeHediff(named, Wearer);
						hediff2.TryGetComp<HediffComp_Disappears>().ticksToDisappear = Props.HediffTickToDisappear;
						Wearer.health.AddHediff(hediff2);
					}
					else
					{
						hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear += Props.HediffTickToDisappear;
					}
					CompApparelReloadable?.UsedOnce();
				}
			}
		};
	}
}
