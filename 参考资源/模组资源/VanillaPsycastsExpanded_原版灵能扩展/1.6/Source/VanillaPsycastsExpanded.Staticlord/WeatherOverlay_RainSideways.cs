using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded.Staticlord;

public class WeatherOverlay_RainSideways : WeatherOverlayDualPanner
{
	public WeatherOverlay_RainSideways()
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			worldOverlayMat = TexHurricane.HurricaneOverlay;
			worldOverlayPanSpeed1 = 0.015f;
			worldPanDir1 = new Vector2(-1f, -0.25f);
			worldPanDir1.Normalize();
			worldOverlayPanSpeed2 = 0.022f;
			worldPanDir2 = new Vector2(-1f, -0.22f);
			worldPanDir2.Normalize();
		});
	}
}
