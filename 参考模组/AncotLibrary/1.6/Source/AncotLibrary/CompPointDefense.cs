using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class CompPointDefense : ThingComp
{
	private Texture2D GizmoIcon;

	public bool switchOn = false;

	public bool showGizmo = false;

	private CompProperties_PointDefense Props => (CompProperties_PointDefense)props;

	public CompApparelReloadable_Custom CompReloadable => parent.TryGetComp<CompApparelReloadable_Custom>();

	public CompThingCarrier_Custom CompThingCarrier => parent.TryGetComp<CompThingCarrier_Custom>();

	public int RemainingCharges
	{
		get
		{
			if (CompReloadable != null)
			{
				return CompReloadable.RemainingCharges;
			}
			if (CompThingCarrier != null)
			{
				return CompThingCarrier.IngredientCount;
			}
			return 0;
		}
	}

	protected Thing OwnerThing
	{
		get
		{
			if (parent is Apparel apparel)
			{
				return apparel.Wearer;
			}
			if (parent is Pawn result)
			{
				return result;
			}
			return parent;
		}
	}

	private bool IsApparel => parent is Apparel;

	private bool IsBuiltIn => !IsApparel;

	public override void CompTickInterval(int delta)
	{
		if (Props.ai_AlwaysSwitchOn && !switchOn)
		{
			switchOn = true;
		}
		if (OwnerThing != null && OwnerThing.Spawned && switchOn && (Props.consumeChargeAmount == 0 || RemainingCharges >= Props.consumeChargeAmount))
		{
			InterceptProjectile(Props.range);
		}
	}

	public void InterceptProjectile(float range)
	{
		List<Projectile> list = new List<Projectile>();
		foreach (Projectile item in OwnerThing.Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile))
		{
			if (item != null && item.Position.InHorDistOf(OwnerThing.Position, range))
			{
				list.Add(item);
			}
		}
		if (list.NullOrEmpty())
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Projectile projectile2 = list[i];
			if (projectile2 == null || !((float)projectile2.DamageAmount > Props.damageThreshold) || (projectile2.Launcher.Faction != null && !projectile2.Launcher.Faction.HostileTo(OwnerThing.Faction)))
			{
				continue;
			}
			MethodInfo method = projectile2.GetType().GetMethod("Impact", BindingFlags.Instance | BindingFlags.NonPublic);
			if (method != null)
			{
				if (Props.defenseEffecter != null)
				{
					Effecter effecter = Props.defenseEffecter.Spawn();
					effecter.Trigger(OwnerThing, projectile2);
					effecter.Cleanup();
				}
				object[] parameters = new object[2] { null, true };
				method.Invoke(projectile2, parameters);
			}
			if (projectile2.Spawned)
			{
				projectile2.Destroy();
			}
			if (Props.consumeChargeAmount != 0)
			{
				parent.TryGetComp<CompApparelReloadable_Custom>()?.UsedOnce(Props.consumeChargeAmount);
				parent.TryGetComp<CompThingCarrier_Custom>()?.TryRemoveThingInCarrier(Props.consumeChargeAmount);
			}
		}
	}

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
		if (!IsBuiltIn)
		{
			Log.Message("IsBuiltIn");
			yield break;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			Log.Message("CompGetGizmosExtra");
			yield return gizmo;
		}
	}

	private IEnumerable<Gizmo> GetGizmos()
	{
		if (OwnerThing.Faction == Faction.OfPlayer && (Props.alwaysShowGizmo || showGizmo))
		{
			if ((object)GizmoIcon == null)
			{
				GizmoIcon = ContentFinder<Texture2D>.Get(Props.iconPath);
			}
			yield return new Command_ToggleShowRange
			{
				defaultLabel = Props.gizmoLabel,
				defaultDesc = Props.gizmoDesc,
				Position = OwnerThing.Position,
				range = Props.range,
				isActive = () => switchOn,
				icon = GizmoIcon,
				toggleAction = delegate
				{
					switchOn = !switchOn;
				}
			};
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref switchOn, "switchOn", defaultValue: false);
		Scribe_Values.Look(ref showGizmo, "showGizmo", defaultValue: false);
	}
}
