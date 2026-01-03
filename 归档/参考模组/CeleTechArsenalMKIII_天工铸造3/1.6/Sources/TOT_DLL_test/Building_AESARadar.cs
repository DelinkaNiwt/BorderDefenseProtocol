using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_AESARadar : Building
{
	private static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/UI_Overlay_RadarTargeting");

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		Messages.Message("Message_AESASetUp".Translate(), MessageTypeDefOf.NeutralEvent);
		Building_CMCTurretMissile.HasAESARadar = true;
	}

	private static bool IsHashIntervalTick(Thing t, int interval)
	{
		return t.HashOffsetTicks() % interval == 0;
	}

	protected override void Tick()
	{
		if (IsHashIntervalTick(this, 60))
		{
			RunDetection();
		}
	}

	private void RunDetection()
	{
		List<Thing> list = base.Map.listerThings.ThingsOfDef(CMC_Def.CMC_SAML);
		foreach (ThingDef projectileDef in DefExtensions.ProjectileDefs)
		{
			using List<Thing>.Enumerator enumerator2 = base.Map.listerThings.ThingsOfDef(projectileDef).GetEnumerator();
			while (enumerator2.MoveNext())
			{
				bool flag = false;
				bool flag2 = false;
				Thing current2 = enumerator2.Current;
				Projectile projectile = current2 as Projectile;
				Thing launcher = projectile.Launcher;
				Building_CMCTurretGun_AAAS building_CMCTurretGun_AAAS = null;
				float num = 99999f;
				if (launcher != null && launcher.Faction != null && !launcher.Faction.AllyOrNeutralTo(base.Faction))
				{
					flag2 = true;
				}
				if (!flag2)
				{
					continue;
				}
				foreach (Thing item in list)
				{
					Building_CMCTurretGun_AAAS building_CMCTurretGun_AAAS2 = item as Building_CMCTurretGun_AAAS;
					if (building_CMCTurretGun_AAAS2.CurrentTarget == current2)
					{
						flag = true;
						break;
					}
					float magnitude = (building_CMCTurretGun_AAAS2.Position - current2.Position).Magnitude;
					if (building_CMCTurretGun_AAAS2.Active && building_CMCTurretGun_AAAS2.AttackVerb.state != VerbState.Bursting && building_CMCTurretGun_AAAS2.burstCooldownTicksLeft <= 0 && magnitude <= building_CMCTurretGun_AAAS2.AttackVerb.EffectiveRange)
					{
						if (building_CMCTurretGun_AAAS == null)
						{
							building_CMCTurretGun_AAAS = building_CMCTurretGun_AAAS2;
							num = magnitude;
						}
						else if (building_CMCTurretGun_AAAS != null && magnitude < num)
						{
							building_CMCTurretGun_AAAS = building_CMCTurretGun_AAAS2;
							num = magnitude;
						}
					}
				}
				if (building_CMCTurretGun_AAAS != null && !flag)
				{
					building_CMCTurretGun_AAAS.currentTargetInt = current2;
				}
			}
		}
	}

	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
	{
		Map map = base.Map;
		List<Thing> list = map.listerThings.ThingsOfDef(CMC_Def.CMC_CICAESA_Radar);
		if (list.Count <= 1)
		{
			Building_CMCTurretMissile.HasAESARadar = false;
		}
		List<Thing> list2 = map.listerThings.ThingsOfDef(CMC_Def.CMCML);
		if (list2.Count > 0)
		{
			Messages.Message("Message_Destroyed".Translate(), MessageTypeDefOf.NeutralEvent);
		}
		base.DeSpawn(mode);
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		List<Gizmo> list = new List<Gizmo>();
		list.AddRange(base.GetGizmos());
		if (base.Faction == Faction.OfPlayer && GameComponent_CeleTech.Instance.ASEA_observedMap == null)
		{
			list.Add(new Command_Action
			{
				defaultLabel = "CMC.ScanMap".Translate(),
				defaultDesc = "CMC.ScanMapDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get("UI/UI_ScanMap_AESA"),
				action = delegate
				{
					CameraJumper.TryShowWorld();
					Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, canTargetTiles: true, TargeterMouseAttachment);
				}
			});
		}
		if (base.Faction == Faction.OfPlayer && GameComponent_CeleTech.Instance.ASEA_observedMap != null)
		{
			if (GameComponent_CeleTech.Instance.ASEA_observedMap.Destroyed)
			{
				GameComponent_CeleTech.Instance.ASEA_observedMap = null;
			}
			list.Add(new Command_Action
			{
				defaultLabel = "CMC.ScanMapStop".Translate(),
				defaultDesc = ((GameComponent_CeleTech.Instance.ASEA_observedMap?.Label != null) ? "CMC.ScanMapStopDesc".Translate(GameComponent_CeleTech.Instance.ASEA_observedMap.Label) : "CMC.ScanMapStopDescDefault".Translate()),
				icon = ContentFinder<Texture2D>.Get("UI/UI_ScanMap_AESA_Stop"),
				action = delegate
				{
					GameComponent_CeleTech.Instance.ASEA_observedMap = null;
				}
			});
		}
		return list;
	}

	private bool ChoseWorldTarget(GlobalTargetInfo target)
	{
		bool result = false;
		PlanetTile tile = target.WorldObject.Tile;
		MapParent mapParent = Find.WorldObjects.MapParentAt(tile);
		if (mapParent != null && mapParent.Map == null && (mapParent.Faction == null || (mapParent.Faction != null && mapParent.Faction.HostileTo(Faction.OfPlayer))))
		{
			Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(tile, null);
			GameComponent_CeleTech.Instance.ASEA_observedMap = mapParent;
			Current.Game.CurrentMap = orGenerateMap;
			CameraJumper.TryJump(new GlobalTargetInfo(orGenerateMap.Center, orGenerateMap));
			result = true;
		}
		else
		{
			Messages.Message("CMC.ScanMapFailed".Translate(), MessageTypeDefOf.RejectInput);
		}
		if (mapParent.Map != null)
		{
			Map map = mapParent.Map;
			GameComponent_CeleTech.Instance.ASEA_observedMap = mapParent;
			Current.Game.CurrentMap = map;
			CameraJumper.TryJump(new GlobalTargetInfo(map.Center, map));
			result = true;
		}
		return result;
	}

	public override void ExposeData()
	{
		base.ExposeData();
	}

	public override string GetInspectString()
	{
		string inspectString = base.GetInspectString();
		if (GameComponent_CeleTech.Instance.ASEA_observedMap != null)
		{
			return inspectString + "CMC_Targeting".Translate(GameComponent_CeleTech.Instance.ASEA_observedMap.Label);
		}
		return inspectString + "CMC_TargetingDefault".Translate();
	}
}
