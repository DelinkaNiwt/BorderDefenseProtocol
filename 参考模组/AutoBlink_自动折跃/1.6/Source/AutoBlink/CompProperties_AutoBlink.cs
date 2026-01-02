using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AutoBlink;

public class CompProperties_AutoBlink : CompProperties
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

	[Unsaved(false)]
	public List<SoundDef> preSoundsCached;

	[Unsaved(false)]
	public List<SoundDef> postSoundsCached;

	[Unsaved(false)]
	public List<ThingDef> preMotesCached;

	[Unsaved(false)]
	public List<EffecterDef> preEffectsCached;

	[Unsaved(false)]
	public List<ThingDef> postMotesCached;

	[Unsaved(false)]
	public List<EffecterDef> postEffectsCached;

	[Unsaved(false)]
	public HashSet<JobDef> allowedJobDefsCached;

	[Unsaved(false)]
	public HashSet<JobDef> excludedJobDefsCached;

	public CompProperties_AutoBlink()
	{
		compClass = typeof(CompAutoBlink);
	}

	public void InitCaches()
	{
		List<string> list = preBlinkSoundDefs;
		preSoundsCached = ((list != null && list.Count > 0) ? (from d in preBlinkSoundDefs.Select(SoundDef.Named)
			where d != null
			select d).ToList() : null);
		List<string> list2 = postBlinkSoundDefs;
		postSoundsCached = ((list2 != null && list2.Count > 0) ? (from d in postBlinkSoundDefs.Select(SoundDef.Named)
			where d != null
			select d).ToList() : null);
		List<string> list3 = preBlinkMoteDefs;
		preMotesCached = ((list3 != null && list3.Count > 0) ? (from d in preBlinkMoteDefs.Select(ThingDef.Named)
			where d != null
			select d).ToList() : null);
		List<string> list4 = postBlinkMoteDefs;
		postMotesCached = ((list4 != null && list4.Count > 0) ? (from d in postBlinkMoteDefs.Select(ThingDef.Named)
			where d != null
			select d).ToList() : null);
		List<string> list5 = preBlinkEffecterDefs;
		preEffectsCached = ((list5 != null && list5.Count > 0) ? (from name in preBlinkEffecterDefs
			select DefDatabase<EffecterDef>.GetNamedSilentFail(name) into d
			where d != null
			select d).ToList() : null);
		List<string> list6 = postBlinkEffecterDefs;
		postEffectsCached = ((list6 != null && list6.Count > 0) ? (from name in postBlinkEffecterDefs
			select DefDatabase<EffecterDef>.GetNamedSilentFail(name) into d
			where d != null
			select d).ToList() : null);
		List<string> list7 = allowedJobDefs;
		allowedJobDefsCached = ((list7 != null && list7.Count > 0) ? new HashSet<JobDef>(from name in allowedJobDefs
			select DefDatabase<JobDef>.GetNamedSilentFail(name) into d
			where d != null
			select d) : null);
		List<string> list8 = excludedJobDefs;
		excludedJobDefsCached = ((list8 != null && list8.Count > 0) ? new HashSet<JobDef>(from name in excludedJobDefs
			select DefDatabase<JobDef>.GetNamedSilentFail(name) into d
			where d != null
			select d) : new HashSet<JobDef>());
	}
}
