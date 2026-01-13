using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace ECT;

[StaticConstructorOnStartup]
public static class ECT_LightningMeshPool
{
	public static readonly List<Mesh> BoltMeshes;

	private const int MeshCount = 20;

	public static Mesh RandomBoltMesh => BoltMeshes.RandomElement();

	static ECT_LightningMeshPool()
	{
		BoltMeshes = new List<Mesh>();
		for (int i = 0; i < 20; i++)
		{
			BoltMeshes.Add(CreateUnitBoltMesh());
		}
	}

	private static Mesh CreateUnitBoltMesh()
	{
		int num = 40;
		List<Vector3> list = new List<Vector3>();
		Perlin perlin = new Perlin(0.02, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
		for (int i = 0; i <= num; i++)
		{
			float num2 = (float)i / (float)num;
			float z = num2;
			float num3 = Mathf.Sin(num2 * (float)Math.PI);
			float x = (float)perlin.GetValue(i, 0.0, 0.0) * num3;
			Vector3 vector = new Vector3(x, 0f, z);
			Vector3 item = vector + new Vector3(-0.5f, 0f, 0f);
			Vector3 item2 = vector + new Vector3(0.5f, 0f, 0f);
			list.Add(item);
			list.Add(item2);
		}
		Vector2[] array = new Vector2[list.Count];
		float num4 = 0f;
		for (int j = 0; j < list.Count; j += 2)
		{
			array[j] = new Vector2(0f, num4);
			array[j + 1] = new Vector2(1f, num4);
			num4 += 0.05f;
		}
		int num5 = num * 6;
		int[] array2 = new int[num5 * 2];
		int num6 = 0;
		for (int k = 0; k < num; k++)
		{
			int num7 = k * 2;
			array2[num6++] = num7;
			array2[num6++] = num7 + 1;
			array2[num6++] = num7 + 2;
			array2[num6++] = num7 + 2;
			array2[num6++] = num7 + 1;
			array2[num6++] = num7 + 3;
		}
		for (int l = 0; l < num; l++)
		{
			int num8 = l * 2;
			array2[num6++] = num8;
			array2[num6++] = num8 + 2;
			array2[num6++] = num8 + 1;
			array2[num6++] = num8 + 2;
			array2[num6++] = num8 + 3;
			array2[num6++] = num8 + 1;
		}
		Mesh mesh = new Mesh();
		mesh.vertices = list.ToArray();
		mesh.uv = array;
		mesh.triangles = array2;
		mesh.RecalculateNormals();
		mesh.bounds = new Bounds(Vector3.zero, new Vector3(100f, 100f, 100f));
		return mesh;
	}
}
