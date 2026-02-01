using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace NCL;

public class CompMechanoidShield : ThingComp
{
	private static readonly FieldInfo deactivatedField = typeof(CompMechanoid).GetField("deactivated", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo activeField = typeof(CompMechanoid).GetField("active", BindingFlags.Instance | BindingFlags.NonPublic);

	private CompProperties_MechanoidShield Props => (CompProperties_MechanoidShield)props;

	public override void CompTick()
	{
		base.CompTick();
		if (!(parent is Pawn { Spawned: not false } pawn) || !pawn.HostileTo(Faction.OfPlayer) || Find.TickManager.TicksGame % Props.checkIntervalTicks != 0)
		{
			return;
		}
		CompMechanoid mechanoidComp = pawn.TryGetComp<CompMechanoid>();
		if (mechanoidComp != null)
		{
			if (IsDeactivated(mechanoidComp))
			{
				ReactivateMech(pawn, mechanoidComp);
			}
			if (pawn.Downed)
			{
				UndownMech(pawn);
			}
		}
	}

	private void ReactivateMech(Pawn pawn, CompMechanoid mechanoidComp)
	{
		SetActivationState(mechanoidComp, active: true);
		if (pawn.CurJobDef == JobDefOf.Deactivated)
		{
			pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
		}
		string message = Props.reactivateMessageKey.Translate(pawn.Named("PAWN"));
		if (!message.NullOrEmpty())
		{
			Messages.Message(message, pawn, MessageTypeDefOf.PositiveEvent);
		}
	}

	private static void UndownMech(Pawn pawn)
	{
		try
		{
			pawn.health.Notify_Resurrected();
			pawn.health.forceDowned = false;
			pawn.health.hediffSet.hediffs.Where((Hediff h) => h.def.lethalSeverity > 0f).ToList().ForEach(delegate(Hediff h)
			{
				pawn.health.RemoveHediff(h);
			});
			pawn.jobs?.StopAll();
			pawn.jobs?.StartJob(new Job(JobDefOf.Goto, pawn.Position), JobCondition.InterruptForced);
			PortraitsCache.SetDirty(pawn);
		}
		catch (Exception arg)
		{
			Log.Error($"Failed to undown mechanoid {pawn.Label}: {arg}");
		}
	}

	private static bool IsDeactivated(CompMechanoid comp)
	{
		return comp != null && (bool)(deactivatedField?.GetValue(comp) ?? ((object)false));
	}

	private static void SetActivationState(CompMechanoid comp, bool active)
	{
		if (comp != null)
		{
			deactivatedField?.SetValue(comp, !active);
			activeField?.SetValue(comp, active);
			comp.WakeUp();
		}
	}
}
