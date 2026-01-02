using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompMechAutoFight : ThingComp
{
	private Texture2D GizmoIcon;

	private bool autoFight;

	public CompProperties_MechAutoFight Props => (CompProperties_MechAutoFight)props;

	public string gizmoLabel => Props.gizmoLabel.NullOrEmpty() ? ((string)"Ancot.AutoFight".Translate()) : Props.gizmoLabel;

	public string gizmoDesc => Props.gizmoDesc.NullOrEmpty() ? ((string)"Ancot.AutoFightDesc".Translate()) : Props.gizmoDesc;

	public bool AutoFight
	{
		get
		{
			return autoFight;
		}
		set
		{
			autoFight = value;
			if (parent is Pawn item)
			{
				if (autoFight)
				{
					Alert_MechAutoFight.Targets.Add(item);
				}
				else
				{
					Alert_MechAutoFight.Targets.Remove(item);
				}
			}
		}
	}

	public bool CanAutoFight
	{
		get
		{
			if (Props.requireResearch != null)
			{
				return Props.requireResearch.IsFinished;
			}
			return true;
		}
	}

	protected Pawn PawnOwner
	{
		get
		{
			if (parent is Pawn result)
			{
				return result;
			}
			return null;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref autoFight, "autoFight", defaultValue: false);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && autoFight && parent is Pawn item)
		{
			Alert_MechAutoFight.Targets.Add(item);
		}
	}

	public override void CompTickRare()
	{
		if (AutoFight && PawnOwner.Spawned && !PawnOwner.Drafted && !PawnOwner.Dead && PawnOwner.needs.energy != null)
		{
			PawnOwner.needs.energy.CurLevel -= 0.1041667f;
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (PawnOwner == null || PawnOwner.Faction != Faction.OfPlayer || PawnOwner.Dead || !CanAutoFight)
		{
			yield break;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
	{
		base.PostDeSpawn(map);
		if (PawnOwner.Dead)
		{
			AutoFight = false;
		}
	}

	private IEnumerable<Gizmo> GetGizmos()
	{
		if ((object)GizmoIcon == null)
		{
			GizmoIcon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath);
		}
		yield return new Command_Toggle
		{
			Order = Props.gizmoOrder,
			defaultLabel = gizmoLabel,
			defaultDesc = gizmoDesc,
			icon = GizmoIcon,
			toggleAction = delegate
			{
				AutoFight = !AutoFight;
				if (AutoFight)
				{
					PawnOwner.health.AddHediff(Props.hediffDef);
				}
				else
				{
					Hediff firstHediffOfDef = PawnOwner.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
					PawnOwner.health.RemoveHediff(firstHediffOfDef);
				}
			},
			isActive = () => AutoFight
		};
	}
}
