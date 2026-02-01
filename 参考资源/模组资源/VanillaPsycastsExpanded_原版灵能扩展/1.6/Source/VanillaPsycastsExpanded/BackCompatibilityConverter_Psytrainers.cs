using System;
using System.Collections.Generic;
using System.Xml;
using RimWorld;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class BackCompatibilityConverter_Psytrainers : BackCompatibilityConverter
{
	private static readonly Dictionary<string, string> specialCases = new Dictionary<string, string>
	{
		{ "BulletShield", "VPE_Skipshield" },
		{ "EntropyDump", "VPE_NeuralHeatDump" }
	};

	public override bool AppliesToVersion(int majorVer, int minorVer)
	{
		return true;
	}

	public override string BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode node = null)
	{
		if (defName == null || !typeof(ThingDef).IsAssignableFrom(defType))
		{
			return null;
		}
		if (defName.StartsWith(ThingDefGenerator_Neurotrainer.PsytrainerDefPrefix))
		{
			string text = defName.Replace(ThingDefGenerator_Neurotrainer.PsytrainerDefPrefix + "_", "");
			if (!text.StartsWith("VPE_"))
			{
				if (text.StartsWith("WordOf"))
				{
					text = text.Replace("WordOf", "Wordof");
				}
				if (!specialCases.TryGetValue(text, out var value))
				{
					value = "VPE_" + text;
				}
				if (DefDatabase<AbilityDef>.GetNamedSilentFail(value) != null)
				{
					return ThingDefGenerator_Neurotrainer.PsytrainerDefPrefix + "_" + value;
				}
				Log.Warning("[VPE] Failed to find psycast for psytrainer called " + value + " (old name: " + text + ")");
				return ThingDefGenerator_Neurotrainer.PsytrainerDefPrefix + "_VPE_Flameball";
			}
		}
		return null;
	}

	public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
	{
		return null;
	}

	public override void PostExposeData(object obj)
	{
	}
}
