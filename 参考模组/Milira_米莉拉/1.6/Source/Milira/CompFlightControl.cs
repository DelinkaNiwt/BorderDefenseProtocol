using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Milira;

public class CompFlightControl : ThingComp
{
	private bool switchOn = true;

	private bool onlyForMove = false;

	private CompProperties_FlightControl Props => (CompProperties_FlightControl)props;

	private Pawn Pawn => parent as Pawn;

	public bool CanFly => switchOn && (Props.hungerPctThresholdCanFly <= 0f || Pawn.needs.food == null || Pawn.needs.food.CurLevelPercentage >= Props.hungerPctThresholdCanFly) && (!onlyForMove || Moving) && HasBodyPart;

	private bool Flying => Pawn.Flying;

	private bool Moving => Pawn.Spawned && Pawn.pather.Moving;

	private bool HasBodyPart => Pawn.health.hediffSet.GetNotMissingParts().Contains(Pawn.RaceProps.body.AllParts.FirstOrDefault((BodyPartRecord bpr) => bpr.def == Props.bodyPart));

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (!respawningAfterLoad)
		{
			CheckAnimation(!Flying);
		}
	}

	public void CheckAnimation(bool flag)
	{
		if (flag)
		{
			Pawn.Drawer.renderer.SetAnimation(null);
		}
	}

	public override void CompTick()
	{
		if (CanFly && switchOn)
		{
			if (Flying && Pawn.needs.food != null && Pawn.IsHashIntervalTick(60))
			{
				Pawn.needs.food.CurLevelPercentage -= Props.hungerPctCostPerSecondFly;
			}
			if (!Flying && Pawn.CurJob != null)
			{
				Pawn.flight?.Notify_JobStarted(Pawn.CurJob);
			}
		}
		else if (Pawn.CurJob != null && Pawn.CurJob.flying)
		{
			Pawn.CurJob.flying = false;
			Pawn.flight?.ForceLand();
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (!Pawn.Drafted)
		{
			yield break;
		}
		string text = ((!switchOn) ? "Milira.SwitchFly_NeverDesc".Translate() : (onlyForMove ? "Milira.SwitchFly_OnlyForMoveDesc".Translate() : "Milira.SwitchFly_AlwaysDesc".Translate()));
		yield return new Command_Action
		{
			defaultLabel = "Milira.SwitchFly".Translate(),
			defaultDesc = "Milira.SwitchFlyDesc".Translate(parent.LabelShort) + "\n\n" + text,
			icon = ((!switchOn) ? MiliraIcon.FlySwitch_Never : (onlyForMove ? MiliraIcon.FlySwitch_OnlyForMove : MiliraIcon.FlySwitch_Always)),
			action = delegate
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				FloatMenuOption item = new FloatMenuOption("Milira.SwitchFly_Never".Translate(), delegate
				{
					foreach (object selectedObject in Find.Selector.SelectedObjects)
					{
						if (selectedObject is Pawn pawn)
						{
							CompFlightControl compFlightControl = pawn.TryGetComp<CompFlightControl>();
							if (compFlightControl != null)
							{
								compFlightControl.switchOn = false;
								compFlightControl.onlyForMove = false;
								pawn.flight?.ForceLand();
							}
						}
					}
				}, MenuOptionPriority.Default, null, null, 29f);
				list.Add(item);
				FloatMenuOption item2 = new FloatMenuOption("Milira.SwitchFly_OnlyForMove".Translate(), delegate
				{
					foreach (object selectedObject2 in Find.Selector.SelectedObjects)
					{
						if (selectedObject2 is Pawn thing)
						{
							CompFlightControl compFlightControl = thing.TryGetComp<CompFlightControl>();
							if (compFlightControl != null)
							{
								compFlightControl.switchOn = true;
								compFlightControl.onlyForMove = true;
							}
						}
					}
				}, MenuOptionPriority.Default, null, null, 29f);
				list.Add(item2);
				FloatMenuOption item3 = new FloatMenuOption("Milira.SwitchFly_Always".Translate(), delegate
				{
					foreach (object selectedObject3 in Find.Selector.SelectedObjects)
					{
						if (selectedObject3 is Pawn pawn)
						{
							CompFlightControl compFlightControl = pawn.TryGetComp<CompFlightControl>();
							if (compFlightControl != null)
							{
								compFlightControl.switchOn = true;
								compFlightControl.onlyForMove = false;
								if (pawn.CurJob != null)
								{
									pawn.flight?.Notify_JobStarted(Pawn.CurJob);
								}
							}
						}
					}
				}, MenuOptionPriority.Default, null, null, 29f);
				list.Add(item3);
				Find.WindowStack.Add(new FloatMenu(list));
			}
		};
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref switchOn, "switchOn", defaultValue: false);
		Scribe_Values.Look(ref onlyForMove, "onlyForMove", defaultValue: false);
	}
}
