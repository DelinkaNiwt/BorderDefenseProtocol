using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class CompJobGizmo : ThingComp
{
	private Texture2D GizmoIcon;

	public CompProperties_JobGizmo Props => (CompProperties_JobGizmo)props;

	protected Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return null;
			}
			return wearer;
		}
	}

	public Apparel Apparel => parent as Apparel;

	public bool IsApparel => Apparel != null;

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetWornGizmosExtra())
		{
			yield return item;
		}
		if (!IsApparel)
		{
			yield break;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (IsApparel)
		{
			yield break;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	private IEnumerable<Gizmo> GetGizmos()
	{
		if (PawnOwner.Faction == Faction.OfPlayer && (Props.showGizmoUndrafted || PawnOwner.Drafted))
		{
			if ((object)GizmoIcon == null)
			{
				GizmoIcon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath);
			}
			yield return new Command_Action
			{
				defaultLabel = Props.gizmoLabel,
				defaultDesc = Props.gizmoDesc,
				icon = GizmoIcon,
				Order = Props.gizmoOrder,
				action = delegate
				{
					Job job = JobMaker.MakeJob(Props.jobDef, PawnOwner);
					PawnOwner.jobs.TryTakeOrderedJob(job, JobTag.Misc);
				}
			};
		}
	}
}
