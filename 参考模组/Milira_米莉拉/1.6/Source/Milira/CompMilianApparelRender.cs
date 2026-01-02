using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Milira;

public class CompMilianApparelRender : ThingComp
{
	private List<Apparel> apparels;

	private bool isHeadSet = false;

	public bool autoSuitUp = false;

	public bool displayInBillWorker = false;

	public CompProperties_MilianApparelRender Props => (CompProperties_MilianApparelRender)props;

	private Pawn Pawn => parent as Pawn;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref autoSuitUp, "autoSuitUp", defaultValue: false);
		Scribe_Values.Look(ref displayInBillWorker, "displayInBillWorker", defaultValue: false);
	}

	public override List<PawnRenderNode> CompRenderNodes()
	{
		apparels = Pawn.apparel.WornApparel;
		if (Pawn != null && !apparels.NullOrEmpty())
		{
			List<PawnRenderNode> list = new List<PawnRenderNode>();
			foreach (Apparel apparel in apparels)
			{
				if (apparel.def.apparel.HasDefinedGraphicProperties)
				{
					foreach (PawnRenderNodeProperties renderNodeProperty in apparel.def.apparel.RenderNodeProperties)
					{
						PawnRenderNode pawnRenderNode = (PawnRenderNode)Activator.CreateInstance(renderNodeProperty.nodeClass, Pawn, renderNodeProperty, Pawn.Drawer.renderer.renderTree);
						pawnRenderNode.apparel = apparel;
						list.Add(pawnRenderNode);
					}
				}
				if (!ApparelGraphicRecordGetter.TryGetGraphicApparel(apparel, BodyTypeDefOf.Female, forStatue: false, out var _))
				{
					continue;
				}
				int num = 20;
				ApparelLayerDef lastLayer = apparel.def.apparel.LastLayer;
				ApparelLayerDef apparelLayerDef = lastLayer;
				if (apparelLayerDef != null)
				{
					if (apparelLayerDef == ApparelLayerDefOf.OnSkin)
					{
						num = 21;
						isHeadSet = false;
					}
					else
					{
						ApparelLayerDef apparelLayerDef2 = apparelLayerDef;
						if (apparelLayerDef2 == ApparelLayerDefOf.Middle)
						{
							num = 22;
							isHeadSet = false;
						}
						else
						{
							ApparelLayerDef apparelLayerDef3 = apparelLayerDef;
							if (apparelLayerDef3 == ApparelLayerDefOf.Shell)
							{
								num = 23;
								isHeadSet = false;
							}
							else
							{
								ApparelLayerDef apparelLayerDef4 = apparelLayerDef;
								if (apparelLayerDef4 == ApparelLayerDefOf.Overhead)
								{
									num = 71;
									isHeadSet = true;
								}
								else
								{
									ApparelLayerDef apparelLayerDef5 = apparelLayerDef;
									if (apparelLayerDef5 == ApparelLayerDefOf.EyeCover)
									{
										num = 72;
										isHeadSet = true;
									}
									else
									{
										ApparelLayerDef apparelLayerDef6 = apparelLayerDef;
										if (apparelLayerDef6 == ApparelLayerDefOf.Belt)
										{
											num = 26;
											isHeadSet = false;
										}
									}
								}
							}
						}
					}
				}
				PawnRenderNodeProperties pawnRenderNodeProperties = new PawnRenderNodeProperties();
				pawnRenderNodeProperties.debugLabel = apparel.def.defName;
				pawnRenderNodeProperties.parentTagDef = PawnRenderNodeTagDefOf.ApparelBody;
				pawnRenderNodeProperties.workerClass = typeof(PawnRenderNodeWorker_MilianApparel_Body);
				pawnRenderNodeProperties.baseLayer = num;
				pawnRenderNodeProperties.pawnType = PawnRenderNodeProperties.RenderNodePawnType.Any;
				pawnRenderNodeProperties.drawData = apparel.def.apparel.drawData;
				PawnRenderNodeProperties pawnRenderNodeProperties2 = pawnRenderNodeProperties;
				if (isHeadSet)
				{
					pawnRenderNodeProperties2.parentTagDef = PawnRenderNodeTagDefOf.ApparelHead;
					pawnRenderNodeProperties2.workerClass = typeof(PawnRenderNodeWorker_MilianApparel_Head);
				}
				PawnRenderNode_MilianApparel item = new PawnRenderNode_MilianApparel(Pawn, pawnRenderNodeProperties2, Pawn.Drawer.renderer.renderTree, apparel);
				list.Add(item);
			}
			return list;
		}
		return base.CompRenderNodes();
	}
}
