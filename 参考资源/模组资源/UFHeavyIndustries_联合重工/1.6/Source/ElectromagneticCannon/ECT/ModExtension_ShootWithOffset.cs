using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ECT;

public class ModExtension_ShootWithOffset : DefModExtension
{
	public List<Vector2> offsets = new List<Vector2>();

	public List<Vector2> muzzleOffsets = new List<Vector2>();

	public string barrelTexturePath;

	public Vector2 barrelTextureSize = new Vector2(1f, 1f);

	public float recoilAmount = 0.5f;

	public int recoilKickTicks = 5;

	public int recoilDurationTicks = 20;

	public float muzzleFlashArcLength = 0f;

	public float muzzleFlashArcWidth = 1.5f;

	public int muzzleFlashArcDuration = 15;

	public float muzzleFlashArcVariance = 0.5f;

	public int muzzleFlashArcCount = 1;

	public float muzzleFlashArcSpacing = 0.5f;

	public Vector2 GetOffsetFor(int index)
	{
		if (offsets.NullOrEmpty())
		{
			return Vector2.zero;
		}
		return offsets[index % offsets.Count];
	}

	public Vector2 GetMuzzleOffsetFor(int index)
	{
		if (muzzleOffsets.NullOrEmpty())
		{
			return Vector2.zero;
		}
		return muzzleOffsets[index % muzzleOffsets.Count];
	}
}
