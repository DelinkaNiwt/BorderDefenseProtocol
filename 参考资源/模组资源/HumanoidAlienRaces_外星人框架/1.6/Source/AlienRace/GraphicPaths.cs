using System.Collections.Generic;
using AlienRace.ApparelGraphics;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AlienRace;

public class GraphicPaths
{
	public const string VANILLA_HEAD_PATH = "Things/Pawn/Humanlike/Heads/";

	public const string VANILLA_BODY_PATH = "Things/Pawn/Humanlike/Bodies/";

	public const string VANILLA_SKELETON_PATH = "Things/Pawn/Humanlike/HumanoidDessicated";

	public AlienPartGenerator.ExtendedGraphicTop body = new AlienPartGenerator.ExtendedGraphicTop
	{
		path = "Things/Pawn/Humanlike/Bodies/"
	};

	public AlienPartGenerator.ExtendedGraphicTop bodyMasks = new AlienPartGenerator.ExtendedGraphicTop
	{
		path = string.Empty
	};

	public AlienPartGenerator.ExtendedGraphicTop head = new AlienPartGenerator.ExtendedGraphicTop
	{
		path = "Things/Pawn/Humanlike/Heads/"
	};

	public AlienPartGenerator.ExtendedGraphicTop headMasks = new AlienPartGenerator.ExtendedGraphicTop
	{
		path = string.Empty
	};

	public AlienPartGenerator.ExtendedGraphicTop skeleton = new AlienPartGenerator.ExtendedGraphicTop
	{
		path = "Things/Pawn/Humanlike/HumanoidDessicated"
	};

	public AlienPartGenerator.ExtendedGraphicTop skull = new AlienPartGenerator.ExtendedGraphicTop
	{
		path = "Things/Pawn/Humanlike/Heads/None_Average_Skull"
	};

	public AlienPartGenerator.ExtendedGraphicTop stump = new AlienPartGenerator.ExtendedGraphicTop
	{
		path = "Things/Pawn/Humanlike/Heads/None_Average_Stump"
	};

	public AlienPartGenerator.ExtendedGraphicTop swaddle = new AlienPartGenerator.ExtendedGraphicTop
	{
		path = "Things/Pawn/Humanlike/Apparel/SwaddledBaby/Swaddled_Child"
	};

	public ApparelGraphicsOverrides apparel = new ApparelGraphicsOverrides();

	public ShaderTypeDef skinShader;

	public Color skinColor = new Color(1f, 0f, 0f, 1f);

	private List<ShaderParameter> skinColoringParameter;

	public List<ShaderParameter> SkinColoringParameter
	{
		get
		{
			if (skinColoringParameter == null)
			{
				ShaderParameter parameter = new ShaderParameter();
				Traverse traverse = Traverse.Create(parameter);
				traverse.Field("name").SetValue("_ShadowColor");
				traverse.Field("value").SetValue(new Vector4(skinColor.r, skinColor.g, skinColor.b, skinColor.a));
				traverse.Field("type").SetValue(1);
				skinColoringParameter = new List<ShaderParameter>(1) { parameter };
			}
			return skinColoringParameter;
		}
	}
}
