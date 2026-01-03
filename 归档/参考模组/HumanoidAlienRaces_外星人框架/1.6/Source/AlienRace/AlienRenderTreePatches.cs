using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace;

public static class AlienRenderTreePatches
{
	public class PawnRenderResolveData
	{
		public Pawn pawn;

		public ThingDef_AlienRace alienProps;

		public AlienPartGenerator.AlienComp alienComp;

		public LifeStageAgeAlien lsaa;

		public int sharedIndex;
	}

	private static readonly Type patchType = typeof(AlienRenderTreePatches);

	public static PawnRenderResolveData pawnRenderResolveData;

	public static Pair<WeakReference, bool> portraitRender = new Pair<WeakReference, bool>(new WeakReference(new Pawn()), second: false);

	public static void HarmonyInit(AlienHarmony harmony)
	{
		harmony.Patch(AccessTools.Method(typeof(HumanlikeMeshPoolUtility), "GetHumanlikeBodySetForPawn"), new HarmonyMethod(patchType, "GetHumanlikeBodySetForPawnPrefix"));
		harmony.Patch(AccessTools.Method(typeof(HumanlikeMeshPoolUtility), "GetHumanlikeHeadSetForPawn"), new HarmonyMethod(patchType, "GetHumanlikeHeadSetForPawnPrefix"));
		harmony.Patch(AccessTools.Method(typeof(HumanlikeMeshPoolUtility), "GetHumanlikeHairSetForPawn"), new HarmonyMethod(patchType, "GetHumanlikeHeadSetForPawnPrefix"));
		harmony.Patch(AccessTools.Method(typeof(HumanlikeMeshPoolUtility), "GetHumanlikeBeardSetForPawn"), new HarmonyMethod(patchType, "GetHumanlikeHeadSetForPawnPrefix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRenderNode), "GetMesh"), null, null, new HarmonyMethod(patchType, "RenderNodeGetMeshTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnRenderTree), "TrySetupGraphIfNeeded"), new HarmonyMethod(patchType, "TrySetupGraphIfNeededPrefix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRenderTree), "EnsureInitialized"), null, new HarmonyMethod(patchType, "PawnRenderTreeEnsureInitializedPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRenderNode_Body), "GraphicFor"), new HarmonyMethod(patchType, "BodyGraphicForPrefix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRenderNode_Head), "GraphicFor"), new HarmonyMethod(patchType, "HeadGraphicForPrefix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRenderNode_Stump), "GraphicFor"), null, null, new HarmonyMethod(patchType, "StumpGraphicForTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(HairDef), "GraphicFor"), null, null, new HarmonyMethod(patchType, "HairDefGraphicForTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(TattooDef), "GraphicFor"), null, null, new HarmonyMethod(patchType, "TattooDefGraphicForTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(BeardDef), "GraphicFor"), null, null, new HarmonyMethod(patchType, "BeardDefGraphicForTranspiler"));
	}

	public static PawnRenderResolveData RegenerateResolveData(Pawn pawn)
	{
		if (AlienRenderTreePatches.pawnRenderResolveData?.pawn == pawn)
		{
			return AlienRenderTreePatches.pawnRenderResolveData;
		}
		PawnRenderResolveData pawnRenderResolveData = new PawnRenderResolveData();
		pawnRenderResolveData.pawn = pawn;
		pawnRenderResolveData.alienProps = pawn.def as ThingDef_AlienRace;
		pawnRenderResolveData.alienComp = pawn.GetComp<AlienPartGenerator.AlienComp>();
		pawnRenderResolveData.lsaa = pawn.ageTracker.CurLifeStageRace as LifeStageAgeAlien;
		pawnRenderResolveData.sharedIndex = 0;
		return AlienRenderTreePatches.pawnRenderResolveData = pawnRenderResolveData;
	}

	public static bool IsStatuePawn(Pawn pawn)
	{
		return pawn.Drawer.renderer.StatueColor.HasValue;
	}

	public static Color CheckOverrideColor(Pawn pawn, Color color)
	{
		return pawn.Drawer.renderer.StatueColor ?? color;
	}

	public static Shader CheckMaskShader(string texPath, Shader shader, bool pathCheckOverride = false)
	{
		if (shader.SupportsMaskTex() || (!pathCheckOverride && !(ContentFinder<Texture2D>.Get(texPath + "_northm", reportFailure: false) != null)))
		{
			return shader;
		}
		return ShaderDatabase.CutoutComplex;
	}

	public static void TrySetupGraphIfNeededPrefix(PawnRenderTree __instance)
	{
		if (__instance.Resolved)
		{
			return;
		}
		Pawn alien = __instance.pawn;
		if (alien.def is ThingDef_AlienRace alienProps && alien.story != null)
		{
			RegenerateResolveData(alien);
			AlienPartGenerator.AlienComp alienComp = pawnRenderResolveData.alienComp;
			if (alienComp != null)
			{
				if (alienComp.fixGenderPostSpawn)
				{
					float? maleGenderProbability = alien.kindDef.GetModExtension<Info>()?.maleGenderProbability ?? alienProps.alienRace.generalSettings.maleGenderProbability;
					__instance.pawn.gender = ((!(Rand.Value >= maleGenderProbability)) ? Gender.Male : Gender.Female);
					__instance.pawn.Name = PawnBioAndNameGenerator.GeneratePawnName(__instance.pawn);
					alienComp.fixGenderPostSpawn = false;
				}
				LifeStageAgeAlien lsaa = pawnRenderResolveData.lsaa;
				if (alien.gender == Gender.Female)
				{
					alienComp.customDrawSize = (lsaa.customFemaleDrawSize.Equals(Vector2.zero) ? lsaa.customDrawSize : lsaa.customFemaleDrawSize);
					alienComp.customHeadDrawSize = (lsaa.customFemaleHeadDrawSize.Equals(Vector2.zero) ? lsaa.customHeadDrawSize : lsaa.customFemaleHeadDrawSize);
					alienComp.customPortraitDrawSize = (lsaa.customFemalePortraitDrawSize.Equals(Vector2.zero) ? lsaa.customPortraitDrawSize : lsaa.customFemalePortraitDrawSize);
					alienComp.customPortraitHeadDrawSize = (lsaa.customFemalePortraitHeadDrawSize.Equals(Vector2.zero) ? lsaa.customPortraitHeadDrawSize : lsaa.customFemalePortraitHeadDrawSize);
				}
				else
				{
					alienComp.customDrawSize = lsaa.customDrawSize;
					alienComp.customHeadDrawSize = lsaa.customHeadDrawSize;
					alienComp.customPortraitDrawSize = lsaa.customPortraitDrawSize;
					alienComp.customPortraitHeadDrawSize = lsaa.customPortraitHeadDrawSize;
				}
				alienComp.UpdateColors();
				portraitRender = new Pair<WeakReference, bool>(new WeakReference(alien), second: false);
			}
			return;
		}
		AnimalComp comp = alien.GetComp<AnimalComp>();
		if (comp == null)
		{
			return;
		}
		AnimalBodyAddons extension = alien.def.GetModExtension<AnimalBodyAddons>();
		if (extension == null)
		{
			return;
		}
		comp.addonGraphics = new List<Graphic>();
		AnimalComp animalComp = comp;
		if (animalComp.addonVariants == null)
		{
			animalComp.addonVariants = new List<int>();
		}
		int sharedIndex = 0;
		for (int i = 0; i < extension.bodyAddons.Count; i++)
		{
			Graphic path = extension.bodyAddons[i].GetGraphic(alien, null, ref sharedIndex, (comp.addonVariants.Count > i) ? new int?(comp.addonVariants[i]) : ((int?)null));
			comp.addonGraphics.Add(path);
			if (comp.addonVariants.Count <= i)
			{
				comp.addonVariants.Add(sharedIndex);
			}
		}
	}

	public static void PawnRenderTreeEnsureInitializedPostfix(PawnRenderTree __instance)
	{
		pawnRenderResolveData = null;
	}

	public static bool BodyGraphicForPrefix(PawnRenderNode_Body __instance, Pawn pawn, ref Graphic __result)
	{
		if (!(pawn.def is ThingDef_AlienRace))
		{
			return true;
		}
		PawnRenderResolveData pawnRenderData = RegenerateResolveData(pawn);
		int sharedIndex = pawnRenderData.sharedIndex;
		GraphicPaths graphicPaths = pawnRenderData.alienProps.alienRace.graphicPaths;
		AlienPartGenerator.AlienComp alienComp = pawnRenderData.alienComp;
		AlienPartGenerator apg = pawnRenderData.alienProps.alienRace.generalSettings.alienPartGenerator;
		string bodyPath = graphicPaths.body.GetPath(pawn, ref sharedIndex, (alienComp.bodyVariant < 0) ? ((int?)null) : new int?(alienComp.bodyVariant));
		alienComp.bodyVariant = sharedIndex;
		string bodyMask = graphicPaths.bodyMasks.GetPath(pawn, ref sharedIndex, (alienComp.bodyMaskVariant < 0) ? ((int?)null) : new int?(alienComp.bodyMaskVariant));
		alienComp.bodyMaskVariant = sharedIndex;
		pawnRenderData.sharedIndex = sharedIndex;
		Shader skinShader = (pawn.Drawer.renderer.StatueColor.HasValue ? ShaderDatabase.Cutout : (graphicPaths.skinShader?.Shader ?? ShaderUtility.GetSkinShader(pawn)));
		if (skinShader == ShaderDatabase.CutoutSkin && pawn.story.SkinColorOverriden)
		{
			skinShader = ShaderDatabase.CutoutSkinColorOverride;
		}
		if (pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated)
		{
			string skeletonPath = graphicPaths.skeleton.GetPath(pawn, ref sharedIndex, alienComp.bodyVariant);
			__result = ((!skeletonPath.NullOrEmpty()) ? GraphicDatabase.Get<Graphic_Multi>(skeletonPath, ShaderDatabase.Cutout) : null);
			return false;
		}
		__result = ((!bodyPath.NullOrEmpty()) ? CachedData.getInnerGraphic(new GraphicRequest(typeof(Graphic_Multi), bodyPath, CheckMaskShader(bodyPath, skinShader, !bodyMask.NullOrEmpty()), Vector2.one, __instance.ColorFor(pawn), apg.SkinColor(pawn, first: false), null, 0, graphicPaths.SkinColoringParameter, bodyMask)) : null);
		return false;
	}

	public static bool HeadGraphicForPrefix(PawnRenderNode_Head __instance, Pawn pawn, ref Graphic __result)
	{
		if (!(pawn.def is ThingDef_AlienRace))
		{
			return true;
		}
		PawnRenderResolveData pawnRenderData = RegenerateResolveData(pawn);
		int sharedIndex = pawnRenderData.sharedIndex;
		GraphicPaths graphicPaths = pawnRenderData.alienProps.alienRace.graphicPaths;
		AlienPartGenerator.AlienComp alienComp = pawnRenderData.alienComp;
		AlienPartGenerator apg = pawnRenderData.alienProps.alienRace.generalSettings.alienPartGenerator;
		string headPath = graphicPaths.head.GetPath(pawn, ref sharedIndex, (alienComp.headVariant < 0) ? ((int?)null) : new int?(alienComp.headVariant));
		alienComp.headVariant = sharedIndex;
		string headMask = graphicPaths.headMasks.GetPath(pawn, ref sharedIndex, (alienComp.headMaskVariant < 0) ? ((int?)null) : new int?(alienComp.headMaskVariant));
		alienComp.headMaskVariant = sharedIndex;
		pawnRenderData.sharedIndex = sharedIndex;
		Shader skinShader = (pawn.Drawer.renderer.StatueColor.HasValue ? ShaderDatabase.Cutout : (graphicPaths.skinShader?.Shader ?? ShaderUtility.GetSkinShader(pawn)));
		if (skinShader == ShaderDatabase.CutoutSkin && pawn.story.SkinColorOverriden)
		{
			skinShader = ShaderDatabase.CutoutSkinColorOverride;
		}
		if (pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated)
		{
			string skullPath = graphicPaths.skull.GetPath(pawn, ref sharedIndex, alienComp.headVariant);
			__result = ((pawn.health.hediffSet.HasHead && !skullPath.NullOrEmpty()) ? GraphicDatabase.Get<Graphic_Multi>(skullPath, ShaderDatabase.Cutout, Vector2.one, Color.white) : null);
			return false;
		}
		__result = ((pawn.health.hediffSet.HasHead && !headPath.NullOrEmpty()) ? CachedData.getInnerGraphic(new GraphicRequest(typeof(Graphic_Multi), headPath, CheckMaskShader(headPath, skinShader, !headMask.NullOrEmpty()), Vector2.one, __instance.ColorFor(pawn), apg.SkinColor(pawn, first: false), null, 0, graphicPaths.SkinColoringParameter, headMask)) : null);
		return false;
	}

	public static IEnumerable<CodeInstruction> StumpGraphicForTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo baseInfo = AccessTools.Method(typeof(PawnRenderNode), "GraphicFor");
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.Calls(baseInfo))
			{
				yield return CodeInstruction.Call(patchType, "StumpGraphicHelper");
			}
			else
			{
				yield return instruction;
			}
		}
	}

	public static Graphic StumpGraphicHelper(PawnRenderNode_Stump node, Pawn pawn)
	{
		string path = pawnRenderResolveData.alienProps?.alienRace.graphicPaths.stump.GetPath(pawn, ref pawnRenderResolveData.sharedIndex, pawnRenderResolveData.alienComp.headVariant);
		if (path.NullOrEmpty())
		{
			return null;
		}
		return GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.CutoutComplex, Vector2.one, node.ColorFor(pawn), pawnRenderResolveData.alienProps.alienRace.generalSettings.alienPartGenerator.SkinColor(pawn, first: false));
	}

	public static IEnumerable<CodeInstruction> HairDefGraphicForTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (instruction.opcode == OpCodes.Call && instructionList[i + 1].opcode == OpCodes.Ret)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return CodeInstruction.Call(patchType, "HairGraphicHelper");
			}
			else
			{
				yield return instruction;
			}
			if (instruction.opcode == OpCodes.Brfalse_S)
			{
				yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(patchType, "pawnRenderResolveData"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderResolveData), "alienProps"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef_AlienRace), "alienRace"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef_AlienRace.AlienSettings), "styleSettings"));
				yield return new CodeInstruction(OpCodes.Ldtoken, typeof(HairDef));
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type), "GetTypeFromHandle"));
				yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<Type, StyleSettings>), "get_Item"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StyleSettings), "hasStyle"));
				yield return new CodeInstruction(OpCodes.Brtrue_S, instruction.operand);
			}
		}
	}

	public static Graphic HairGraphicHelper(string texPath, Shader shader, Vector2 size, Color color, Pawn pawn)
	{
		return GraphicDatabase.Get<Graphic_Multi>(texPath, CheckMaskShader(texPath, RegenerateResolveData(pawn).alienProps?.alienRace.styleSettings[typeof(HairDef)].shader?.Shader ?? shader), size, color, CheckOverrideColor(pawn, pawnRenderResolveData.alienComp?.GetChannel("hair").second ?? Color.white));
	}

	public static IEnumerable<CodeInstruction> TattooDefGraphicForTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		FieldInfo headTypeInfo = AccessTools.Field(typeof(HeadTypeDef), "graphicPath");
		FieldInfo bodyTypeInfo = AccessTools.Field(typeof(BodyTypeDef), "bodyNakedGraphicPath");
		Label nonAlienJumpLabel = ilg.DefineLabel();
		Label colorLabel = ilg.DefineLabel();
		LocalBuilder inactiveLocal = ilg.DeclareLocal(typeof(bool));
		LocalBuilder styleLocal = ilg.DeclareLocal(typeof(StyleSettings));
		LocalBuilder colorLocal = ilg.DeclareLocal(typeof(AlienPartGenerator.ExposableValueTuple<Color, Color>));
		bool conditionJumpDone = false;
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (!conditionJumpDone && instruction.opcode == OpCodes.Brfalse_S)
			{
				conditionJumpDone = true;
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "RegenerateResolveData"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderResolveData), "alienProps"));
				yield return new CodeInstruction(OpCodes.Dup);
				yield return new CodeInstruction(OpCodes.Ldnull);
				yield return new CodeInstruction(OpCodes.Ceq);
				yield return new CodeInstruction(OpCodes.Dup);
				yield return new CodeInstruction(OpCodes.Stloc, inactiveLocal.LocalIndex);
				yield return new CodeInstruction(OpCodes.Brfalse_S, nonAlienJumpLabel);
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ThingDefOf), "Human"));
				yield return new CodeInstruction(OpCodes.Castclass, typeof(ThingDef_AlienRace));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef_AlienRace), "alienRace")).WithLabels(nonAlienJumpLabel);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef_AlienRace.AlienSettings), "styleSettings"));
				yield return new CodeInstruction(OpCodes.Ldtoken, typeof(TattooDef));
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type), "GetTypeFromHandle"));
				yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<Type, StyleSettings>), "get_Item"));
				yield return new CodeInstruction(OpCodes.Stloc, styleLocal.LocalIndex);
				yield return instruction;
				yield return new CodeInstruction(OpCodes.Ldloc, styleLocal.LocalIndex);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StyleSettings), "hasStyle"));
				yield return new CodeInstruction(OpCodes.Brtrue_S, instruction.operand);
			}
			else if (instruction.opcode == OpCodes.Ldarg_2)
			{
				yield return instruction;
				yield return instructionList[i + 1];
				yield return new CodeInstruction(OpCodes.Ldloc, inactiveLocal.LocalIndex);
				yield return new CodeInstruction(OpCodes.Brtrue, colorLabel);
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Pop);
				i++;
				yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(patchType, "pawnRenderResolveData"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderResolveData), "alienComp"));
				yield return new CodeInstruction(OpCodes.Ldstr, "tattoo");
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AlienPartGenerator.AlienComp), "GetChannel"));
				yield return new CodeInstruction(OpCodes.Dup);
				yield return new CodeInstruction(OpCodes.Stloc, colorLocal.LocalIndex);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(AlienPartGenerator.ExposableValueTuple<Color, Color>), "first"));
				yield return new CodeInstruction(OpCodes.Ldloc, colorLocal.LocalIndex);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(AlienPartGenerator.ExposableValueTuple<Color, Color>), "second"));
				instructionList[i + 1].labels.Add(colorLabel);
			}
			else
			{
				yield return instruction;
			}
			if (instructionList[i].LoadsField(headTypeInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldc_I4_0);
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "TattooPathHelper"));
			}
			else if (instructionList[i].LoadsField(bodyTypeInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldc_I4_1);
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "TattooPathHelper"));
			}
			if (instruction.opcode == OpCodes.Ldsfld && (instruction.operand as FieldInfo)?.FieldType == typeof(Shader))
			{
				yield return new CodeInstruction(OpCodes.Ldloc, styleLocal.LocalIndex).MoveLabelsFrom(instructionList[i + 1]);
				yield return CodeInstruction.Call(patchType, "TattooShaderHelper");
			}
		}
	}

	public static Shader TattooShaderHelper(Shader shader, StyleSettings style)
	{
		return style.shader?.Shader ?? shader;
	}

	public static string TattooPathHelper(string path, Pawn pawn, bool body)
	{
		return ((!body) ? pawn.Drawer.renderer.HeadGraphic?.path : pawn.Drawer.renderer.BodyGraphic?.path) ?? path;
	}

	public static IEnumerable<CodeInstruction> BeardDefGraphicForTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (instruction.opcode == OpCodes.Call && instructionList[i + 1].opcode == OpCodes.Ret)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return CodeInstruction.Call(patchType, "BeardGraphicHelper");
			}
			else
			{
				yield return instruction;
			}
			if (instruction.opcode == OpCodes.Brfalse_S)
			{
				yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(patchType, "pawnRenderResolveData"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderResolveData), "alienProps"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef_AlienRace), "alienRace"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef_AlienRace.AlienSettings), "styleSettings"));
				yield return new CodeInstruction(OpCodes.Ldtoken, typeof(BeardDef));
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type), "GetTypeFromHandle"));
				yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<Type, StyleSettings>), "get_Item"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StyleSettings), "hasStyle"));
				yield return new CodeInstruction(OpCodes.Brtrue_S, instruction.operand);
			}
		}
	}

	public static Graphic BeardGraphicHelper(string texPath, Shader shader, Vector2 size, Color color, Pawn pawn)
	{
		return GraphicDatabase.Get<Graphic_Multi>(texPath, CheckMaskShader(texPath, RegenerateResolveData(pawn).alienProps?.alienRace.styleSettings[typeof(BeardDef)].shader?.Shader ?? shader), size, color, CheckOverrideColor(pawn, pawnRenderResolveData.alienComp?.GetChannel("hair").second ?? Color.white));
	}

	public static IEnumerable<CodeInstruction> RenderNodeGetMeshTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		FieldInfo meshSetInfo = AccessTools.Field(typeof(PawnRenderNode), "meshSet");
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int index = 0; index < instructionList.Count; index++)
		{
			CodeInstruction instruction = instructionList[index];
			yield return instruction;
			if (instruction.LoadsField(meshSetInfo) && instructionList[index + 1].opcode == OpCodes.Ldarg_1)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return CodeInstruction.Call(patchType, "RenderNodeGetMeshHelper");
			}
		}
	}

	public static GraphicMeshSet RenderNodeGetMeshHelper(GraphicMeshSet meshSet, PawnRenderNode node, PawnDrawParms parms)
	{
		portraitRender = new Pair<WeakReference, bool>(new WeakReference(parms.pawn), parms.Portrait);
		if (parms.Portrait)
		{
			return node.MeshSetFor(parms.pawn);
		}
		return meshSet;
	}

	public static bool IsPortrait(Pawn pawn)
	{
		if (portraitRender.First?.Target as Pawn == pawn)
		{
			return portraitRender.Second;
		}
		return false;
	}

	public static void GetHumanlikeHeadSetForPawnPrefix(Pawn pawn, ref float wFactor, ref float hFactor)
	{
		Vector2 drawSize = ((!IsPortrait(pawn)) ? pawn.GetComp<AlienPartGenerator.AlienComp>()?.customHeadDrawSize : pawn.GetComp<AlienPartGenerator.AlienComp>()?.customPortraitHeadDrawSize) ?? Vector2.one;
		wFactor *= drawSize.x;
		hFactor *= drawSize.y;
	}

	public static void GetHumanlikeBodySetForPawnPrefix(Pawn pawn, ref float wFactor, ref float hFactor)
	{
		Vector2 drawSize = ((!IsPortrait(pawn)) ? pawn.GetComp<AlienPartGenerator.AlienComp>()?.customDrawSize : pawn.GetComp<AlienPartGenerator.AlienComp>()?.customPortraitDrawSize) ?? Vector2.one;
		wFactor *= drawSize.x;
		hFactor *= drawSize.y;
	}
}
