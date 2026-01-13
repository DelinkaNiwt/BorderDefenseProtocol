using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FreeElectronOrbitalLaser;

[StaticConstructorOnStartup]
public class Comp_FreeElectronOrbitalLaser : ThingComp
{
	private int coolDowntime;

	private TargetingParameters targetingParameters = new TargetingParameters
	{
		canTargetLocations = true,
		canTargetBuildings = true,
		canTargetPawns = true
	};

	private Color IconColor = Color.white;

	public CompProperties_FreeElectronOrbitalLaser Props => (CompProperties_FreeElectronOrbitalLaser)props;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref coolDowntime, "coolDowntime", 0);
	}

	private bool ResearchRequirementMet(out string reason)
	{
		if (Props.requiredResearch != null && !Props.requiredResearch.IsFinished)
		{
			if (!Props.disabledReasonKey.NullOrEmpty())
			{
				reason = Props.disabledReasonKey.Translate();
			}
			else
			{
				reason = "RequiresResearch".Translate() + ": " + Props.requiredResearch.LabelCap;
			}
			return false;
		}
		reason = null;
		return true;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		CompPowerTrader powerComp = parent.TryGetComp<CompPowerTrader>();
		if ((powerComp != null && !powerComp.PowerOn) || parent.Map.IsPocketMap)
		{
			yield break;
		}
		Command_ActionWithCooldown command = new Command_ActionWithCooldown
		{
			icon = TextureCache.iconBeam,
			defaultLabel = ("OrbitalStrikePowerBeam".CanTranslate() ? "OrbitalStrikePowerBeam".Translate().ToString() : "Orbital Power Beam"),
			defaultDesc = "OrbitalStrikePowerBeamDesc".Translate(),
			action = delegate
			{
				if (!ResearchRequirementMet(out var reason2))
				{
					Messages.Message(reason2, MessageTypeDefOf.RejectInput, historical: false);
				}
				else if (coolDowntime > 0)
				{
					Messages.Message("cooldownLeftSeconds".Translate(coolDowntime / 60), MessageTypeDefOf.RejectInput, historical: false);
				}
				else
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();
					foreach (Map map in Find.Maps.Where((Map m) => !m.IsPocketMap))
					{
						list.Add(new FloatMenuOption(map.info.parent.Label, delegate
						{
							if (Current.Game.CurrentMap != map)
							{
								Current.Game.CurrentMap = map;
							}
							Messages.Message("SelectBeamStartpoint".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
							Find.Targeter.BeginTargeting(targetingParameters, delegate(LocalTargetInfo startTarget)
							{
								Messages.Message("SelectBeamEndpoint".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
								Find.Targeter.BeginTargeting(targetingParameters, delegate(LocalTargetInfo endTarget)
								{
									Thing thing = ThingMaker.MakeThing(Props.lavaThingDef);
									MoltenFlowProcess moltenFlowProcess = thing as MoltenFlowProcess;
									if (moltenFlowProcess != null)
									{
										moltenFlowProcess.isLinkedToBeam = true;
										moltenFlowProcess.forceCoolDelay = Props.lavaCoolDelay.RandomInRange;
										moltenFlowProcess.expandIntervalTicks = Props.lavaExpandIntervalTicks;
										moltenFlowProcess.cellsToSpreadPerInterval = Props.lavaExpandCellsPerInterval;
										int lavaPoolSize = Props.lavaPoolSize;
										moltenFlowProcess.forcePoolSize = lavaPoolSize;
									}
									GenSpawn.Spawn(thing, startTarget.Cell, map);
									ThingDef def = Props.beamThingDef ?? ThingDefOf.PowerBeam;
									Thing thing2 = GenSpawn.Spawn(def, startTarget.Cell, map);
									if (thing2 is FreeElectronLaserBeam freeElectronLaserBeam)
									{
										freeElectronLaserBeam.instigator = parent;
										freeElectronLaserBeam.weaponDef = parent.def;
										freeElectronLaserBeam.from = startTarget.Cell;
										freeElectronLaserBeam.to = endTarget.Cell;
										float num = startTarget.Cell.DistanceTo(endTarget.Cell);
										freeElectronLaserBeam.duration = (int)(num * (float)Props.ticksPerCell);
										if (freeElectronLaserBeam.duration <= 0)
										{
											freeElectronLaserBeam.duration = Props.durationTicks;
										}
										freeElectronLaserBeam.lavaManager = moltenFlowProcess;
										freeElectronLaserBeam.StartStrike();
									}
									Messages.Message("OrbitalStrikeMessage".Translate(thing2.LabelCap, map.info.parent.Label), MessageTypeDefOf.PositiveEvent);
									coolDowntime = Props.cooldownSeconds * 60;
								}, delegate(LocalTargetInfo targetInfo)
								{
									if (targetInfo.IsValid && !map.fogGrid.IsFogged(targetInfo.Cell))
									{
										GenDraw.DrawTargetHighlight(targetInfo);
										GenDraw.DrawLineBetween(startTarget.CenterVector3, targetInfo.CenterVector3, SimpleColor.White);
									}
								}, (LocalTargetInfo targetInfo) => targetInfo.IsValid && targetInfo.Cell.InBounds(map) && !map.fogGrid.IsFogged(targetInfo.Cell));
							}, delegate(LocalTargetInfo targetInfo)
							{
								if (!map.fogGrid.IsFogged(targetInfo.Cell))
								{
									GenDraw.DrawTargetHighlight(targetInfo);
								}
							}, delegate(LocalTargetInfo targetInfo)
							{
								if (targetInfo.IsValid && targetInfo.Cell.InBounds(map) && !map.fogGrid.IsFogged(targetInfo.Cell))
								{
									if (targetInfo.IsValid && targetInfo.Cell.InBounds(map) && !map.fogGrid.IsFogged(targetInfo.Cell))
									{
										return true;
									}
									Messages.Message("LaserInvalidTarget".Translate(), MessageTypeDefOf.RejectInput, historical: false);
									return false;
								}
								if (targetInfo.IsValid && targetInfo.Cell.InBounds(map) && !map.fogGrid.IsFogged(targetInfo.Cell))
								{
									return true;
								}
								Messages.Message("LaserInvalidTarget".Translate(), MessageTypeDefOf.RejectInput, historical: false);
								return false;
							});
						}));
					}
					if (list.Count <= 0)
					{
						list.Add(new FloatMenuOption("OrbitalStrikeNoOtherMap".Translate(), null, MenuOptionPriority.DisabledOption));
					}
					Find.WindowStack.Add(new FloatMenu(list));
				}
			},
			defaultIconColor = IconColor
		};
		if (!ResearchRequirementMet(out var reason))
		{
			command.disabledReason = reason;
		}
		yield return command;
	}

	public override void CompTickInterval(int delta)
	{
		base.CompTickInterval(delta);
		if (coolDowntime > 0)
		{
			coolDowntime -= delta;
		}
	}
}
