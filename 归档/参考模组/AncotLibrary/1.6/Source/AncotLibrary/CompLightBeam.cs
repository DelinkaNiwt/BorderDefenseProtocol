using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class CompLightBeam : ThingComp
{
	private int startTick;

	public int spawnTick;

	private int totalDuration;

	private int fadeOutDuration;

	private float angle;

	private float speed;

	private float distance;

	private Sustainer sustainer;

	private const float AlphaAnimationSpeed = 0.3f;

	private const float AlphaAnimationStrength = 0.025f;

	private const float BeamEndHeightRatio = 0.5f;

	private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

	private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

	private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

	public CompProperties_LightBeam Props => (CompProperties_LightBeam)props;

	private int TicksPassed => Find.TickManager.TicksGame - startTick;

	private int TicksLeft => totalDuration - TicksPassed;

	private float BeamEndHeight => Props.width * 0.5f;

	public CompEquippable compEquippable => parent.TryGetComp<CompEquippable>();

	protected Pawn PawnOwner => compEquippable.PrimaryVerb.caster as Pawn;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref startTick, "startTick", 0);
		Scribe_Values.Look(ref totalDuration, "totalDuration", 600);
		Scribe_Values.Look(ref fadeOutDuration, "fadeOutDuration", 100);
		Scribe_Values.Look(ref angle, "angle", 0f);
		Scribe_Values.Look(ref spawnTick, "spawnTick", 0);
	}

	public void StartAnimation(int totalDuration, int fadeOutDuration, float angle)
	{
		startTick = Find.TickManager.TicksGame;
		this.totalDuration = totalDuration;
		this.fadeOutDuration = fadeOutDuration;
		this.angle = angle;
		CheckSpawnSustainer();
	}

	public void StartAnimationModified(int totalDuration, int fadeOutDuration, float angle, float speed, float distance)
	{
		startTick = Find.TickManager.TicksGame;
		this.totalDuration = totalDuration;
		this.fadeOutDuration = fadeOutDuration;
		this.angle = angle;
		this.speed = speed;
		this.distance = distance;
		CheckSpawnSustainer();
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			spawnTick = Find.TickManager.TicksGame;
		}
		CheckSpawnSustainer();
	}

	public override void CompTick()
	{
		base.CompTick();
		if (sustainer != null)
		{
			sustainer.Maintain();
			if (TicksLeft < fadeOutDuration)
			{
				sustainer.End();
				sustainer = null;
			}
		}
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (TicksLeft > 0)
		{
			Vector3 drawPos = PawnOwner.DrawPos;
			float num = ((float)PawnOwner.Map.Size.z - drawPos.z) * 1.4142135f;
			float num2 = Mathf.Min(num, num * 35f * (float)TicksPassed / (float)(totalDuration + fadeOutDuration));
			Vector3 vector = Vector3Utility.FromAngleFlat(angle - 90f);
			Vector3 vector2 = new Vector3(0f, 0f, 0.5f);
			Vector3 pos = drawPos + vector * num2 * 0.5f + vector2;
			pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
			float num3 = Mathf.Min((float)TicksPassed / 10f, 1f);
			Vector3 vector3 = vector * ((1f - num3) * num2) + vector2;
			float num4 = 0.975f + Mathf.Sin((float)TicksPassed * 0.3f) * 0.025f;
			if (TicksLeft < fadeOutDuration)
			{
				num4 *= (float)TicksLeft / (float)fadeOutDuration;
			}
			Color color = Props.color;
			color.a *= num4;
			MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(pos, Quaternion.Euler(0f, angle, 0f), new Vector3(Props.width, 1f, num2));
			Graphics.DrawMesh(MeshPool.plane10, matrix, BeamMat, 0, null, 0, MatPropertyBlock);
			Vector3 pos2 = drawPos + vector3;
			pos2.y = AltitudeLayer.MetaOverlays.AltitudeFor();
			Matrix4x4 matrix2 = default(Matrix4x4);
			matrix2.SetTRS(pos2, Quaternion.Euler(0f, angle, 0f), new Vector3(Props.width, 1f, BeamEndHeight));
			Graphics.DrawMesh(MeshPool.plane10, matrix2, BeamEndMat, 0, null, 0, MatPropertyBlock);
		}
	}

	private void CheckSpawnSustainer()
	{
		Log.Message("CheckSpawnSustainer");
		if (TicksLeft >= fadeOutDuration && Props.sound != null)
		{
			LongEventHandler.ExecuteWhenFinished(delegate
			{
				sustainer = Props.sound.TrySpawnSustainer(SoundInfo.InMap(parent, MaintenanceType.PerTick));
			});
		}
	}
}
