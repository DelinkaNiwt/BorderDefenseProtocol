using UnityEngine;
using Verse;

namespace ECT;

[StaticConstructorOnStartup]
public class WeatherEvent_LightningTrail : WeatherEvent
{
	private Mesh boltMesh;

	private Vector3 startPos;

	private Quaternion rotation;

	private float targetLength;

	private int duration;

	private int growTicks;

	private float variance;

	private float width;

	private int age = 0;

	private static readonly Material LightningMat = MatLoader.LoadMat("Weather/LightningBolt");

	public override bool Expired => age > duration;

	public WeatherEvent_LightningTrail(Map map, Vector3 start, Vector3 dir, float length, int duration, int growTicks, float variance, float width)
		: base(map)
	{
		startPos = start;
		targetLength = length;
		this.duration = duration;
		this.growTicks = growTicks;
		this.variance = variance;
		this.width = width;
		rotation = Quaternion.LookRotation(dir);
		boltMesh = ECT_LightningMeshPool.RandomBoltMesh;
	}

	public override void FireEvent()
	{
	}

	public override void WeatherEventTick()
	{
		age++;
	}

	public override void WeatherEventDraw()
	{
		if (age < duration && boltMesh != null)
		{
			float alpha = 1f;
			int num = (int)((float)duration * 0.6f);
			if (age > num)
			{
				alpha = 1f - (float)(age - num) / (float)(duration - num);
			}
			float num2 = 1f;
			if (age < growTicks)
			{
				num2 = 0.05f + 0.95f * ((float)age / (float)growTicks);
			}
			Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(width * variance, 1f, targetLength * num2), pos: startPos, q: rotation);
			Graphics.DrawMesh(boltMesh, matrix, FadedMaterialPool.FadedVersionOf(LightningMat, alpha), 0);
		}
	}
}
