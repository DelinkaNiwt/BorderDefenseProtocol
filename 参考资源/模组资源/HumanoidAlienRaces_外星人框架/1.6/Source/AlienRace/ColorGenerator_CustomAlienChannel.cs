using UnityEngine;
using Verse;

namespace AlienRace;

public class ColorGenerator_CustomAlienChannel : ColorGenerator
{
	public string colorChannel;

	public override Color NewRandomizedColor()
	{
		return Color.clear;
	}

	public void GetInfo(out string channel, out bool first)
	{
		string[] split = colorChannel.Split('_');
		channel = split[0];
		first = split[1] == "1";
	}
}
