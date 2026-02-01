using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class PawnRenderNodeProperties_AnnihilatorPart : PawnRenderNodeProperties
	{
		public BodyPartGroupDef linkedWithGroup;

		public PawnRenderNodeProperties_AnnihilatorPart()
		{
			nodeClass = typeof(PawnRenderNode_Annihilator_Part);
		}
	}

}
