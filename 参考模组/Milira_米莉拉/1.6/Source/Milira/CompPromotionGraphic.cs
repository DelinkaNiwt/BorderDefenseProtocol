using UnityEngine;
using Verse;

namespace Milira;

[StaticConstructorOnStartup]
public class CompPromotionGraphic : ThingComp
{
	private float floatOffset = 0f;

	private float alpha = 1f;

	private float randTime = Rand.Range(0f, 600f);

	public bool drawGraphic = false;

	private bool drawBishop = false;

	private bool drawKnight = false;

	private bool drawRook = false;

	private bool drawQueen = false;

	private static readonly Material BishopGraphic = MaterialPool.MatFrom("Milira/Effect/Promotion/Promotion_Bishop", ShaderDatabase.MoteGlow, Color.white);

	private static readonly Material KnightGraphic = MaterialPool.MatFrom("Milira/Effect/Promotion/Promotion_Knight", ShaderDatabase.MoteGlow, Color.white);

	private static readonly Material RookGraphic = MaterialPool.MatFrom("Milira/Effect/Promotion/Promotion_Rook", ShaderDatabase.MoteGlow, Color.white);

	private static readonly Material QueenGraphic = MaterialPool.MatFrom("Milira/Effect/Promotion/Promotion_Queen", ShaderDatabase.MoteGlow, Color.white);

	private CompProperties_PromotionGraphic Props => (CompProperties_PromotionGraphic)props;

	private GraphicData graphicData => Props.graphicData;

	public bool drawAdditionalGraphic
	{
		get
		{
			if (Props.drawAdditionalGraphicDefault)
			{
				return true;
			}
			return false;
		}
		set
		{
			drawGraphic = value;
		}
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (drawAdditionalGraphic || drawGraphic)
		{
			if (drawKnight)
			{
				Mesh mesh = graphicData.Graphic.MeshAt(parent.Rotation);
				Vector3 drawPos = parent.DrawPos;
				drawPos.z = parent.DrawPos.z + floatOffset;
				drawPos.y = Props.altitudeLayer.AltitudeFor();
				Color color = KnightGraphic.color;
				KnightGraphic.color = new Color(color.r, color.g, color.b, alpha);
				Graphics.DrawMesh(mesh, drawPos + graphicData.drawOffset, Quaternion.identity, KnightGraphic, 0);
			}
			else if (drawRook)
			{
				Mesh mesh2 = graphicData.Graphic.MeshAt(parent.Rotation);
				Vector3 drawPos2 = parent.DrawPos;
				drawPos2.z = parent.DrawPos.z + floatOffset;
				drawPos2.y = Props.altitudeLayer.AltitudeFor();
				Color color2 = RookGraphic.color;
				RookGraphic.color = new Color(color2.r, color2.g, color2.b, alpha);
				Graphics.DrawMesh(mesh2, drawPos2 + graphicData.drawOffset, Quaternion.identity, RookGraphic, 0);
			}
			else if (drawBishop)
			{
				Mesh mesh3 = graphicData.Graphic.MeshAt(parent.Rotation);
				Vector3 drawPos3 = parent.DrawPos;
				drawPos3.z = parent.DrawPos.z + floatOffset;
				drawPos3.y = Props.altitudeLayer.AltitudeFor();
				Color color3 = BishopGraphic.color;
				BishopGraphic.color = new Color(color3.r, color3.g, color3.b, alpha);
				Graphics.DrawMesh(mesh3, drawPos3 + graphicData.drawOffset, Quaternion.identity, BishopGraphic, 0);
			}
			else if (drawQueen)
			{
				Mesh mesh4 = graphicData.Graphic.MeshAt(parent.Rotation);
				Vector3 drawPos4 = parent.DrawPos;
				drawPos4.z = parent.DrawPos.z + floatOffset;
				drawPos4.y = Props.altitudeLayer.AltitudeFor();
				Color color4 = QueenGraphic.color;
				QueenGraphic.color = new Color(color4.r, color4.g, color4.b, alpha);
				Graphics.DrawMesh(mesh4, drawPos4 + graphicData.drawOffset, Quaternion.identity, QueenGraphic, 0);
			}
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		floatOffset = Mathf.Sin(((float)Find.TickManager.TicksGame + randTime) * Props.floatSpeed) * Props.floatAmplitude;
		alpha = Mathf.Sin(((float)Find.TickManager.TicksGame + randTime) * Props.flickerSpeed);
		Pawn pawn = parent as Pawn;
		if (!drawKnight && !drawRook && !drawBishop && !drawQueen)
		{
			if (!drawKnight && (pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_KnightI) || pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_KnightII) || pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_KnightIII) || pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_KnightIV)))
			{
				drawKnight = true;
			}
			else if (!drawRook && (pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_RookI) || pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_RookII) || pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_RookIII) || pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_RookIV)))
			{
				drawRook = true;
			}
			else if (!drawBishop && (pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_BishopI) || pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_BishopII) || pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_BishopIII) || pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_BishopIV)))
			{
				drawBishop = true;
			}
			else if (!drawQueen && pawn.health.hediffSet.HasHediff(MiliraDefOf.Milian_PawnPromotion_Queen))
			{
				drawQueen = true;
			}
		}
	}
}
