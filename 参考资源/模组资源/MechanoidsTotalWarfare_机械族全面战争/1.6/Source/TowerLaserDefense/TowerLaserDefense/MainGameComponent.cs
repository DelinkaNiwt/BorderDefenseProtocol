using Verse;

namespace TowerLaserDefense;

public class MainGameComponent : GameComponent
{
	public MainGameComponent(Game game)
	{
	}

	public override void FinalizeInit()
	{
		if (Current.ProgramState == ProgramState.Entry)
		{
			GameComponent_BulletsCache.ClearStaticCache();
			LaserDefenceCore.Instances.Clear();
		}
	}
}
