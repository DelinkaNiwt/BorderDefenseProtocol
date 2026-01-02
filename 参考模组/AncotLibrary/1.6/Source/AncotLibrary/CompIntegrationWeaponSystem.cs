using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompIntegrationWeaponSystem : CompApparelReloadable_Custom
{
	public bool activate = false;

	private Texture2D GizmoIcon;

	public CompProperties_IntegrationWeaponSystem Props_IWS => (CompProperties_IntegrationWeaponSystem)props;

	public CompPointDefense CompPointDefense => parent.TryGetComp<CompPointDefense>();

	public override void CompTick()
	{
		if (base.PawnOwner == null)
		{
			return;
		}
		if (activate && Props_IWS.ticksConsumeChargeOnceWhenUse > 0 && parent.IsHashIntervalTick(Props_IWS.ticksConsumeChargeOnceWhenUse))
		{
			UsedOnce();
			if (base.RemainingCharges == 0)
			{
				activate = false;
			}
		}
		if (!parent.IsHashIntervalTick(Props_IWS.ai_ActivateSystemTicksInterval) || base.PawnOwner.Faction.IsPlayer)
		{
			return;
		}
		if (!activate && base.PawnOwner.mindState.enemyTarget != null && remainingCharges > 0)
		{
			activate = true;
			base.PawnOwner.Drawer.renderer.renderTree.SetDirty();
			SwitchGizmo(activate);
			if (activate && Props_IWS.activateEffect != null)
			{
				Effecter effecter = Props_IWS.activateEffect.Spawn();
				effecter.Trigger(new TargetInfo(base.PawnOwner.Position, base.PawnOwner.Map), null);
				effecter.Cleanup();
			}
		}
		else if (base.PawnOwner.mindState.enemyTarget == null)
		{
			DeactivateSystem();
		}
	}

	public override void UsedOnce()
	{
		base.UsedOnce();
		if (remainingCharges == 0)
		{
			DeactivateSystem();
		}
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetWornGizmosExtra())
		{
			yield return item;
		}
		if (base.PawnOwner.Faction != Faction.OfPlayer)
		{
			yield break;
		}
		if (Props_IWS.gizmoIconPath != null && (object)GizmoIcon == null)
		{
			GizmoIcon = ContentFinder<Texture2D>.Get(Props_IWS.gizmoIconPath);
		}
		Command_Toggle command_Toggle = new Command_Toggle
		{
			defaultLabel = Props_IWS.gizmoLable,
			defaultDesc = Props_IWS.gizmoDesc,
			isActive = () => activate,
			icon = ((Props_IWS.gizmoIconPath != null) ? GizmoIcon : AncotLibraryIcon.Spanner),
			toggleAction = delegate
			{
				activate = !activate;
				base.PawnOwner.Drawer.renderer.renderTree.SetDirty();
				SwitchGizmo(activate);
				if (activate && Props_IWS.activateEffect != null)
				{
					Effecter effecter = Props_IWS.activateEffect.Spawn();
					effecter.Trigger(new TargetInfo(base.PawnOwner.Position, base.PawnOwner.Map), null);
					effecter.Cleanup();
				}
			}
		};
		if (remainingCharges == 0)
		{
			command_Toggle.Disable("Ancot.Ability_ChargeLow".Translate());
		}
		yield return command_Toggle;
	}

	public void DeactivateSystem()
	{
		activate = false;
		base.PawnOwner.Drawer.renderer.renderTree.SetDirty();
		SwitchGizmo(activate);
	}

	public void SwitchGizmo(bool flag)
	{
		if (CompPointDefense != null)
		{
			CompPointDefense.switchOn = flag;
			CompPointDefense.showGizmo = flag;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref activate, "activate", defaultValue: false);
	}
}
