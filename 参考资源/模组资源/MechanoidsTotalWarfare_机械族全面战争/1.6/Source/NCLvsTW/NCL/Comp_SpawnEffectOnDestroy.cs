using Verse;

namespace NCL;

public class Comp_SpawnEffectOnDestroy : ThingComp
{
	private CompProperties_SpawnEffectOnDestroy Props => (CompProperties_SpawnEffectOnDestroy)props;

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		if (previousMap == null || Current.ProgramState != ProgramState.Playing)
		{
			return;
		}
		EffecterDef effectDef = DefDatabase<EffecterDef>.GetNamedSilentFail(Props.effectDefName);
		if (effectDef == null)
		{
			Log.Warning("特效定义未找到: " + Props.effectDefName);
			return;
		}
		MapEffecterTracker tracker = previousMap.GetComponent<MapEffecterTracker>();
		if (tracker == null)
		{
			tracker = new MapEffecterTracker(previousMap);
			previousMap.components.Add(tracker);
		}
		Effecter effect = effectDef.Spawn();
		effect.scale = Props.effectSize;
		TargetInfo target = new TargetInfo(parent.Position, previousMap);
		effect.Trigger(target, target);
		tracker.AddEffecter(effect, target, Props.durationTicks);
	}
}
