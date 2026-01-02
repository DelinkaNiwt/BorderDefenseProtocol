using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class Building_CMCTempControl : Building_TempControl
{
	private bool IsBlocked;

	public override void TickRare()
	{
		if (!base.Spawned || !compPowerTrader.PowerOn)
		{
			return;
		}
		IntVec3 intVec = base.Position + IntVec3.South.RotatedBy(base.Rotation);
		float ambientTemperature = base.AmbientTemperature;
		CompProperties_Power props = compPowerTrader.Props;
		if (!intVec.Impassable(base.Map))
		{
			Room room = intVec.GetRoom(base.Map);
			if (room != null && !room.UsesOutdoorTemperature)
			{
				if (room.Temperature > 100f || room.Temperature < -100f)
				{
					CompBreakdownable compBreakdownable = this.TryGetComp<CompBreakdownable>();
					if (compBreakdownable != null && !this.IsBrokenDown())
					{
						compBreakdownable.DoBreakdown();
					}
				}
				else
				{
					compPowerTrader.PowerOutput = 0f - props.PowerConsumption - (float)room.CellCount * 0.2f - (float)room.OpenRoofCount * 11f;
					room.Temperature = compTempControl.targetTemperature;
					compTempControl.operatingAtHighPower = (float)room.CellCount > 20f;
				}
			}
			IsBlocked = false;
		}
		else
		{
			IsBlocked = true;
		}
	}

	public override string GetInspectString()
	{
		string text = base.GetInspectString();
		if (IsBlocked)
		{
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}
			text += "CMC.TempControlIsBlocked".Translate().Colorize(ColorLibrary.Red);
		}
		return text;
	}
}
