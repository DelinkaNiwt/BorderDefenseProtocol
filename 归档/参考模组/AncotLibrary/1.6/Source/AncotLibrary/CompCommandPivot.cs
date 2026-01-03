using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompCommandPivot : ThingComp
{
	public bool sortie;

	private Texture2D GizmoIcon1;

	private Texture2D GizmoIcon2;

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

	public CompMechCarrier_Custom compMechCarrier_Custom => PawnOwner.TryGetComp<CompMechCarrier_Custom>();

	private List<Pawn> spawnedPawns => compMechCarrier_Custom.spawnedPawns;

	public CompProperties_CommandPivot Props => (CompProperties_CommandPivot)props;

	public string gizmoLabel1 => Props.gizmoLabel1.NullOrEmpty() ? ((string)"Ancot.FloatUnit_Follow".Translate()) : Props.gizmoLabel1;

	public string gizmoDesc1 => Props.gizmoDesc1.NullOrEmpty() ? ((string)"Ancot.FloatUnit_FollowDesc".Translate(PawnOwner.Name.ToStringShort)) : Props.gizmoDesc1;

	public string gizmoLabel2 => Props.gizmoLabel2.NullOrEmpty() ? ((string)"Ancot.FloatUnitSortie".Translate()) : Props.gizmoLabel2;

	public string gizmoDesc2 => Props.gizmoDesc2.NullOrEmpty() ? ((string)"Ancot.FloatUnitSortieDesc".Translate(PawnOwner.Name.ToStringShort)) : Props.gizmoDesc2;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref sortie, "sortie", defaultValue: false);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (!PawnOwner.Faction.IsPlayer)
		{
			yield break;
		}
		if (sortie)
		{
			if ((object)GizmoIcon2 == null)
			{
				GizmoIcon2 = ContentFinder<Texture2D>.Get(Props.gizmoIconPath2);
			}
		}
		else if ((object)GizmoIcon1 == null)
		{
			GizmoIcon1 = ContentFinder<Texture2D>.Get(Props.gizmoIconPath1);
		}
		yield return new Command_Action
		{
			Order = Props.gizmoOrder,
			defaultLabel = (sortie ? gizmoLabel2 : gizmoLabel1),
			defaultDesc = (sortie ? gizmoDesc2 : gizmoDesc1),
			icon = (sortie ? GizmoIcon2 : GizmoIcon1),
			action = delegate
			{
				sortie = !sortie;
				for (int i = 0; i < spawnedPawns.Count; i++)
				{
					CompCommandTerminal compCommandTerminal = spawnedPawns[i].TryGetComp<CompCommandTerminal>();
					if (compCommandTerminal != null)
					{
						compCommandTerminal.sortie_Terminal = sortie;
					}
				}
			}
		};
	}
}
