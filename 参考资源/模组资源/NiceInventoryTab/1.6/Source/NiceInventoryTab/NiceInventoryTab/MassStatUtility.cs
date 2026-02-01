using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public static class MassStatUtility
{
	public static float AllMass(Pawn p, StatDrawer statBar)
	{
		StatBar obj = statBar as StatBar;
		float num = ApparelMass(p);
		float num2 = EquipmentMass(p);
		Color color = Assets.fromHEX(2586623);
		Color color2 = Assets.fromHEX(2611711);
		Color color3 = Assets.fromHEX(2621381);
		float num3 = MassUtility.InventoryMass(p);
		obj.AddOverlay(num, color, 0f);
		obj.AddOverlay(num2, color2, 0.2f);
		obj.AddOverlay(MassUtility.InventoryMass(p), color3, 0.2f);
		obj.Descr = MakeTooltip(num, num2, num3, Capacity(p), color, color2, color3);
		return num + num2 + num3;
	}

	private static string MakeTooltip(float app, float equip, float inv, float cap, Color appCol, Color equipCol, Color invCol)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("MassCarried".Translate((app + equip + inv).ToString("0.##"), cap.ToString("0.##")));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(string.Format("{0}: {1}", "NIT_Apparel".Translate(), app.ToString(Assets.Format_KG)).Colorize(appCol));
		stringBuilder.AppendLine(string.Format("{0}: {1}", "NIT_Equipment".Translate(), equip.ToString(Assets.Format_KG)).Colorize(equipCol));
		stringBuilder.AppendLine(string.Format("{0}: {1}", "NIT_Inventory".Translate(), inv.ToString(Assets.Format_KG)).Colorize(invCol));
		return stringBuilder.ToString();
	}

	public static float Capacity(Pawn p)
	{
		return MassUtility.Capacity(p);
	}

	public static float ApparelMass(Pawn p)
	{
		float num = 0f;
		if (p.apparel != null)
		{
			List<Apparel> wornApparel = p.apparel.WornApparel;
			for (int i = 0; i < wornApparel.Count; i++)
			{
				num += wornApparel[i].GetStatValue(StatDefOf.Mass, applyPostProcess: true, 1);
			}
		}
		return num;
	}

	public static float EquipmentMass(Pawn p)
	{
		float num = 0f;
		if (p.equipment != null)
		{
			foreach (ThingWithComps item in p.equipment.AllEquipmentListForReading)
			{
				num += item.GetStatValue(StatDefOf.Mass, applyPostProcess: true, 1);
			}
		}
		return num;
	}
}
