using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public static class SkillGraphics
{
	private static Dictionary<SkillType, Dictionary<string, SkillBehaviorMode>> behaviorModes;

	static SkillGraphics()
	{
		behaviorModes = new Dictionary<SkillType, Dictionary<string, SkillBehaviorMode>>();
		behaviorModes[SkillType.Charge] = new Dictionary<string, SkillBehaviorMode>();
		behaviorModes[SkillType.Charge].Add("ChargeA", new SkillBehaviorMode
		{
			behaviorId = "ChargeA",
			graphicDataA = new GraphicData
			{
				texPath = "ModIcon/Mantis_FlyingA",
				graphicClass = typeof(Graphic_Single),
				drawSize = new Vector2(4.5f, 4.5f)
			}
		});
		behaviorModes[SkillType.Charge].Add("ChargeB", new SkillBehaviorMode
		{
			behaviorId = "ChargeB",
			graphicDataA = new GraphicData
			{
				texPath = "ModIcon/Mantis_FlyingA",
				graphicClass = typeof(Graphic_Single),
				drawSize = new Vector2(4.5f, 4.5f)
			}
		});
		behaviorModes[SkillType.FlyingKick] = new Dictionary<string, SkillBehaviorMode>();
		behaviorModes[SkillType.FlyingKick].Add("FlyingKickA", new SkillBehaviorMode
		{
			behaviorId = "FlyingKickA",
			graphicDataA = new GraphicData
			{
				texPath = "Ability/PillBugFlyingB",
				graphicClass = typeof(Graphic_Single),
				drawSize = new Vector2(5f, 5f)
			}
		});
		behaviorModes[SkillType.FlyingKick].Add("FlyingKickB", new SkillBehaviorMode
		{
			behaviorId = "FlyingKickB",
			graphicDataA = new GraphicData
			{
				texPath = "Ability/PillBugFlyingB",
				graphicClass = typeof(Graphic_Single),
				drawSize = new Vector2(5f, 5f),
				shaderType = ShaderTypeDefOf.Transparent
			},
			graphicDataB = new GraphicData
			{
				texPath = "Ability/PillBugFlyingC",
				graphicClass = typeof(Graphic_Single),
				drawSize = new Vector2(5f, 5f),
				shaderType = ShaderTypeDefOf.Transparent
			}
		});
	}

	public static SkillBehaviorMode GetBehaviorMode(SkillType skillType, string modeId)
	{
		if (behaviorModes.TryGetValue(skillType, out var skillModes) && skillModes.TryGetValue(modeId, out var mode))
		{
			return mode;
		}
		return new SkillBehaviorMode();
	}
}
