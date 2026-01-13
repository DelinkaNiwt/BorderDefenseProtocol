using UnityEngine;
using Verse;

namespace ATFieldGenerator;

public class Mote_ATFieldOctagon : Mote
{
	private const float ScaleStart = 0.2f;

	private const float ScaleEnd = 2f;

	public float customRotation = 0f;

	public float damageScale = 1f;

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		if (def.graphicData == null)
		{
			return;
		}
		ATFieldMoteExtension modExtension = def.GetModExtension<ATFieldMoteExtension>();
		float num = modExtension?.duration ?? 0.4f;
		float ageSecs = base.AgeSecs;
		float num2 = ((num > 0.001f) ? (ageSecs / num) : 1f);
		if (num2 > 1f)
		{
			return;
		}
		Color value;
		if (modExtension == null || modExtension.colors == null || modExtension.colors.Count < 2)
		{
			value = ((modExtension == null) ? Color.Lerp(Color.white, Color.clear, num2) : Color.Lerp(modExtension.colorStart, modExtension.colorEnd, num2));
		}
		else
		{
			int count = modExtension.colors.Count;
			int num3 = count - 1;
			float num4 = num2 * (float)num3;
			int num5 = (int)num4;
			if (num5 >= num3)
			{
				num5 = num3 - 1;
				num4 = num3;
			}
			float t = num4 - (float)num5;
			value = Color.Lerp(modExtension.colors[num5], modExtension.colors[num5 + 1], t);
		}
		float num6 = Mathf.Lerp(0.2f, 2f, num2);
		float num7 = num6 * damageScale;
		Vector3 s = new Vector3(def.graphicData.drawSize.x * num7, 1f, def.graphicData.drawSize.y * num7);
		Matrix4x4 matrix = default(Matrix4x4);
		drawLoc.y = def.altitudeLayer.AltitudeFor();
		matrix.SetTRS(drawLoc, Quaternion.AngleAxis(customRotation, Vector3.up), s);
		Material matSingle = def.graphicData.Graphic.MatSingle;
		if (!(matSingle == null))
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			materialPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
			Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0, null, 0, materialPropertyBlock);
		}
	}
}
