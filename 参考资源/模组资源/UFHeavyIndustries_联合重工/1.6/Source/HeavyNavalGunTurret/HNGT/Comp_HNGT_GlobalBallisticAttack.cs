using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HNGT;

public class Comp_HNGT_GlobalBallisticAttack : ThingComp
{
	private int coolDowntime;

	private Texture2D cachedIcon;

	private TargetingParameters targetingParameters;

	public CompProperties_HNGT_GlobalBallisticAttack Props => (CompProperties_HNGT_GlobalBallisticAttack)props;

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		targetingParameters = new TargetingParameters
		{
			canTargetLocations = true,
			canTargetBuildings = true,
			canTargetPawns = true
		};
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			cachedIcon = ContentFinder<Texture2D>.Get(Props.iconPath);
		});
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref coolDowntime, "HNGT_orbitalCooldown", 0);
	}

	public override void CompTickInterval(int delta)
	{
		base.CompTickInterval(delta);
		if (coolDowntime > 0)
		{
			coolDowntime -= delta;
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (!((Building)parent).TryGetComp<CompPowerTrader>().PowerOn || parent.Map.IsPocketMap)
		{
			yield break;
		}
		yield return new Command_ActionWithCooldown
		{
			icon = cachedIcon,
			defaultLabel = "HNGT_InterMapBombardment_Label".Translate(),
			defaultDesc = "HNGT_InterMapBombardment_Desc".Translate(),
			cooldownPercentGetter = () => Mathf.InverseLerp(Props.cooldownSeconds * 60, 0f, coolDowntime),
			action = delegate
			{
				if (string.IsNullOrEmpty(Props.worldObjectDefName))
				{
					Log.Error("HNGT: " + parent.def.defName + " CompProperties lack 'worldObjectDefName'.");
				}
				else if (string.IsNullOrEmpty(Props.payloadThingDefName))
				{
					Log.Error("HNGT: " + parent.def.defName + " 的 CompProperties lack 'payloadThingDefName'.");
				}
				else
				{
					Building_TurretGunRotateAim parentTurret = parent as Building_TurretGunRotateAim;
					if (parentTurret == null)
					{
						Log.Error("HNGT: Comp_HNGT_GlobalBallisticAttack's parent is not Building_TurretGunRotateAim.");
					}
					else if (parentTurret.isFiringInterMap)
					{
						Messages.Message("HNGT_TurretAlreadyFiring".Translate(), MessageTypeDefOf.RejectInput, historical: false);
					}
					else if (coolDowntime > 0)
					{
						Messages.Message("HNGT_CooldownLeft".Translate(coolDowntime / 60), MessageTypeDefOf.RejectInput, historical: false);
					}
					else if (Find.Targeter.IsTargeting)
					{
						Messages.Message("HNGT_TargeterBusy".Translate(), MessageTypeDefOf.RejectInput, historical: false);
					}
					else
					{
						List<FloatMenuOption> list = new List<FloatMenuOption>();
						foreach (Map map in Find.Maps)
						{
							if (!map.IsPocketMap && map != parent.Map)
							{
								list.Add(new FloatMenuOption(map.info.parent.Label, delegate
								{
									Current.Game.CurrentMap = map;
									Find.Targeter.BeginTargeting(targetingParameters, delegate(LocalTargetInfo targetinfo)
									{
										coolDowntime = Props.cooldownSeconds * 60;
										WorldObjectDef named = DefDatabase<WorldObjectDef>.GetNamed(Props.worldObjectDefName, errorOnFail: false);
										if (named == null)
										{
											Log.Error("HNGT: Can not find '" + Props.worldObjectDefName + "' WorldObjectDef.");
										}
										else
										{
											int burstRounds = named.GetModExtension<DefModExtension_GlobalAttackDeviceParams>()?.fakeBurstRounds ?? 3;
											parentTurret.StartInterMapFire(map.Tile, map, targetinfo.Cell, Props.worldObjectDefName, Props.payloadThingDefName, burstRounds);
										}
									}, delegate(LocalTargetInfo targetInfo)
									{
										if (!map.fogGrid.IsFogged(targetInfo.Cell))
										{
											GenDraw.DrawTargetHighlight(targetInfo);
											float radius = 15f;
											ThingDef named = DefDatabase<ThingDef>.GetNamed(Props.payloadThingDefName, errorOnFail: false);
											if (named != null)
											{
												ModExtension_HighOrbitAttack modExtension = named.GetModExtension<ModExtension_HighOrbitAttack>();
												if (modExtension != null)
												{
													radius = modExtension.impactAreaRadius;
												}
											}
											GenDraw.DrawRadiusRing(targetInfo.Cell, radius);
										}
									}, delegate(LocalTargetInfo targetInfo)
									{
										if (targetInfo.IsValid && targetInfo.Cell.InBounds(map) && !map.fogGrid.IsFogged(targetInfo.Cell))
										{
											return true;
										}
										Messages.Message("HNGT_LocationInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
										return false;
									});
								}));
							}
						}
						if (list.Count <= 0)
						{
							list.Add(new FloatMenuOption("HNGT_NoOtherMap".Translate(), null, MenuOptionPriority.DisabledOption));
							Find.WindowStack.Add(new FloatMenu(list));
						}
						else
						{
							Find.WindowStack.Add(new FloatMenu(list));
						}
					}
				}
			}
		};
	}
}
