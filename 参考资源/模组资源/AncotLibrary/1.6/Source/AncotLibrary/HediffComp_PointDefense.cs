using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class HediffComp_PointDefense : HediffComp_AISwitchCombat
{
	private HediffCompProperties_PointDefense Props => (HediffCompProperties_PointDefense)props;

	protected override string GizmoLabel => Props.gizmoLabel;

	protected override string GizmoDesc => Props.gizmoDesc;

	protected override string IconPath => Props.iconPath;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (switchOn)
		{
			parent.Severity = Mathf.Max(parent.Severity - Mathf.Min(Props.switchOnConsumeRate / 60f, parent.Severity), Props.availableSeverityThreshold);
			if (parent.Severity <= Props.availableSeverityThreshold)
			{
				switchOn = false;
			}
			InterceptProjectile(Props.range);
		}
		else
		{
			parent.Severity += Props.switchOffRestoreRate / 60f;
		}
	}

	public void InterceptProjectile(float range)
	{
		if (base.Pawn?.Map == null || !base.Pawn.Spawned)
		{
			return;
		}
		List<Thing> list = GenRadial.RadialDistinctThingsAround(base.Pawn.Position, base.Pawn.Map, range, useCenter: true).ToList();
		if (list.NullOrEmpty())
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (!(list[i] is Projectile projectile) || !((float)projectile.DamageAmount > Props.damageThreshold) || (projectile.Launcher.Faction != null && !projectile.Launcher.Faction.HostileTo(base.Pawn.Faction)))
			{
				continue;
			}
			MethodInfo method = projectile.GetType().GetMethod("Impact", BindingFlags.Instance | BindingFlags.NonPublic);
			if (method != null)
			{
				if (Props.defenseEffecter != null)
				{
					Effecter effecter = Props.defenseEffecter.Spawn();
					effecter.Trigger(base.Pawn, projectile);
					effecter.Cleanup();
				}
				object[] parameters = new object[2] { null, true };
				method.Invoke(projectile, parameters);
				parent.Severity = Mathf.Max(parent.Severity - Props.severityCostPerDefense, Props.availableSeverityThreshold);
			}
			if (projectile.Spawned)
			{
				projectile.Destroy();
			}
		}
	}
}
