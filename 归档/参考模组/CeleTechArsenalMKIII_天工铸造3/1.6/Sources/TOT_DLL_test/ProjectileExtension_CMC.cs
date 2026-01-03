using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class ProjectileExtension_CMC : DefModExtension
{
	public float FleckFadeRange = 0.45f;

	public float FluctrationIntensity = 1.22f;

	public bool DoEXPCircle = true;

	public bool DoEXP2 = true;

	public bool SpawnFilthnFleck = false;

	public FleckDef SpawnFleckDef;

	public float SpawnedSize = 1f;

	public float Exp2Range = 8f;

	public float SpreadAngle = 15f;

	public bool Exp2Fleck = false;

	public FleckDef SpawnExp2FleckDef;

	public string SpritePath;

	public string texturePath;

	public int totalFrames;

	public int ticksPerFrame;

	public Vector2 DrawSize = Vector2.zero;
}
