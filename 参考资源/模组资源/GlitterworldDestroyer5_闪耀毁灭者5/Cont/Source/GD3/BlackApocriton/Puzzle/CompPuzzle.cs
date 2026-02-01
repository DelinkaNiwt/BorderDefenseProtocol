using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using System.Text;
using UnityEngine;

namespace GD3
{
	public class CompPuzzle : ThingComp
	{
		public CompProperties_Puzzle Props
		{
			get
			{
				return (CompProperties_Puzzle)this.props;
			}
		}

		public Building Pillar
		{
			get
			{
				return this.parent as Building;
			}
		}

		public bool Answer
        {
            get
            {
				return this.state;
            }
        }

		public bool IsPuzzle
        {
            get
            {
				return this.Props.isPuzzle;
            }
        }

		public override void CompTick()
		{
			base.CompTick();
			bool flag = this.parent != null && !this.Props.isPuzzle && this.checking;
			if (flag)
            {
				this.ticks++;
				if (this.ticks % 60 == 0)
                {
					GDDefOf.PuzzleTrigger.PlayOneShot(new TargetInfo(Pillar.PositionHeld, Pillar.MapHeld, false));
				}
				if (this.ticks > 301)
                {
					List<Thing> list = this.Pillar.Map.listerThings.AllThings.FindAll((Thing t) => t is Building);
					for (int i = 0; i < list.Count; i++)
                    {
						Building b = list[i] as Building;
						if (b == null)
                        {
							continue;
                        }
						CompPuzzle comp = b.TryGetComp<CompPuzzle>();
						if (comp != null && comp.IsPuzzle)
                        {
							if (comp.Props.answer != comp.state)
                            {
								this.result = false;
							}
                        }
                    }
					this.CheckResult(this.result, this.Pillar.DrawPos);
                }
            }
		}

        /*public override string CompInspectStringExtra()
        {
			if (!DebugSettings.ShowDevGizmos)
            {
				return null;
            }
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(this.state + "||" + this.Props.answer);
			return stringBuilder.ToString();
		}*/

        public override void PostDraw()
        {
            base.PostDraw();
			bool flag = this.IsPuzzle;
			if (flag)
			{
				if (this.state)
                {
					this.DrawExtra(this.Props.graphic);
                }
			}
            else
            {
				this.DrawExtra("Buildings/Puzzle/PuzzlePillar_Center");
			}
		}

		public void DrawExtra(string str)
        {
			//str = "Buildings/Puzzle/PuzzlePillar";
			Vector3 pos = this.Pillar.DrawPos;
			float scale = this.Pillar.def.graphicData.drawSize.x;
			pos.y = AltitudeLayer.MetaOverlays.AltitudeFor(0f);
			string graphic = str;

			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(pos + this.Props.drawOffset, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(scale, 1f, scale));
			Material material = MaterialPool.MatFrom(graphic, ShaderDatabase.Mote, new Color(255, 255, 255));
			Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
		}

		public void CheckResult(bool result, Vector3 pos)
        {
			this.checking = !this.checking;
			this.ticks = 0;
			if (result)
            {
				this.ended = true;
				MoteMaker.ThrowText(pos, this.Pillar.Map, "PuzzleScuccess".Translate(), 12f);
				Find.ResearchManager.FinishProject(GDDefOf.GD3_Puzzle);
				Find.LetterStack.ReceiveLetter("PuzzleFinished".Translate(), "PuzzleFinishedDesc".Translate(), LetterDefOf.PositiveEvent);
			}
            else
            {
				this.result = true;
				MoteMaker.ThrowText(pos, this.Pillar.Map, "PuzzleFailed".Translate(), 12f);
			}
        }

        public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.ticks, "ticks", 0, false);
			Scribe_Values.Look<bool>(ref this.state, "state", false, false);
			Scribe_Values.Look<bool>(ref this.checking, "checking", false, false);
			Scribe_Values.Look<bool>(ref this.ended, "ended", false, false);
			Scribe_Values.Look<bool>(ref this.result, "result", true, false);
		}

		public bool state = false;

		public bool checking = false;

		public int ticks;

		public bool ended = false;

		public bool result = true;
	}
}