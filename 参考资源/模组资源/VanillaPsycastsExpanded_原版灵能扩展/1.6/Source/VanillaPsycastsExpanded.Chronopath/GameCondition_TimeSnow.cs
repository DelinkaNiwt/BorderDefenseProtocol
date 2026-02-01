using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded.Chronopath;

[StaticConstructorOnStartup]
public class GameCondition_TimeSnow : GameCondition
{
	public static readonly Material TimeSnowOverlay = MaterialPool.MatFrom("Effects/Chronopath/Timesnow/TimesnowWorldOverlay", ShaderDatabase.WorldOverlayTransparent);

	private Material worldOverlayMat;

	public override void PostMake()
	{
		base.PostMake();
		worldOverlayMat = TimeSnowOverlay;
	}

	public override void GameConditionDraw(Map map)
	{
		base.GameConditionDraw(map);
		if (worldOverlayMat != null)
		{
			UnityEngine.Graphics.DrawMesh(MeshPool.wholeMapPlane, map.Center.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather), Quaternion.identity, worldOverlayMat, 0);
		}
	}

	public override void GameConditionTick()
	{
		base.GameConditionTick();
		if (worldOverlayMat != null)
		{
			worldOverlayMat.SetTextureOffset("_MainTex", Find.TickManager.TicksGame % 3600000 * new Vector2(0.0005f, -0.002f) * worldOverlayMat.GetTextureScale("_MainTex").x);
			if (worldOverlayMat.HasProperty("_MainTex2"))
			{
				worldOverlayMat.SetTextureOffset("_MainTex2", Find.TickManager.TicksGame % 3600000 * new Vector2(0.0004f, -0.002f) * worldOverlayMat.GetTextureScale("_MainTex").x);
			}
		}
	}
}
