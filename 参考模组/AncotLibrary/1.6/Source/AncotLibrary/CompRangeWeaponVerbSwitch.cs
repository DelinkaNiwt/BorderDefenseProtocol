using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompRangeWeaponVerbSwitch : ThingComp, IPawnWeaponGizmoProvider
{
	private Texture2D GizmoIcon1;

	private Texture2D GizmoIcon2;

	public bool switched = false;

	public string gizmoLabel1 => Props.gizmoLabel1.NullOrEmpty() ? ((string)"Ancot.SwitchVerb".Translate()) : Props.gizmoLabel1;

	public string gizmoDesc1 => Props.gizmoDesc1.NullOrEmpty() ? ((string)"Ancot.SwitchVerbDesc".Translate()) : Props.gizmoDesc1;

	public string gizmoLabel2 => Props.gizmoLabel2.NullOrEmpty() ? ((string)"Ancot.SwitchVerb".Translate()) : Props.gizmoLabel2;

	public string gizmoDesc2 => Props.gizmoDesc2.NullOrEmpty() ? ((string)"Ancot.SwitchVerbDesc".Translate()) : Props.gizmoDesc2;

	private CompProperties_RangeWeaponVerbSwitch Props => (CompProperties_RangeWeaponVerbSwitch)props;

	public CompEquippable compEquippable => parent.TryGetComp<CompEquippable>();

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref switched, "switched", defaultValue: false);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && switched)
		{
			compEquippable.PrimaryVerb.verbProps = Props.verbProps;
		}
	}

	public override void Notify_Equipped(Pawn pawn)
	{
		base.Notify_Equipped(pawn);
		if (Rand.Chance(Props.aiInitialSwitchChance.GetValueOrDefault()))
		{
			Rand.PushState();
			switched = true;
			compEquippable.PrimaryVerb.verbProps = Props.verbProps;
			VerbRefresh();
			Rand.PopState();
		}
	}

	public virtual IEnumerable<Gizmo> GetWeaponGizmos()
	{
		if (!IsEquippedByPawn())
		{
			yield break;
		}
		if (switched)
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
			defaultLabel = (switched ? gizmoLabel2 : gizmoLabel1),
			defaultDesc = (switched ? gizmoDesc2 : gizmoDesc1),
			icon = (switched ? GizmoIcon2 : GizmoIcon1),
			action = delegate
			{
				compEquippable.PrimaryVerb.Reset();
				switched = !switched;
				if (switched)
				{
					compEquippable.PrimaryVerb.verbProps = Props.verbProps;
					VerbRefresh();
				}
				else
				{
					compEquippable.PrimaryVerb.verbProps = parent.def.Verbs[0];
					VerbRefresh();
				}
				Pawn_EquipmentTracker pawn_EquipmentTracker = parent.ParentHolder as Pawn_EquipmentTracker;
				if (pawn_EquipmentTracker.pawn != null)
				{
					pawn_EquipmentTracker.pawn.jobs.StopAll();
				}
			}
		};
	}

	public void Notify_VerbSwitch(bool switchToSecond)
	{
		if (switched != switchToSecond)
		{
			compEquippable.PrimaryVerb.Reset();
			switched = switchToSecond;
			if (switched)
			{
				compEquippable.PrimaryVerb.verbProps = Props.verbProps;
				VerbRefresh();
			}
			else
			{
				compEquippable.PrimaryVerb.verbProps = parent.def.Verbs[0];
				VerbRefresh();
			}
			Pawn_EquipmentTracker pawn_EquipmentTracker = parent.ParentHolder as Pawn_EquipmentTracker;
			if (pawn_EquipmentTracker.pawn != null)
			{
				pawn_EquipmentTracker.pawn.jobs.StopAll();
			}
		}
	}

	private bool IsEquippedByPawn()
	{
		return parent.ParentHolder is Pawn_EquipmentTracker { pawn: not null } pawn_EquipmentTracker && pawn_EquipmentTracker.pawn.Faction == Faction.OfPlayer && (!Props.onlyShowGizmoDrafted || pawn_EquipmentTracker.pawn.Drafted);
	}

	public void VerbRefresh()
	{
		EquipmentUtility.VerbRefresh(compEquippable);
	}

	public string VerbSwitchInfo()
	{
		return string.Concat(string.Concat(string.Concat("Ancot.VerbSwitchInfoDesc".Translate() + "\n\n" + "Ancot.VerbSwitch_BurstShotCount".Translate() + ": ", Props.verbProps.burstShotCount.ToString(), "\n") + "Ancot.VerbSwitch_Range".Translate() + ": ", Mathf.RoundToInt(Props.verbProps.range).ToString(), "\n") + "Ancot.VerbSwitch_WarmupTime".Translate() + ": ", Props.verbProps.warmupTime.ToString(), " ") + "Ancot.Second".Translate();
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "Ancot.SwitchVerb".Translate(), gizmoLabel2, VerbSwitchInfo(), 5600);
	}
}
