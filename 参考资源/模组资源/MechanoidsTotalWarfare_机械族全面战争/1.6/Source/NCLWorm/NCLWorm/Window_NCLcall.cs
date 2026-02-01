using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCLWorm;

public class Window_NCLcall : Window
{
	public string instructionString;

	public Vector2 resultsAreaScroll;

	public Pawn usedBy;

	public NCLCallDef callDef;

	public NCLCallTool calltool;

	public bool useBaseFunction = true;

	public bool DrawPic = true;

	private bool isFirstCall;

	public override Vector2 InitialSize => new Vector2(886f, 680f);

	public Window_NCLcall(Pawn usedBy, NCLCallDef callDef, string name = null, bool useBaseFunction = true, NCLCallTool tool = null, bool DrawPic = true)
	{
		optionalTitle = "NCL".Translate();
		preventCameraMotion = true;
		forcePause = true;
		absorbInputAroundWindow = true;
		draggable = false;
		doCloseX = true;
		closeOnCancel = false;
		this.usedBy = usedBy;
		this.useBaseFunction = useBaseFunction;
		if (!name.NullOrEmpty())
		{
			instructionString = name;
		}
		else
		{
			instructionString = callDef.RandomHello.RandomElement();
		}
		this.callDef = callDef;
		calltool = tool;
		this.DrawPic = DrawPic;
		if (tool != null)
		{
			tool.windows = this;
		}
		isFirstCall = Current.Game.GetComponent<GameComp_NCLWorm>().firstCall;
	}

	public override void DoWindowContents(Rect inRect)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Expected O, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Expected O, but got Unknown
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Expected O, but got Unknown
		Rect rect = new Rect(inRect);
		Widgets.DrawTextureFitted(rect, NCLWormTexCommand.WindowBase, 1f);
		if (!isFirstCall)
		{
			float num = 250f;
			float height = 120f;
			float num2 = 10f;
			float num3 = 20f;
			Rect rect2 = new Rect(rect.xMax - num - num2 - num3, rect.yMin + num2, num, height);
			GUIStyle val = new GUIStyle(Text.CurTextAreaStyle)
			{
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.UpperRight,
				wordWrap = true,
				padding = new RectOffset(0, 5, 0, 0),
				normal = new GUIStyleState
				{
					background = null,
					textColor = Color.white
				},
				active = new GUIStyleState
				{
					background = null,
					textColor = Color.white
				},
				hover = new GUIStyleState
				{
					background = null,
					textColor = Color.white
				},
				border = new RectOffset(0, 0, 0, 0),
				margin = new RectOffset(0, 0, 0, 0),
				overflow = new RectOffset(0, 0, 0, 0)
			};
			GUIStyle val2 = new GUIStyle(val)
			{
				fontSize = (int)((float)val.fontSize * 2.7f),
				fontStyle = FontStyle.Bold
			};
			GUIStyle val3 = new GUIStyle(val)
			{
				fontSize = (int)((float)val.fontSize * 0.9f),
				fontStyle = FontStyle.Bold
			};
			string text = "UnknownAddressEncrypted".Translate();
			GUI.Label(rect2, text, val2);
			Rect rect3 = new Rect(rect2);
			rect3.y += 60f;
			GUI.Label(rect3, (string)("NCLEncryptedComms".Translate() + "\n" + "Factional_Relation".Translate()), val3);
		}
		Rect source = new Rect(rect);
		source.height = rect.height - 250f;
		source.width = rect.width - 200f;
		Rect rect4 = new Rect(source);
		rect4.width *= 0.75f;
		rect4.x += 30f;
		rect4.y += 25f;
		Rect rect5 = new Rect(source);
		rect5.height = rect.height;
		rect5.width = rect.width - source.width;
		rect5.x = source.x + source.width - 20f - 40f;
		Rect rect6 = rect5;
		float num4 = 1.2f;
		rect5.height *= num4;
		rect5.width *= num4;
		rect5.x = rect6.x + rect6.width - rect5.width;
		rect5.y = rect6.y + rect6.height - rect5.height;
		rect5.x += 20f;
		rect5.y += 110f;
		if (DrawPic)
		{
			if (calltool != null && calltool.GraphicData != null)
			{
				Widgets.DrawTextureFitted(rect5, ContentFinder<Texture2D>.Get(calltool.GraphicData.texPath), calltool.GraphicData.drawSize.x);
			}
			else
			{
				Widgets.DrawTextureFitted(rect5, NCLWormTexCommand.NCLCourier, 1.4f);
			}
		}
		Text.Font = GameFont.Small;
		Widgets.TextArea(rect4, instructionString, readOnly: true);
		int num5 = 6;
		if (useBaseFunction)
		{
			num5 = callDef.NCLCallTools.Count;
			foreach (NCLCallTool nCLCallTool in callDef.NCLCallTools)
			{
				if (nCLCallTool.NoCanSee())
				{
					num5--;
				}
			}
		}
		else if (calltool != null && calltool is NCLCallTool_TraderShip nCLCallTool_TraderShip)
		{
			num5 = ((!nCLCallTool_TraderShip.Canuse()) ? 1 : nCLCallTool_TraderShip.TraderKindDefs.Count);
		}
		else if (calltool != null && calltool is NCLCallTool_Bool)
		{
			num5 = 2;
		}
		else if (calltool != null && calltool is NCLCallTool_LianXuDuiHua nCLCallTool_LianXuDuiHua)
		{
			num5 = nCLCallTool_LianXuDuiHua.NextCallTools.Count;
		}
		Rect rect7 = new Rect(rect4);
		rect7.width = rect.width / 2f;
		rect7.y = rect.height - (float)Math.Min(150, 25 * num5);
		rect7.height = 25 * Math.Min(6, num5);
		rect7.x += 15f;
		rect7.y -= 90f;
		Rect rect8 = new Rect(rect7);
		rect8.width -= 16f;
		rect8.height = 25 * num5;
		Widgets.BeginScrollView(rect7, ref resultsAreaScroll, rect8);
		Rect source2 = new Rect(rect8);
		source2.height = 25f;
		source2.width -= 16f;
		if (useBaseFunction)
		{
			foreach (NCLCallTool nCLCallTool2 in callDef.NCLCallTools)
			{
				if (nCLCallTool2.NoCanSee())
				{
					continue;
				}
				nCLCallTool2.NCLCall = callDef;
				nCLCallTool2.windows = this;
				Rect rect9 = new Rect(source2);
				Widgets.DrawHighlightIfMouseover(rect9);
				string text2 = nCLCallTool2.label;
				bool flag = nCLCallTool2.Canuse();
				Color textColor = Widgets.NormalOptionColor;
				if (!flag)
				{
					text2 += nCLCallTool2.Canuse().Reason;
					if (!(nCLCallTool2 is NCLCallTool_TraderShip))
					{
						textColor = Color.gray;
					}
				}
				if (Widgets.ButtonText(rect9, text2, drawBackground: false, doMouseoverSound: true, textColor, nCLCallTool2 is NCLCallTool_TraderShip || flag))
				{
					if (!nCLCallTool2.FirstUseMess.NullOrEmpty() && !Current.Game.GetComponent<GameComp_NCLWorm>().Usedcalltools.Contains(nCLCallTool2.label))
					{
						Current.Game.GetComponent<GameComp_NCLWorm>().Usedcalltools.Add(nCLCallTool2.label);
						Close();
						Find.WindowStack.Add(new Window_NCLcall(usedBy, callDef, nCLCallTool2.FirstUseMess));
					}
					else
					{
						nCLCallTool2.Action();
					}
				}
				source2.y += 25f;
			}
		}
		else if (calltool != null)
		{
			if (calltool is NCLCallTool_TraderShip nCLCallTool_TraderShip2)
			{
				if ((bool)nCLCallTool_TraderShip2.Canuse())
				{
					foreach (TraderKindDef traderKindDef in nCLCallTool_TraderShip2.TraderKindDefs)
					{
						Rect rect10 = new Rect(source2);
						if (Widgets.ButtonText(rect10, traderKindDef.LabelCap, drawBackground: false))
						{
							nCLCallTool_TraderShip2.SecAction(traderKindDef);
						}
						source2.y += 25f;
					}
				}
				else
				{
					Rect rect11 = new Rect(source2);
					if (Widgets.ButtonText(rect11, "GoBack".Translate(), drawBackground: false))
					{
						nCLCallTool_TraderShip2.TriAction();
					}
				}
			}
			else if (calltool is NCLCallTool_Bool nCLCallTool_Bool)
			{
				Rect rect12 = new Rect(source2);
				if (Widgets.ButtonText(rect12, nCLCallTool_Bool.TextYes, drawBackground: false, doMouseoverSound: true, nCLCallTool_Bool.Canuse()))
				{
					nCLCallTool_Bool.SecAction();
				}
				source2.y += 25f;
				Rect rect13 = new Rect(source2);
				if (Widgets.ButtonText(rect13, nCLCallTool_Bool.TextNo, drawBackground: false, doMouseoverSound: true, nCLCallTool_Bool.Canuse()))
				{
					nCLCallTool_Bool.TriAction();
				}
			}
			else if (calltool is NCLCallTool_LianXuDuiHua nCLCallTool_LianXuDuiHua2)
			{
				foreach (NCLCallTool nextCallTool in nCLCallTool_LianXuDuiHua2.NextCallTools)
				{
					nextCallTool.NCLCall = callDef;
					nextCallTool.windows = this;
					Rect rect14 = new Rect(source2);
					string text3 = nextCallTool.label;
					bool flag2 = nextCallTool.Canuse();
					Color textColor2 = Widgets.NormalOptionColor;
					if (!flag2)
					{
						text3 += nextCallTool.Canuse().Reason;
						if (!(nextCallTool is NCLCallTool_TraderShip))
						{
							textColor2 = Color.gray;
						}
					}
					if (Widgets.ButtonText(rect14, text3, drawBackground: false, doMouseoverSound: true, textColor2, flag2))
					{
						nextCallTool.Action();
					}
					source2.y += 25f;
				}
			}
		}
		Widgets.EndScrollView();
		Log.WarningOnce("MainRect" + rect.width + "," + rect.height + ")+(" + rect.x + "," + rect.y + ")", 4399);
		Log.WarningOnce("rectStory" + rect4.width + "," + rect4.height + ")+(" + rect4.x + "," + rect4.y + ")", 4391);
		Log.WarningOnce("rectStoryTex" + rect5.width + "," + rect5.height + ")+(" + rect5.x + "," + rect5.y + ")", 4329);
	}
}
