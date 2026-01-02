using Verse;

namespace AncotLibrary;

public class Verb_DeployThing : Verb
{
	protected override bool TryCastShot()
	{
		return DeployThing(base.EquipmentSource.TryGetComp<CompApparelReloadable_DeployThing>());
	}

	public static bool DeployThing(CompApparelReloadable_DeployThing reloadable)
	{
		if (reloadable == null || !reloadable.CanBeUsed(out var _) || reloadable.thingToDeploy == null)
		{
			return false;
		}
		reloadable.Deploy();
		reloadable.UsedOnce();
		return true;
	}
}
