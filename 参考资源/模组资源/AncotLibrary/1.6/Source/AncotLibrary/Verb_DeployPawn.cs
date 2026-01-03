using Verse;

namespace AncotLibrary;

public class Verb_DeployPawn : Verb
{
	protected override bool TryCastShot()
	{
		return DeployPawn(base.EquipmentSource.TryGetComp<CompApparelReloadable_DeployPawn>());
	}

	public static bool DeployPawn(CompApparelReloadable_DeployPawn reloadable)
	{
		if (reloadable == null || !reloadable.CanBeUsed(out var _) || reloadable.SpawnPawnKind == null)
		{
			return false;
		}
		reloadable.Deploy();
		reloadable.UsedOnce();
		return true;
	}
}
