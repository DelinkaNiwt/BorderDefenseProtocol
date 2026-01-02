using AncotLibrary;
using RimWorld;

namespace WeaponFitting;

public class MainButtonWorker_WeaponFittings : MainButtonWorker_ToggleTab
{
	public override bool Disabled
	{
		get
		{
			if (base.Disabled)
			{
				return true;
			}
			GameComponent_AncotLibrary gc = GameComponent_AncotLibrary.GC;
			if (gc == null)
			{
				return false;
			}
			if (gc.AllWeapons.Count != 0)
			{
				return false;
			}
			return true;
		}
	}

	public override bool Visible => !Disabled;
}
