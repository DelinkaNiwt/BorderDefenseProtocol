using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HNGT;

public class ModExtension_BarrelWithRecoilAndFlash : DefModExtension
{
	public List<Vector2> offsets = new List<Vector2>();

	public string barrelTexturePath;

	public Vector2 barrelTextureSize = new Vector2(1f, 1f);

	public float recoilAmount = 0.5f;

	public int recoilDurationTicks = 20;

	public int recoilKickTicks = 5;

	public string muzzleFlashMoteDefName;
}
