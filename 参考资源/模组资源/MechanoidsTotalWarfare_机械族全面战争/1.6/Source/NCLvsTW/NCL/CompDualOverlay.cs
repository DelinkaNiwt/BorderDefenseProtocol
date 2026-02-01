using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NCL;

public class CompDualOverlay : ThingComp
{
	private Graphic staticGraphic;

	private Graphic floatingGraphic;

	private CompProperties_DualOverlay Props => (CompProperties_DualOverlay)props;

	public override void PostDraw()
	{
		base.PostDraw();
		DrawStaticOverlay();
		DrawFloatingOverlay();
	}

	private void DrawStaticOverlay()
	{
		if (!string.IsNullOrEmpty(Props.staticGraphicPath))
		{
			if (staticGraphic == null)
			{
				staticGraphic = GraphicDatabase.Get<Graphic_Single>(Props.staticGraphicPath, ShaderDatabase.Transparent, Vector2.one * Props.staticScale, Color.white);
			}
			Vector3 drawPos = parent.DrawPos + Props.staticOffset;
			drawPos.y = Props.staticAltitudeLayer.AltitudeFor();
			Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.AngleAxis(Props.staticRotation, Vector3.up), Vector3.one * Props.staticScale);
			Graphics.DrawMesh(MeshPool.plane10, matrix, staticGraphic.MatSingle, 0);
		}
	}

	private void DrawFloatingOverlay()
	{
		if (!string.IsNullOrEmpty(Props.floatingGraphicPath))
		{
			if (floatingGraphic == null)
			{
				floatingGraphic = GraphicDatabase.Get<Graphic_Single>(Props.floatingGraphicPath, ShaderDatabase.Transparent, Vector2.one * Props.floatingScale, Color.white);
			}
			Vector3 basePos = parent.DrawPos + Props.floatingOffset;
			float verticalOffset = Mathf.Sin((float)Find.TickManager.TicksGame * Props.floatFrequency) * Props.floatAmplitude;
			Vector3 drawPos = new Vector3(basePos.x, Props.floatingAltitudeLayer.AltitudeFor(), basePos.z + verticalOffset);
			Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.AngleAxis(Props.floatingRotation, Vector3.up), Vector3.one * Props.floatingScale);
			Graphics.DrawMesh(MeshPool.plane10, matrix, floatingGraphic.MatSingle, 0);
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (DebugSettings.godMode)
		{
			yield return new Command_Action
			{
				defaultLabel = "Static: Offset +X",
				action = delegate
				{
					Props.staticOffset.x += 0.1f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Static: Offset -X",
				action = delegate
				{
					Props.staticOffset.x -= 0.1f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Static: Offset +Z",
				action = delegate
				{
					Props.staticOffset.z += 0.1f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Static: Offset -Z",
				action = delegate
				{
					Props.staticOffset.z -= 0.1f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Static: Rotate +15°",
				action = delegate
				{
					Props.staticRotation = (Props.staticRotation + 15f) % 360f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Floating: Offset +X",
				action = delegate
				{
					Props.floatingOffset.x += 0.1f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Floating: Offset -X",
				action = delegate
				{
					Props.floatingOffset.x -= 0.1f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Floating: Offset +Z",
				action = delegate
				{
					Props.floatingOffset.z += 0.1f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Floating: Offset -Z",
				action = delegate
				{
					Props.floatingOffset.z -= 0.1f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Floating: Rotate +15°",
				action = delegate
				{
					Props.floatingRotation = (Props.floatingRotation + 15f) % 360f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Float: Amplitude +0.05",
				action = delegate
				{
					Props.floatAmplitude += 0.05f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Float: Amplitude -0.05",
				action = delegate
				{
					Props.floatAmplitude = Mathf.Max(0f, Props.floatAmplitude - 0.05f);
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Float: Frequency +0.005",
				action = delegate
				{
					Props.floatFrequency += 0.005f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Float: Frequency -0.005",
				action = delegate
				{
					Props.floatFrequency = Mathf.Max(0f, Props.floatFrequency - 0.005f);
				}
			};
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		if ((Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.Saving) && DebugSettings.godMode)
		{
			Scribe_Values.Look(ref Props.staticOffset, "staticOffset");
			Scribe_Values.Look(ref Props.staticRotation, "staticRotation", 0f);
			Scribe_Values.Look(ref Props.floatingOffset, "floatingOffset");
			Scribe_Values.Look(ref Props.floatingRotation, "floatingRotation", 0f);
			Scribe_Values.Look(ref Props.floatAmplitude, "floatAmplitude", 0f);
			Scribe_Values.Look(ref Props.floatFrequency, "floatFrequency", 0f);
		}
	}
}
