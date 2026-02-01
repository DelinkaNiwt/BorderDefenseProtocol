using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Text;
using UnityEngine;

namespace GD3
{
	public class CompUseEffect_Trainer : CompUseEffect
	{
		public override AcceptanceReport CanBeUsedBy(Pawn p)
		{
			bool flag = p.skills.skills.FindAll((SkillRecord s) => s.Level < 20).Count == 0;
			AcceptanceReport result;
			if (flag)
			{
				result = "AllSkillsPerfect".Translate();
			}
			else
			{
				result = base.CanBeUsedBy(p);
			}
			return result;
		}

		public override void DoEffect(Pawn user)
		{
			base.DoEffect(user);
			foreach (SkillRecord skillRecord in user.skills.skills)
			{
				if (skillRecord.Level < 20 && !skillRecord.PermanentlyDisabled)
                {
					skillRecord.Level += 1;
				}
			}
			this.DoDestroy();
		}
		private void DoDestroy()
		{
			this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
		}
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.delayTicks, "delayTicks", -1, false);
		}

		public override void CompTick()
		{
			base.CompTick();
			if (this.delayTicks > 0)
			{
				this.delayTicks--;
			}
			if (this.delayTicks == 0)
			{
				this.DoDestroy();
				this.delayTicks = -1;
			}
		}
		private int delayTicks = -1;
	}
}
