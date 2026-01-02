using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AutoBlink;

public class HediffCompProperties_AutoBlink : HediffCompProperties
{
	public List<string> preBlinkSoundDefs = new List<string>();

	public List<string> postBlinkSoundDefs = new List<string>();

	public List<string> preBlinkMoteDefs = new List<string>();

	public List<string> preBlinkEffecterDefs = new List<string>();

	public List<string> postBlinkMoteDefs = new List<string>();

	public List<string> postBlinkEffecterDefs = new List<string>();

	public List<string> allowedJobDefs = new List<string>();

	public List<string> excludedJobDefs = new List<string>();

	public string gizmoLabel;

	public bool drawGizmo = true;

	public bool playerFactionOnly = true;

	public bool defaultAutoBlinkDrafted = true;

	public bool defaultAutoBlinkIdle = true;

	public bool defaultJumpAsFarAsPossible = true;

	public int delayAfterEligibleTicks = 0;

	public int cellsBeforeTarget;

	public int blinkIntervalTicks;

	public int minDistanceToBlink;

	public int maxDistanceToBlink;

	public int postBlinkStanceTicks = -1;

	public string gizmoIconPath;

	public HediffCompProperties_AutoBlink()
	{
		compClass = typeof(HediffComp_AutoBlink);
	}

	public CompProperties_AutoBlink ToThingCompProps()
	{
		CompProperties_AutoBlink compProperties_AutoBlink = new CompProperties_AutoBlink();
		compProperties_AutoBlink.preBlinkSoundDefs = preBlinkSoundDefs?.ToList();
		compProperties_AutoBlink.postBlinkSoundDefs = postBlinkSoundDefs?.ToList();
		compProperties_AutoBlink.preBlinkMoteDefs = preBlinkMoteDefs?.ToList();
		compProperties_AutoBlink.preBlinkEffecterDefs = preBlinkEffecterDefs?.ToList();
		compProperties_AutoBlink.postBlinkMoteDefs = postBlinkMoteDefs?.ToList();
		compProperties_AutoBlink.postBlinkEffecterDefs = postBlinkEffecterDefs?.ToList();
		compProperties_AutoBlink.allowedJobDefs = allowedJobDefs?.ToList();
		compProperties_AutoBlink.excludedJobDefs = excludedJobDefs?.ToList();
		compProperties_AutoBlink.gizmoLabel = gizmoLabel;
		compProperties_AutoBlink.drawGizmo = drawGizmo;
		compProperties_AutoBlink.playerFactionOnly = playerFactionOnly;
		compProperties_AutoBlink.defaultAutoBlinkDrafted = defaultAutoBlinkDrafted;
		compProperties_AutoBlink.defaultAutoBlinkIdle = defaultAutoBlinkIdle;
		compProperties_AutoBlink.defaultJumpAsFarAsPossible = defaultJumpAsFarAsPossible;
		compProperties_AutoBlink.delayAfterEligibleTicks = delayAfterEligibleTicks;
		compProperties_AutoBlink.cellsBeforeTarget = cellsBeforeTarget;
		compProperties_AutoBlink.blinkIntervalTicks = blinkIntervalTicks;
		compProperties_AutoBlink.minDistanceToBlink = minDistanceToBlink;
		compProperties_AutoBlink.maxDistanceToBlink = maxDistanceToBlink;
		compProperties_AutoBlink.postBlinkStanceTicks = postBlinkStanceTicks;
		compProperties_AutoBlink.gizmoIconPath = gizmoIconPath;
		compProperties_AutoBlink.InitCaches();
		return compProperties_AutoBlink;
	}
}
