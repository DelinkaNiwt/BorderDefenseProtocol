using System;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class CompProperties_Puzzle : CompProperties
	{
		public CompProperties_Puzzle()
		{
			this.compClass = typeof(CompPuzzle);
		}

		public bool isPuzzle;

		public bool answer;

		public string graphic;

		public Vector3 drawOffset = new Vector3(0, 0, 0.8f);
	}
}
