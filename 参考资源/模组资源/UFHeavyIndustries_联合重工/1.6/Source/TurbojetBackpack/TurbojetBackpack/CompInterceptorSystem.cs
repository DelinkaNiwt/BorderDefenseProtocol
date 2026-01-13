using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TurbojetBackpack;

public class CompInterceptorSystem : ThingComp
{
	private bool isActive = true;

	private List<Thing> engagedTargets = new List<Thing>();

	private Command_Toggle cachedToggleCmd;

	public CompProperties_InterceptorSystem Props => (CompProperties_InterceptorSystem)props;

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		Pawn p = GetWearer();
		if (p == null || !p.IsColonistPlayerControlled || !p.Drafted)
		{
			yield break;
		}
		if (cachedToggleCmd == null)
		{
			cachedToggleCmd = new Command_Toggle();
			cachedToggleCmd.defaultLabel = "Turbojet_ADS_Label".Translate();
			cachedToggleCmd.defaultDesc = "Turbojet_ADS_Desc".Translate();
			cachedToggleCmd.icon = ((!Props.uiIconPath.NullOrEmpty()) ? ContentFinder<Texture2D>.Get(Props.uiIconPath) : TexCommand.ToggleVent);
			cachedToggleCmd.isActive = () => isActive;
			cachedToggleCmd.toggleAction = delegate
			{
				isActive = !isActive;
			};
		}
		yield return cachedToggleCmd;
	}

	public override void CompTick()
	{
		base.CompTick();
		Pawn wearer = GetWearer();
		if (wearer != null && wearer.Spawned && wearer.Map != null && !wearer.Downed)
		{
			engagedTargets.RemoveAll((Thing t) => t == null || t.Destroyed || !t.Spawned);
			if (isActive && wearer.Drafted && wearer.IsHashIntervalTick(Props.checkInterval))
			{
				TryFindAndIntercept(wearer);
			}
		}
	}

	private Pawn GetWearer()
	{
		if (parent is Pawn result)
		{
			return result;
		}
		if (!(parent is Apparel { Wearer: var wearer }))
		{
			return null;
		}
		return wearer;
	}

	private void TryFindAndIntercept(Pawn pawn)
	{
		Map map = pawn.Map;
		List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
		List<Projectile> list2 = new List<Projectile>();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is Projectile { Destroyed: false } projectile && !engagedTargets.Contains(projectile) && projectile.Launcher != null && projectile.Launcher.HostileTo(pawn) && projectile.def.projectile.flyOverhead && projectile.Position.InBounds(map) && !map.roofGrid.Roofed(projectile.Position))
			{
				list2.Add(projectile);
			}
		}
		if (list2.Count <= 0)
		{
			return;
		}
		if (Props.launchSound != null)
		{
			Props.launchSound.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
		}
		foreach (Projectile item in list2)
		{
			LaunchInterceptor(pawn, item);
			engagedTargets.Add(item);
		}
	}

	private void LaunchInterceptor(Pawn launcher, Projectile target)
	{
		if (Props.interceptorProjectileDef != null)
		{
			Vector3 drawPos = launcher.DrawPos;
			for (int i = 0; i < Props.burstCount; i++)
			{
				Projectile_ActiveInterceptor projectile_ActiveInterceptor = (Projectile_ActiveInterceptor)GenSpawn.Spawn(Props.interceptorProjectileDef, launcher.Position, launcher.Map);
				projectile_ActiveInterceptor.Launch(launcher, drawPos, new LocalTargetInfo(target), new LocalTargetInfo(target), ProjectileHitFlags.IntendedTarget);
				projectile_ActiveInterceptor.SetInterceptTarget(target);
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref isActive, "isActive", defaultValue: true);
		Scribe_Collections.Look(ref engagedTargets, "engagedTargets", LookMode.Reference);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && engagedTargets == null)
		{
			engagedTargets = new List<Thing>();
		}
	}
}
