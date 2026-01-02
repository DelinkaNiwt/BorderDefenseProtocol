using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace;

public static class TweakValues
{
	public enum TweakValueEditingMode
	{
		Lifestages,
		BodyAddon
	}

	private static readonly Dictionary<string, float> TWEAKVALUES_SAVED = new Dictionary<string, float>();

	private static int lineCount;

	private static ThingDef_AlienRace currentRace;

	private static int subcategoryIndex;

	private static TweakValueEditingMode editingMode;

	private static Color color1 = new Color32(238, 99, 82, byte.MaxValue);

	private static Color color2 = new Color32(8, 178, 227, byte.MaxValue);

	private static Color color3 = new Color32(225, 214, 234, byte.MaxValue);

	private static Color color4 = new Color32(87, 167, 115, byte.MaxValue);

	private static Color color5 = new Color32(72, 77, 109, byte.MaxValue);

	public static IEnumerable<CodeInstruction> TweakValuesTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo endScrollInfo = AccessTools.Method(typeof(DevGUI), "EndScrollView");
		MethodInfo countInfo = AccessTools.Property(AccessTools.Field(typeof(EditWindow_TweakValues), "tweakValueFields").FieldType, "Count").GetGetMethod();
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.OperandIs(endScrollInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldloca_S, 1)
				{
					labels = instruction.ExtractLabels()
				};
				yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
				yield return new CodeInstruction(OpCodes.Ldloca_S, 3);
				yield return new CodeInstruction(OpCodes.Ldloca_S, 4);
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TweakValues), "TweakValuesInstanceBased"));
			}
			yield return instruction;
			if (instruction.OperandIs(countInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TweakValues), "lineCount"));
				yield return new CodeInstruction(OpCodes.Add);
			}
		}
	}

	public static void TweakValuesInstanceBased(ref Rect refRect2, ref Rect refRect3, ref Rect refRect4, ref Rect refRect5)
	{
		bool dirtyGraphics = false;
		Rect rect2 = refRect2;
		Rect rect3 = refRect3;
		Rect rect4 = refRect4;
		Rect rect5 = refRect5;
		lineCount = 0;
		NextLine();
		if (DefDatabase<ThingDef_AlienRace>.DefCount > 0)
		{
			Widgets.Label(rect2, "Alien Race");
			Widgets.Label(rect3, "Select the race you wish to edit");
			if (currentRace == null)
			{
				currentRace = DefDatabase<ThingDef_AlienRace>.AllDefsListForReading.First();
			}
			Widgets.Dropdown(rect5, currentRace, (ThingDef_AlienRace td) => td, (ThingDef_AlienRace _) => DefDatabase<ThingDef_AlienRace>.AllDefsListForReading.Select((ThingDef_AlienRace td) => new Widgets.DropdownMenuElement<ThingDef_AlienRace>
			{
				option = new FloatMenuOption(td.LabelCap, delegate
				{
					currentRace = td;
					subcategoryIndex = 0;
				}),
				payload = td
			}), currentRace.LabelCap);
			ThingDef_AlienRace ar = currentRace;
			string label2 = ar.LabelCap;
			Widgets.Label(rect4, label2);
			NextLine();
			Widgets.Label(rect3, "Select what you want to edit");
			Widgets.Dropdown(rect5, editingMode, (TweakValueEditingMode i) => i, (TweakValueEditingMode _) => from TweakValueEditingMode tvem in Enum.GetValues(typeof(TweakValueEditingMode))
				select new Widgets.DropdownMenuElement<TweakValueEditingMode>
				{
					option = new FloatMenuOption(tvem.ToString(), delegate
					{
						editingMode = tvem;
						subcategoryIndex = 0;
					}),
					payload = tvem
				}, editingMode.ToString());
			NextLine();
			switch (editingMode)
			{
			case TweakValueEditingMode.Lifestages:
			{
				Widgets.Label(rect3, "Select the lifestage you wish to edit");
				LifeStageAgeAlien lsaa = (LifeStageAgeAlien)ar.race.lifeStageAges[subcategoryIndex];
				Widgets.Dropdown(rect5, subcategoryIndex, (int i) => i, (int _) => ar.race.lifeStageAges.Select((LifeStageAge lsa, int i) => new Widgets.DropdownMenuElement<int>
				{
					option = new FloatMenuOption($"{lsa.def.defName} ({lsa.minAge})", delegate
					{
						subcategoryIndex = i;
					}),
					payload = i
				}), $"{lsaa.def.defName} ({lsaa.minAge})");
				NextLine();
				lsaa.headOffset.x = WriteLine(ar.LabelCap, "headOffset".Colorize(color1) + ".x", ar.defName + ".headOffset.x", lsaa.headOffset.x);
				NextLine();
				lsaa.headOffset.y = WriteLine(ar.LabelCap, "headOffset".Colorize(color1) + ".y", ar.defName + ".headOffset.y", lsaa.headOffset.y);
				NextLine();
				lsaa.headFemaleOffset.x = WriteLine(ar.LabelCap, "headFemaleOffset".Colorize(color1) + ".x", ar.defName + ".headFemaleOffset.x", lsaa.headFemaleOffset.x);
				NextLine();
				lsaa.headFemaleOffset.y = WriteLine(ar.LabelCap, "headFemaleOffset".Colorize(color1) + ".y", ar.defName + ".headFemaleOffset.y", lsaa.headFemaleOffset.y);
				NextLine();
				WriteRotationOffset(lsaa.headOffsetDirectional, (float value, float _, string text) => WriteLine(ar.LabelCap, "headOffsetDirectional".Colorize(color5) + "." + text, ar.defName + ".headOffsetDirectional." + text, value));
				WriteRotationOffset(lsaa.headFemaleOffsetDirectional, (float value, float _, string text) => WriteLine(ar.LabelCap, "headFemaleOffsetDirectional".Colorize(color5) + "." + text, ar.defName + ".headFemaleOffsetDirectional." + text, value));
				break;
			}
			case TweakValueEditingMode.BodyAddon:
			{
				if (ar.alienRace.generalSettings.alienPartGenerator?.bodyAddons?.NullOrEmpty() ?? true)
				{
					break;
				}
				Widgets.Label(rect2, label2);
				Widgets.Label(rect3, "Select the addon you wish to edit");
				Widgets.Dropdown(rect5, subcategoryIndex, (int i) => i, (int _) => ar.alienRace.generalSettings.alienPartGenerator.bodyAddons.Select((AlienPartGenerator.BodyAddon bodyAddon, int i) => new Widgets.DropdownMenuElement<int>
				{
					option = new FloatMenuOption(bodyAddon.Name, delegate
					{
						subcategoryIndex = i;
					}),
					payload = i
				}), ar.alienRace.generalSettings.alienPartGenerator.bodyAddons[subcategoryIndex].Name);
				AlienPartGenerator.BodyAddon ba = ar.alienRace.generalSettings.alienPartGenerator.bodyAddons[subcategoryIndex];
				string label3Addons = ba.Name ?? "";
				Widgets.Label(rect4, label3Addons);
				NextLine();
				Widgets.Label(rect2, label2);
				string offsetLabel = label3Addons + ".DefaultOffsets";
				string offsetDictKey = label2 + "." + offsetLabel;
				Widgets.Label(rect3, offsetLabel);
				if (!TWEAKVALUES_SAVED.ContainsKey(offsetDictKey))
				{
					TWEAKVALUES_SAVED.Add(offsetDictKey, ar.alienRace.generalSettings.alienPartGenerator.offsetDefaults.FirstIndexOf((AlienPartGenerator.OffsetNamed on) => on.name == ba.defaultOffset));
				}
				AlienPartGenerator.OffsetNamed offsetOld = ar.alienRace.generalSettings.alienPartGenerator.offsetDefaults[(int)TWEAKVALUES_SAVED[offsetDictKey]];
				AlienPartGenerator.OffsetNamed offsetNew = ar.alienRace.generalSettings.alienPartGenerator.offsetDefaults.First((AlienPartGenerator.OffsetNamed on) => on.name == ba.defaultOffset);
				Widgets.Dropdown(rect5, offsetNew, (AlienPartGenerator.OffsetNamed on) => on, (AlienPartGenerator.OffsetNamed _) => ar.alienRace.generalSettings.alienPartGenerator.offsetDefaults.Select((AlienPartGenerator.OffsetNamed on) => new Widgets.DropdownMenuElement<AlienPartGenerator.OffsetNamed>
				{
					option = new FloatMenuOption(on.name, delegate
					{
						ba.defaultOffset = on.name;
						ba.defaultOffsets = on.offsets;
					}),
					payload = on
				}), offsetNew.name);
				Rect valueFieldRect = rect4;
				GUI.color = Color.red;
				string savedS = offsetOld.name + " -> ";
				bool changed = offsetOld.name != offsetNew.name;
				float width = (changed ? Text.CalcSize(savedS).x : 0f);
				Rect savedRect = rect4.LeftPartPixels(width);
				Widgets.Label(savedRect, savedS);
				GUI.color = Color.white;
				if (changed)
				{
					valueFieldRect = rect4.RightPartPixels(rect4.width - width);
				}
				Widgets.Label(valueFieldRect, offsetNew.name);
				NextLine();
				WriteRotationOffset(ba.offsets, delegate(float value, float valueDefault, string text2)
				{
					string text = label3Addons + "." + text2;
					string saveKey = label2 + "." + text;
					return WriteLine(text, $"{ba.defaultOffset}: {valueDefault:+0.#####;-0.#####;0}", saveKey, value);
				}, ba.defaultOffsets);
				break;
			}
			}
		}
		NextLine();
		refRect2 = rect2;
		refRect3 = rect3;
		refRect4 = rect4;
		refRect5 = rect5;
		if (dirtyGraphics)
		{
			GlobalTextureAtlasManager.FreeAllRuntimeAtlases();
		}
		void NextLine()
		{
			lineCount++;
			rect2.y += rect2.height;
			rect3.y += rect2.height;
			rect4.y += rect2.height;
			rect5.y += rect2.height;
		}
		float WriteLine(string lineLabel, string lineLabel2, string saveKey, float value)
		{
			if (!TWEAKVALUES_SAVED.ContainsKey(saveKey))
			{
				TWEAKVALUES_SAVED.Add(saveKey, value);
			}
			Widgets.Label(rect2.Union(rect3.LeftPart(0.55f)), lineLabel);
			Text.Anchor = TextAnchor.MiddleRight;
			Widgets.Label(rect3.LeftPart(0.9f), lineLabel2);
			Text.Anchor = TextAnchor.UpperLeft;
			float num = value;
			Rect sliderRect = rect5;
			sliderRect.y += rect2.height / 3f;
			num = Widgets.HorizontalSlider(sliderRect, num, -1f, 1f);
			Rect valueFieldRect2 = rect4;
			GUI.color = Color.red;
			string savedS2 = TWEAKVALUES_SAVED[saveKey].ToString(CultureInfo.InvariantCulture) + " -> ";
			bool changed2 = Mathf.Abs(TWEAKVALUES_SAVED[saveKey] - value) > float.Epsilon;
			float width2 = (changed2 ? Text.CalcSize(savedS2).x : 0f);
			Rect savedRect2 = rect4.LeftPartPixels(width2);
			Widgets.Label(savedRect2, savedS2);
			GUI.color = Color.white;
			if (changed2)
			{
				valueFieldRect2 = rect4.RightPartPixels(rect4.width - width2);
			}
			if (Widgets.ButtonInvisible(savedRect2))
			{
				dirtyGraphics = true;
				return num = TWEAKVALUES_SAVED[saveKey];
			}
			string valueS = value.ToString(CultureInfo.InvariantCulture);
			string num2 = Widgets.TextField(valueFieldRect2.ContractedBy(2f).LeftPartPixels(Text.CalcSize(valueS).x + 6f), valueS);
			if (Mathf.Abs(num - value) < float.Epsilon && float.TryParse(num2, out var num3))
			{
				num = num3;
			}
			if (Mathf.Abs(num - value) > float.Epsilon)
			{
				dirtyGraphics = true;
			}
			return Mathf.Clamp(num, -1f, 1f);
		}
		void WriteRotationOffset(AlienPartGenerator.DirectionalOffset offset, Func<float, float, string, float> writeLineFunc, AlienPartGenerator.DirectionalOffset offsetComparison = null)
		{
			for (int i = 0; i < 4; i++)
			{
				Rot4 rotation = new Rot4(i);
				AlienPartGenerator.RotationOffset ro = offset.GetOffset(rotation);
				AlienPartGenerator.RotationOffset roD = offsetComparison?.GetOffset(rotation);
				string label3Rotation = rotation.ToStringHuman().Colorize(color1) + ".";
				ro.layerOffset = writeLineFunc(ro.layerOffset, roD?.layerOffset ?? 0f, label3Rotation + "layerOffset".Colorize(color2));
				NextLine();
				ro.offset.x = writeLineFunc(ro.offset.x, roD?.offset.x ?? 0f, label3Rotation + "offset".Colorize(color2) + ".x");
				NextLine();
				ro.offset.y = writeLineFunc(ro.offset.y, roD?.offset.y ?? 0f, label3Rotation + "offset".Colorize(color2) + ".y");
				NextLine();
				string label3Type;
				if (!ro.bodyTypes.NullOrEmpty())
				{
					foreach (BodyTypeDef bodyTypeDef in DefDatabase<BodyTypeDef>.AllDefsListForReading)
					{
						AlienPartGenerator.BodyTypeOffset bodyTypeOffset = ro.bodyTypes?.FirstOrDefault((AlienPartGenerator.BodyTypeOffset bto) => bto.bodyType == bodyTypeDef);
						if (bodyTypeOffset != null)
						{
							AlienPartGenerator.BodyTypeOffset bodyTypeOffsetDefault = roD?.bodyTypes?.FirstOrDefault((AlienPartGenerator.BodyTypeOffset bto) => bto.bodyType == bodyTypeDef);
							label3Type = bodyTypeOffset.bodyType.defName.Colorize(color3) + ".";
							bodyTypeOffset.offset.x = WriteRotationLine(bodyTypeOffset.offset.x, bodyTypeOffsetDefault?.offset.x ?? 0f, x: true);
							NextLine();
							bodyTypeOffset.offset.y = WriteRotationLine(bodyTypeOffset.offset.y, bodyTypeOffsetDefault?.offset.y ?? 0f, x: false);
							NextLine();
						}
					}
				}
				if (!ro.headTypes.NullOrEmpty())
				{
					foreach (HeadTypeDef headTypeDef in DefDatabase<HeadTypeDef>.AllDefsListForReading)
					{
						AlienPartGenerator.HeadTypeOffsets headTypeOffsets = ro.headTypes?.FirstOrDefault((AlienPartGenerator.HeadTypeOffsets hto) => hto.headType == headTypeDef);
						if (headTypeOffsets != null)
						{
							AlienPartGenerator.HeadTypeOffsets headTypeOffsetsDefault = roD?.headTypes?.FirstOrDefault((AlienPartGenerator.HeadTypeOffsets hto) => hto.headType == headTypeDef);
							label3Type = headTypeOffsets.headType.defName.Colorize(color4) + ".";
							headTypeOffsets.offset.x = WriteRotationLine(headTypeOffsets.offset.x, headTypeOffsetsDefault?.offset.x ?? 0f, x: true);
							NextLine();
							headTypeOffsets.offset.y = WriteRotationLine(headTypeOffsets.offset.y, headTypeOffsetsDefault?.offset.y ?? 0f, x: false);
							NextLine();
						}
					}
				}
				float WriteRotationLine(float value, float valueDefault, bool x)
				{
					return writeLineFunc(value, valueDefault, label3Rotation + label3Type + (x ? "x" : "y"));
				}
			}
		}
	}
}
